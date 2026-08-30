using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
    private readonly string? _adminEmail;

    public AdminNewUserRegistrationNotifier(
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        CustomerNotificationManager notificationManager,
        IBackgroundJobManager backgroundJobManager,
        IEmailSender emailSender,
        IClock clock,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration,
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
        _adminEmail = configuration["AdminEmail"]?.Trim();
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
        if (string.IsNullOrWhiteSpace(newUser.Email))
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

        var administrators = await GetActiveAdministratorsAsync(newUser.Id);
        foreach (var administrator in administrators)
        {
            await _notificationManager.CreateOnceAsync(
                administrator.Id,
                CustomerNotificationCategory.Administration,
                CustomerNotificationKind.NewUserRegistered,
                "Người dùng mới đăng ký",
                $"{newUser.Email} vừa đăng ký tài khoản CatBack qua {methodLabel}.",
                "/Identity/Users",
                $"registration:{newUser.Id:N}");
        }

        if (!TryGetAdminEmail(out var adminEmail))
        {
            return;
        }

        var currentUnitOfWork = _unitOfWorkManager.Current;
        if (currentUnitOfWork is null)
        {
            await QueueEmailAsync(adminEmail, emailDetails);
            return;
        }

        currentUnitOfWork.OnCompleted(() => QueueEmailAsync(adminEmail, emailDetails));
    }

    private async Task<List<IdentityUser>> GetActiveAdministratorsAsync(Guid newUserId)
    {
        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            return new List<IdentityUser>();
        }

        return (await _userManager.GetUsersInRoleAsync(AdminRoleName))
            .Where(user => user.IsActive && user.Id != newUserId)
            .GroupBy(user => user.Id)
            .Select(group => group.First())
            .ToList();
    }

    private bool TryGetAdminEmail(out string adminEmail)
    {
        adminEmail = string.Empty;
        if (string.IsNullOrWhiteSpace(_adminEmail))
        {
            _logger.LogWarning(
                "Bỏ qua email thông báo đăng ký mới vì cấu hình AdminEmail đang trống.");
            return false;
        }

        if (!MailAddress.TryCreate(_adminEmail, out var address))
        {
            _logger.LogError(
                "Bỏ qua email thông báo đăng ký mới vì cấu hình AdminEmail không hợp lệ.");
            return false;
        }

        adminEmail = address.Address;
        return true;
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

    private async Task QueueEmailAsync(
        string recipient,
        RegistrationEmailDetails details)
    {
        var body = BuildEmailBody(details);
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
                "Không thể xếp email thông báo đăng ký của user {UserId} cho AdminEmail.",
                details.UserId);
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
