using System;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.SettingManagement;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Admin;
using WebHoanTien.Settings;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Operations;

[UnitOfWork(IsDisabled = true)]
public class ShopeeAutoApprovalJob : ITransientDependency
{
    public const string RecurringJobId = "shopee-completed-seven-days-daily";
    public static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "CatBack Vietnam", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
    private readonly IAdminShopeeSettlementApprovalAppService _settlements;
    private readonly ISettingManager _settings;
    private readonly IUnitOfWorkManager _uow;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IUserClaimsPrincipalFactory<IdentityUser> _claims;
    private readonly ICurrentPrincipalAccessor _principal;
    private readonly ICurrentTenant _tenant;
    private readonly IClock _clock;
    private readonly ILogger<ShopeeAutoApprovalJob> _logger;

    public ShopeeAutoApprovalJob(IAdminShopeeSettlementApprovalAppService settlements,
        ISettingManager settings, IUnitOfWorkManager uow, IRepository<IdentityUser, Guid> users,
        IUserClaimsPrincipalFactory<IdentityUser> claims, ICurrentPrincipalAccessor principal,
        ICurrentTenant tenant, IClock clock, ILogger<ShopeeAutoApprovalJob> logger)
    {
        _settlements = settlements; _settings = settings; _uow = uow; _users = users;
        _claims = claims; _principal = principal; _tenant = tenant; _clock = clock; _logger = logger;
    }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(bool isManual, Guid? requestedBy)
    {
        using var host = _tenant.Change(null);
        Guid actorId;
        using (var read = _uow.Begin(requiresNew: true, isTransactional: false))
        {
            // Re-read at execution time so switching off also disables queued scheduled runs.
            var enabled = string.Equals(await _settings.GetOrNullGlobalAsync(ShopeeAutoApprovalSettings.Enabled),
                "true", StringComparison.OrdinalIgnoreCase);
            if (!isManual && !enabled) return;
            var configuredBy = await _settings.GetOrNullGlobalAsync(ShopeeAutoApprovalSettings.ApprovedBy);
            actorId = isManual ? requestedBy ?? Guid.Empty : Guid.TryParse(configuredBy, out var id) ? id : Guid.Empty;
            await read.CompleteAsync();
        }

        var run = new ShopeeAutoApprovalRunDto { StartedAtUtc = _clock.Now, IsManual = isManual };
        await SaveRunAsync(run);
        try
        {
            System.Security.Claims.ClaimsPrincipal claims;
            using (var read = _uow.Begin(requiresNew: true, isTransactional: false))
            {
                var user = actorId == Guid.Empty ? null : await _users.FindAsync(actorId);
                if (user is null || !user.IsActive || user.TenantId.HasValue ||
                    (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow))
                    throw new UserFriendlyException("Tài khoản cho phép duyệt không còn hoạt động. Hãy cấu hình lại job bằng tài khoản admin hợp lệ.");
                claims = await _claims.CreateAsync(user);
                await read.CompleteAsync();
            }
            // Use fresh claims for the real admin; the app service still checks Admin.Orders.
            using (_principal.Change(claims))
            {
                var result = await _settlements.ApproveCompletedOlderThanSevenDaysAsync(isManual);
                run.ApprovedCount = result.ApprovedCount;
                run.SkippedCount = result.SkippedCount;
                run.CreditedUserCommission = result.CreditedUserCommission;
            }
            run.Status = "Succeeded";
            run.Message = $"Đã duyệt {run.ApprovedCount} đơn; bỏ qua {run.SkippedCount} bản ghi cần kiểm tra.";
        }
        catch (Exception exception)
        {
            run.Status = "Failed";
            run.Message = exception is UserFriendlyException friendly ? friendly.Message
                : "Lượt duyệt không hoàn tất. Kiểm tra lịch sử job trước khi chạy lại.";
            _logger.LogError(exception, "Shopee completed-order approval failed for admin {AdminId}.", actorId);
            throw;
        }
        finally
        {
            run.FinishedAtUtc = _clock.Now;
            await SaveRunAsync(run);
        }
    }

    private async Task SaveRunAsync(ShopeeAutoApprovalRunDto run)
    {
        using var work = _uow.Begin(requiresNew: true, isTransactional: true);
        await _settings.SetGlobalAsync(ShopeeAutoApprovalSettings.LastRun, JsonSerializer.Serialize(run));
        await work.CompleteAsync();
    }
}
