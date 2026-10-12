using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages.Admin.TikTokOrders;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Orders)]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel : PageModel
{
    private readonly IAdminTikTokOperationsAppService _operations;
    private readonly IAffiliateOrderAppService _orders;
    private readonly ICurrentTenant _tenant;
    private readonly IAuthorizationService _authorization;
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime FromDate { get; set; } = DateTime.UtcNow.AddHours(7).Date.AddDays(-30);
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime ToDate { get; set; } = DateTime.UtcNow.AddHours(7).Date;
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public PagedResultDto<AffiliateOrderDto> Orders { get; private set; } = new();
    public string? Error { get; private set; }
    public bool CanSync { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Orders.TotalCount / 50d));

    public IndexModel(IAdminTikTokOperationsAppService operations, IAffiliateOrderAppService orders,
        ICurrentTenant tenant, IAuthorizationService authorization)
    { _operations = operations; _orders = orders; _tenant = tenant; _authorization = authorization; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        if (!ModelState.IsValid || !HasValidDates) { Error = "Chọn khoảng ngày hợp lệ, tối đa 90 ngày và không ở tương lai."; await LoadAsync(); return Page(); }
        try
        {
            var result = await _operations.SyncOrdersAsync(Window());
            TempData["TikTokNotice"] = $"Đọc {result.FetchedOrders} đơn: thêm {result.InsertedOrders}, cập nhật {result.UpdatedOrders}, " +
                $"chưa khớp {result.UnmatchedOrders}, cần kiểm tra {result.NeedsReview}. {result.Notice}";
            return RedirectToPage(new { FromDate = FromDate.ToString("yyyy-MM-dd"), ToDate = ToDate.ToString("yyyy-MM-dd") });
        }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        catch (AbpAuthorizationException) { return Forbid(); }
        await LoadAsync();
        return Page();
    }

    private bool HasValidDates => FromDate.Year >= 2026 && FromDate <= ToDate
        && ToDate - FromDate <= TimeSpan.FromDays(89) && ToDate.Date <= DateTime.UtcNow.AddHours(7).Date;

    private TikTokManualSyncInput Window() => new()
    {
        FromUtc = new DateTimeOffset(DateTime.SpecifyKind(FromDate.Date, DateTimeKind.Unspecified), TimeSpan.FromHours(7)),
        ToUtc = new DateTimeOffset(DateTime.SpecifyKind(ToDate.Date.AddDays(1), DateTimeKind.Unspecified), TimeSpan.FromHours(7))
    };

    private async Task LoadAsync()
    {
        CanSync = await _authorization.IsGrantedAsync(WebHoanTienPermissions.Admin.Sync);
        if (!ModelState.IsValid || !HasValidDates)
        { Error ??= "Chọn khoảng ngày tối đa 90 ngày."; return; }
        var window = Window();
        PageNumber = Math.Clamp(PageNumber, 1, 100_000);
        Orders = await _orders.GetListAsync(new AffiliateOrderListInput
        {
            Platform = AffiliatePlatform.TikTok, From = window.FromUtc.UtcDateTime, To = window.ToUtc.UtcDateTime,
            SkipCount = (PageNumber - 1) * 50, MaxResultCount = 50
        });
    }
}
