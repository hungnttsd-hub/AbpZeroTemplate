using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Notifications;

namespace WebHoanTien.Affiliates;

public class AdminWithdrawalRequestNotifier : ITransientDependency
{
    private const string AdminRoleName = "admin";
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "CatBack-Withdrawal-Vietnam", TimeSpan.FromHours(7), "Việt Nam", "Việt Nam");

    private readonly IRepository<WithdrawalRequest, Guid> _withdrawals;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly CustomerNotificationManager _notificationManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<AdminWithdrawalRequestNotifier> _logger;
    private readonly string? _adminEmail;

    public AdminWithdrawalRequestNotifier(
        IRepository<WithdrawalRequest, Guid> withdrawals,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        CustomerNotificationManager notificationManager,
        IBackgroundJobManager backgroundJobManager,
        IEmailSender emailSender,
        IClock clock,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration,
        ILogger<AdminWithdrawalRequestNotifier> logger)
    {
        _withdrawals = withdrawals;
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

    public async Task EnqueueAsync(Guid withdrawalRequestId)
    {
        var args = new AdminWithdrawalRequestJobArgs
        {
            WithdrawalRequestId = withdrawalRequestId
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
    public virtual async Task ProcessAsync(Guid withdrawalRequestId)
    {
        var request = await _withdrawals.FindAsync(withdrawalRequestId);
        if (request is null)
        {
            _logger.LogWarning(
                "Bỏ qua thông báo yêu cầu rút tiền vì không tìm thấy request {WithdrawalRequestId}.",
                withdrawalRequestId);
            return;
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        var userLabel = user?.Email ?? user?.UserName ?? request.UserId.ToString("D");
        var bankName = PayoutBankCatalog.Banks
            .FirstOrDefault(bank => bank.Code.Equals(request.BankCode, StringComparison.OrdinalIgnoreCase))?.Name
            ?? request.BankCode;
        var createdAt = request.CreationTime == default ? _clock.Now : request.CreationTime;
        var actionUrl = $"/Admin/Payouts?Filter={Uri.EscapeDataString(request.RequestCode)}";

        foreach (var administrator in await GetActiveAdministratorsAsync())
        {
            await _notificationManager.CreateOnceAsync(
                administrator.Id,
                CustomerNotificationCategory.Administration,
                CustomerNotificationKind.WithdrawalRequested,
                "Có yêu cầu rút tiền mới",
                $"{userLabel} vừa tạo yêu cầu {request.RequestCode} trị giá {FormatMoney(request.NetAmount)}.",
                actionUrl,
                $"admin-withdrawal:{request.Id:N}:pending");
        }

        if (!TryGetAdminEmail(out var adminEmail))
        {
            return;
        }

        var details = new WithdrawalEmailDetails(
            request.Id,
            request.RequestCode,
            WithdrawalTransferContent.Create(request.RequestCode),
            userLabel,
            request.Amount,
            request.FeeAmount,
            request.NetAmount,
            bankName,
            MaskAccount(request.AccountNumber),
            request.AccountHolderName,
            createdAt);

        var currentUnitOfWork = _unitOfWorkManager.Current;
        if (currentUnitOfWork is null)
        {
            await QueueEmailAsync(adminEmail, details);
            return;
        }

        currentUnitOfWork.OnCompleted(() => QueueEmailAsync(adminEmail, details));
    }

    private async Task<List<IdentityUser>> GetActiveAdministratorsAsync()
    {
        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            return new List<IdentityUser>();
        }

        return (await _userManager.GetUsersInRoleAsync(AdminRoleName))
            .Where(user => user.IsActive)
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
                "Bỏ qua email yêu cầu rút tiền mới vì cấu hình AdminEmail đang trống.");
            return false;
        }

        if (!MailAddress.TryCreate(_adminEmail, out var address))
        {
            _logger.LogError(
                "Bỏ qua email yêu cầu rút tiền mới vì cấu hình AdminEmail không hợp lệ.");
            return false;
        }

        adminEmail = address.Address;
        return true;
    }

    private async Task EnqueueJobAsync(AdminWithdrawalRequestJobArgs args)
    {
        try
        {
            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Không thể xếp background job thông báo yêu cầu rút tiền {WithdrawalRequestId}.",
                args.WithdrawalRequestId);
        }
    }

    private async Task QueueEmailAsync(string recipient, WithdrawalEmailDetails details)
    {
        try
        {
            await _emailSender.QueueAsync(
                recipient,
                $"CatBack - Yêu cầu rút tiền {details.RequestCode}",
                BuildEmailBody(details),
                isBodyHtml: true);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Không thể xếp email yêu cầu rút tiền {WithdrawalRequestId} cho AdminEmail.",
                details.WithdrawalRequestId);
        }
    }

    private static string BuildEmailBody(WithdrawalEmailDetails details)
    {
        var encoder = HtmlEncoder.Default;
        var localCreatedAt = ToVietnamTime(details.CreatedAt);
        return $"""
            <p>Chào admin,</p>
            <p>CatBack vừa nhận được một yêu cầu rút tiền mới.</p>
            <table cellpadding="6" cellspacing="0" border="0">
                <tr><td><strong>Mã yêu cầu</strong></td><td>{encoder.Encode(details.RequestCode)}</td></tr>
                <tr><td><strong>Nội dung chuyển khoản</strong></td><td><strong>{encoder.Encode(details.TransferContent)}</strong></td></tr>
                <tr><td><strong>Người dùng</strong></td><td>{encoder.Encode(details.UserLabel)}</td></tr>
                <tr><td><strong>Số tiền yêu cầu</strong></td><td>{FormatMoney(details.Amount)}</td></tr>
                <tr><td><strong>Phí</strong></td><td>{FormatMoney(details.FeeAmount)}</td></tr>
                <tr><td><strong>Thực nhận</strong></td><td>{FormatMoney(details.NetAmount)}</td></tr>
                <tr><td><strong>Ngân hàng</strong></td><td>{encoder.Encode(details.BankName)}</td></tr>
                <tr><td><strong>Tài khoản</strong></td><td>{encoder.Encode(details.MaskedAccountNumber)}</td></tr>
                <tr><td><strong>Chủ tài khoản</strong></td><td>{encoder.Encode(details.AccountHolderName)}</td></tr>
                <tr><td><strong>Thời gian</strong></td><td>{localCreatedAt:dd/MM/yyyy HH:mm} (GMT+7)</td></tr>
            </table>
            <p>Bạn có thể mở mục Quản lý chi trả trong CatBack để xử lý yêu cầu.</p>
            """;
    }

    private static string FormatMoney(decimal value) =>
        $"{Math.Max(0m, value).ToString("N0", VietnameseCulture)}đ";

    private static string MaskAccount(string value) => value.Length <= 4
        ? value
        : new string('*', Math.Min(4, value.Length - 4)) + value[^4..];

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

    private sealed record WithdrawalEmailDetails(
        Guid WithdrawalRequestId,
        string RequestCode,
        string TransferContent,
        string UserLabel,
        decimal Amount,
        decimal FeeAmount,
        decimal NetAmount,
        string BankName,
        string MaskedAccountNumber,
        string AccountHolderName,
        DateTime CreatedAt);
}

[Serializable]
public sealed class AdminWithdrawalRequestJobArgs
{
    public Guid WithdrawalRequestId { get; set; }
}

public class AdminWithdrawalRequestJob :
    IAsyncBackgroundJob<AdminWithdrawalRequestJobArgs>,
    ITransientDependency
{
    private readonly AdminWithdrawalRequestNotifier _notifier;

    public AdminWithdrawalRequestJob(AdminWithdrawalRequestNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task ExecuteAsync(AdminWithdrawalRequestJobArgs args) =>
        _notifier.ProcessAsync(args.WithdrawalRequestId);
}
