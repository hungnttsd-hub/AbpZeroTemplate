using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.Permissions;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Orders)]
[RemoteService(IsEnabled = false)]
[UnitOfWork(IsDisabled = true)]
public class AdminTikTokSettlementAppService : WebHoanTienAppService, IAdminTikTokSettlementAppService
{
    private readonly TikTokSettlementService _settlements;
    private readonly ICurrentTenant _tenant;

    public AdminTikTokSettlementAppService(TikTokSettlementService settlements, ICurrentTenant tenant)
    { _settlements = settlements; _tenant = tenant; }

    private void RequireHostAdmin()
    {
        if (_tenant.Id.HasValue || !CurrentUser.IsAuthenticated || !CurrentUser.IsInRole("admin"))
            throw new AbpAuthorizationException("Chỉ admin hệ thống được đối soát TikTok.");
    }

    public Task<TikTokSettlementPreview> PreviewAsync(Guid orderId, TikTokSettlementInput? input = null)
    {
        RequireHostAdmin();
        return _settlements.PreviewAsync(orderId, input);
    }

    public Task<TikTokSettlementApprovalResult> ApproveAsync(Guid orderId, TikTokSettlementInput input, string expectedPlanHash)
    {
        RequireHostAdmin();
        return _settlements.ApproveAsync(orderId, input, expectedPlanHash, CurrentUser.GetId());
    }

    public Task<List<TikTokSettlementPreview>> PreviewPaidOrdersAsync(string reason)
    {
        RequireHostAdmin();
        return _settlements.PreviewPaidOrdersAsync(reason);
    }

    public Task<TikTokBulkSettlementResult> ApprovePaidOrdersAsync(List<TikTokBulkSettlementPlan> plans, string reason)
    {
        RequireHostAdmin();
        return _settlements.ApprovePaidOrdersAsync(plans, reason, CurrentUser.GetId());
    }
}
