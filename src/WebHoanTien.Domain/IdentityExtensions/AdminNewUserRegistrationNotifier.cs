using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Notifications;

namespace WebHoanTien.IdentityExtensions;

public enum UserSelfRegistrationMethod
{
    Email = 1,
    Google = 2,
    ExternalProvider = 3
}

public class AdminNewUserRegistrationNotifier : ITransientDependency
{
    private const string AdminRoleName = "admin";
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "CatBack-Vietnam", TimeSpan.FromHours(7), "Việt Nam", "Việt Nam");

    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly CustomerNotificationManager _notificationManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<AdminNewUserRegistrationNotifier> _logger;

    public AdminNewUserRegistrationNotifier(
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        CustomerNotificationManager notificationManager,
        IBackgroundJobManager backgroundJobManager,
        IEmailSender emailSender,
        IClock clock,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<AdminNewUserRegistrationNotifier> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _notificationManager = notificationManager;
        _backgroundJobManager = backgroundJobManager;
        _emailSender = emailSender;
        _clock = clock;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task EnqueueAsync(Guid userId, UserSelfRegistrationMethod registrationMethod)
    {
        var args = new AdminNewUserRegistrationJobArgs
        {
            UserId = userId,
            RegistrationMethod = registrationMethod
        };
        var currentUnitOfWork = _unitOfWorkManager.Current;
        if (currentUnitOfWork is null)
        {
            await EnqueueJobAsync(args);
            return;
        }

        currentUnitOfWork.OnCompleted(() => EnqueueJobAsync(args));
    }

    [UnitOfWork]
    public virtual async Task ProcessAsync(
        IdentityUser newUser,
        UserSelfRegistrationMethod registrationMethod)
    {
        if (string.IsNullOrWhiteSpace(newUser.Email) ||
            !await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            return;
        }

        var administrators = (await _userManager.GetUsersInRoleAsync(AdminRoleName))
            .Where(user => user.IsActive && user.Id != newUser.Id)
            .GroupBy(user => user.Id)
            .Select(group => group.First())
            .ToList();
        if (administrators.Count == 0)
        {
            return;
        }

        var registeredAt = newUser.CreationTime == default ? _clock.Now : newUser.CreationTime;
        var methodLabel = GetRegistrationMethodLabel(registrationMethod);
        var emailDetails = new RegistrationEmailDetails(
            newUser.Id,
            newUser.Email,
            newUser.UserName ?? string.Empty,
            methodLabel,
            registeredAt);
        var emailRecipients = new List<string>();
        foreach (var administrator in administrators)
        {
            var created = await _notificationManager.CreateOnceAsync(
                administrator.Id,
                CustomerNotificationCategory.Administration,
                CustomerNotificationKind.NewUserRegistered,
                "Người dùng mới đăng ký",
                $"{newUser.Email} vừa đăng ký tài khoản CatBack qua {methodLabel}.",
                "/Identity/Users",
                $"registration:{newUser.Id:N}");

            if (created && !string.IsNullOrWhiteSpace(administrator.Email))
            {
                emailRecipients.Add(administrator.Email.Trim());
            }
        }

        if (emailRecipients.Count == 0)
        {
            return;
        }

        var recipients = emailRecipients.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var currentUnitOfWork = _unitOfWorkManager.Current;
        if (currentUnitOfWork is null)
        {
            await QueueEmailsAsync(recipients, emailDetails);
            return;
        }

        currentUnitOfWork.OnCompleted(() => QueueEmailsAsync(recipients, emailDetails));
    }

    private async Task EnqueueJobAsync(AdminNewUserRegistrationJobArgs args)
    {
        try
        {
            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Không thể xếp background job thông báo đăng ký của user {UserId}.",
                args.UserId);
        }
    }

    private async Task QueueEmailsAsync(
        IEnumerable<string> recipients,
        RegistrationEmailDetails details)
    {
        var body = BuildEmailBody(details);
        foreach (var recipient in recipients)
        {
            try
            {
                await _emailSender.QueueAsync(
                    recipient,
                    "CatBack - Có người dùng mới đăng ký",
                    body,
                    isBodyHtml: true);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Không thể xếp email thông báo đăng ký của user {UserId} cho admin {AdminEmail}.",
                    details.UserId,
                    recipient);
            }
        }
    }

    private static string BuildEmailBody(RegistrationEmailDetails details)
    {
        var encoder = HtmlEncoder.Default;
        var localRegisteredAt = ToVietnamTime(details.RegisteredAt);
        return $"""
            <p>Chào admin,</p>
            <p>CatBack vừa có một người dùng mới đăng ký.</p>
            <table cellpadding="6" cellspacing="0" border="0">
                <tr><td><strong>Email</strong></td><td>{encoder.Encode(details.Email)}</td></tr>
                <tr><td><strong>Tên đăng nhập</strong></td><td>{encoder.Encode(details.UserName)}</td></tr>
                <tr><td><strong>Mã người dùng</strong></td><td>{details.UserId:D}</td></tr>
                <tr><td><strong>Hình thức</strong></td><td>{encoder.Encode(details.MethodLabel)}</td></tr>
                <tr><td><strong>Thời gian</strong></td><td>{localRegisteredAt:dd/MM/yyyy HH:mm} (GMT+7)</td></tr>
            </table>
            <p>Bạn có thể mở mục Quản trị người dùng trong CatBack để xem chi tiết.</p>
            """;
    }

    private static string GetRegistrationMethodLabel(UserSelfRegistrationMethod method) => method switch
    {
        UserSelfRegistrationMethod.Email => "email",
        UserSelfRegistrationMethod.Google => "Google",
        _ => "nhà cung cấp đăng nhập liên kết"
    };

    private static DateTime ToVietnamTime(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }

    private sealed record RegistrationEmailDetails(
        Guid UserId,
        string Email,
        string UserName,
        string MethodLabel,
        DateTime RegisteredAt);
}

[Serializable]
public sealed class AdminNewUserRegistrationJobArgs
{
    public Guid UserId { get; set; }
    public UserSelfRegistrationMethod RegistrationMethod { get; set; }
}

public class AdminNewUserRegistrationJob :
    IAsyncBackgroundJob<AdminNewUserRegistrationJobArgs>,
    ITransientDependency
{
    private readonly IdentityUserManager _userManager;
    private readonly AdminNewUserRegistrationNotifier _notifier;
    private readonly ILogger<AdminNewUserRegistrationJob> _logger;

    public AdminNewUserRegistrationJob(
        IdentityUserManager userManager,
        AdminNewUserRegistrationNotifier notifier,
        ILogger<AdminNewUserRegistrationJob> logger)
    {
        _userManager = userManager;
        _notifier = notifier;
        _logger = logger;
    }

    [UnitOfWork]
    public virtual async Task ExecuteAsync(AdminNewUserRegistrationJobArgs args)
    {
        var user = await _userManager.FindByIdAsync(args.UserId.ToString());
        if (user is null)
        {
            _logger.LogWarning(
                "Bỏ qua background job thông báo đăng ký vì không tìm thấy user {UserId}.",
                args.UserId);
            return;
        }

        await _notifier.ProcessAsync(user, args.RegistrationMethod);
    }
}
