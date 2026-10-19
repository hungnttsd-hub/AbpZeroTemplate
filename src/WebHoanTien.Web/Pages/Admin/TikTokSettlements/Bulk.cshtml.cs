using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.Permissions;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages.Admin.TikTokSettlements;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Orders)]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BulkModel : PageModel
{
    private readonly IAdminTikTokSettlementAppService _settlements;
    private readonly IDataProtectionProvider _protection;
    private readonly IDistributedCache _cache;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;

    [BindProperty, StringLength(1000, MinimumLength = 10)]
    public string Reason { get; set; } = "Duyệt các đơn TikTok đã thanh toán theo dữ liệu đối soát đã đồng bộ.";
    [BindProperty] public string? PreviewToken { get; set; }
    [BindProperty] public bool ConfirmApproval { get; set; }
    public List<TikTokSettlementPreview> Orders { get; private set; } = new();
    public TikTokBulkSettlementResult? Result { get; private set; }
    public string? Error { get; private set; }
    public bool HasPreview { get; private set; }
    public int EligibleCount => Orders.Count(x => x.CanApprove);
    public decimal TotalTax => Orders.Where(x => x.CanApprove).Sum(x => x.TaxAmount);
    public decimal TotalCredit => Orders.Where(x => x.CanApprove).Sum(x => x.UserCommission);

    public BulkModel(IAdminTikTokSettlementAppService settlements, IDataProtectionProvider protection,
        IDistributedCache cache, ICurrentUser user, ICurrentTenant tenant)
    { _settlements = settlements; _protection = protection; _cache = cache; _user = user; _tenant = tenant; }

    public IActionResult OnGet() => _tenant.Id.HasValue ? Forbid() : Page();

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        PreviewToken = null;
        ConfirmApproval = false;
        ModelState.Remove(nameof(PreviewToken));
        ModelState.Remove(nameof(ConfirmApproval));
        if (!ModelState.IsValid) return Page();
        try
        {
            Reason = Reason.Trim();
            Orders = await _settlements.PreviewPaidOrdersAsync(Reason);
            HasPreview = true;
            var plans = Orders.Where(x => x.CanApprove)
                .Select(x => new TikTokBulkSettlementPlan(x.OrderId, x.ExternalOrderId, x.PlanHash)).ToList();
            if (plans.Count > 0)
            {
                PreviewToken = Guid.NewGuid().ToString("N");
                var signed = Protector().Protect(JsonSerializer.Serialize(new SignedPreview(plans, Reason)), TimeSpan.FromMinutes(10));
                await _cache.SetStringAsync(CacheKey(PreviewToken), signed,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, HttpContext.RequestAborted);
            }
        }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        try
        {
            if (!ConfirmApproval || !Guid.TryParseExact(PreviewToken, "N", out _))
                throw new UserFriendlyException("Hãy xem trước và đánh dấu xác nhận duyệt toàn bộ đơn đủ điều kiện.");
            var key = CacheKey(PreviewToken!);
            var protectedPreview = await _cache.GetStringAsync(key, HttpContext.RequestAborted);
            if (protectedPreview is null) throw new CryptographicException();
            var signed = JsonSerializer.Deserialize<SignedPreview>(Protector().Unprotect(protectedPreview));
            if (signed is null) throw new CryptographicException();
            Reason = signed.Reason;
            Result = await _settlements.ApprovePaidOrdersAsync(signed.Plans, signed.Reason);
            await _cache.RemoveAsync(key, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        { Error = "Bản xem trước đã hết hạn hoặc không thuộc admin hiện tại. Hãy xem trước lại."; }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        PreviewToken = null;
        ConfirmApproval = false;
        ModelState.Clear();
        return Page();
    }

    private string CacheKey(string token) => $"CatBack.TikTokBulkSettlement:{_user.GetId():N}:{token}";
    private ITimeLimitedDataProtector Protector() => _protection.CreateProtector(
        "CatBack.TikTokBulkSettlement.Preview.v1", _user.GetId().ToString("N")).ToTimeLimitedDataProtector();
    public sealed record SignedPreview(List<TikTokBulkSettlementPlan> Plans, string Reason);
}
