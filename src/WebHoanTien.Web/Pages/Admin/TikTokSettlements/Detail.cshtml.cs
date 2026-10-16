using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
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
public class DetailModel : PageModel
{
    private readonly IAdminTikTokSettlementAppService _settlements;
    private readonly IDataProtectionProvider _protection;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    [BindProperty(SupportsGet = true)] public Guid OrderId { get; set; }
    [BindProperty] public TikTokSettlementInput Input { get; set; } = new();
    [BindProperty] public string? PreviewToken { get; set; }
    [BindProperty] public bool ConfirmApproval { get; set; }
    public TikTokSettlementPreview Preview { get; private set; } = new();
    public bool ShowConfirmation { get; private set; }
    public string? Error { get; private set; }
    public string? Notice { get; private set; }

    public DetailModel(IAdminTikTokSettlementAppService settlements, IDataProtectionProvider protection, ICurrentUser user, ICurrentTenant tenant)
    { _settlements = settlements; _protection = protection; _user = user; _tenant = tenant; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        Headers();
        Notice = TempData["TikTokSettlementNotice"] as string;
        Preview = await _settlements.PreviewAsync(OrderId);
        Input = Preview.Input;
        return Page();
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        Headers();
        if (!ModelState.IsValid)
        { Preview = await _settlements.PreviewAsync(OrderId); return Page(); }
        try
        {
            Input.Reason = Input.Reason?.Trim();
            if (Input.Reason is not { Length: >= 10 and <= 1000 } || Input.Reason.Any(char.IsControl))
                throw new UserFriendlyException("Nhập lý do duyệt từ 10–1.000 ký tự để xem trước và xác nhận.");
            Preview = await _settlements.PreviewAsync(OrderId, Input);
            if (Preview.CanApprove)
            {
                PreviewToken = Protector().Protect(JsonSerializer.Serialize(new SignedPreview(OrderId, Input, Preview.PlanHash)), TimeSpan.FromMinutes(10));
                ShowConfirmation = true;
            }
        }
        catch (UserFriendlyException ex)
        { Error = ex.Message; Preview = await _settlements.PreviewAsync(OrderId); }
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        Headers();
        try
        {
            if (!ConfirmApproval || PreviewToken is not { Length: > 0 and <= 8192 })
                throw new UserFriendlyException("Hãy xem trước và đánh dấu xác nhận cộng ví.");
            var signed = JsonSerializer.Deserialize<SignedPreview>(Protector().Unprotect(PreviewToken));
            if (signed is null || signed.OrderId != OrderId) throw new CryptographicException();
            var result = await _settlements.ApproveAsync(OrderId, signed.Input, signed.PlanHash);
            TempData["TikTokSettlementNotice"] = result.AlreadySettled
                ? "Đơn đã được duyệt trước đó. Không cộng tiền lần nữa."
                : $"Đã duyệt đối soát và ghi nhận {Money(result.CreditedAmount)} vào ví người nhận.";
            return RedirectToPage(new { OrderId });
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        { Error = "Bản xem trước đã hết hạn hoặc không khớp phiên admin. Hãy xem trước lại."; }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        catch (AbpDbConcurrencyException) { Error = "Đơn vừa được cập nhật bởi thao tác khác. Chưa ghi nhận khoản duyệt này; hãy xem trước lại."; }
        Preview = await _settlements.PreviewAsync(OrderId);
        Input = Preview.Input;
        ModelState.Clear();
        PreviewToken = null; ConfirmApproval = false;
        return Page();
    }

    private ITimeLimitedDataProtector Protector() => _protection.CreateProtector("CatBack.TikTokSettlement.Preview.v1", _user.GetId().ToString("N")).ToTimeLimitedDataProtector();
    private void Headers()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
    public static string Precise(decimal value) => value.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
    public static string LocalTime(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm:ss") : "—";
    public sealed record SignedPreview(Guid OrderId, TikTokSettlementInput Input, string PlanHash);
}
