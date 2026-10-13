using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TikTokAffiliateModel : PageModel
{
    private readonly ITikTokAffiliateService _service;
    private readonly IAdminTikTokOperationsAppService _operations;
    private readonly IAuthorizationService _authorization;
    private readonly ICurrentTenant _tenant;
    private readonly ILogger<TikTokAffiliateModel> _logger;
    public TikTokAffiliateIntegrationDto Integration { get; private set; } = new(false);
    public TikTokCreatorDto? Creator { get; private set; }
    public string InitialStateJson { get; private set; } = "{}";
    public string? ConnectionError { get; private set; }
    public bool CanManageConnection { get; private set; }
    public bool CanSync { get; private set; }
    public bool IsAdminOrderView { get; private set; }
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime FromDate { get; set; } = DateTime.UtcNow.AddHours(7).Date.AddDays(-30);
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime ToDate { get; set; } = DateTime.UtcNow.AddHours(7).Date;
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? StatusGroup { get; set; }
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }

    public TikTokAffiliateModel(ITikTokAffiliateService service, IAdminTikTokOperationsAppService operations,
        IAuthorizationService authorization, ICurrentTenant tenant, ILogger<TikTokAffiliateModel> logger)
    { _service = service; _operations = operations; _authorization = authorization; _tenant = tenant; _logger = logger; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        await LoadPermissionsAsync();
        Integration = await _service.GetIntegrationInfo();
        try { Creator = await _service.GetCreatorProfile(); }
        catch (UserFriendlyException ex) { ConnectionError = ex.Message; }
        catch (Exception ex)
        {
            _logger.LogWarning("Cannot load TikTok Creator profile: {ErrorType}", ex.GetType().Name);
            ConnectionError = "Chưa đọc được hồ sơ Creator từ TikTok. Hãy thử tải lại hoặc kiểm tra kết nối.";
        }
        IReadOnlyList<TikTokGeneratedLinkDto> links = Array.Empty<TikTokGeneratedLinkDto>();
        try { links = await _service.GetGeneratedLinks(); }
        catch (UserFriendlyException ex) { ConnectionError ??= ex.Message; }
        var orders = new PagedResultDto<AffiliateOrderDto>();
        string? ordersError = null;
        try { orders = await LoadOrdersAsync(); }
        catch (UserFriendlyException ex) { ordersError = ex.Message; }
        InitialStateJson = JsonSerializer.Serialize(new
        {
            integration = Integration, creator = Creator, links, orders = OrdersPayload(orders), ordersError,
            isAdminOrderView = IsAdminOrderView, initialTab = Tab is "orders" or "links" ? Tab : "overview"
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Page();
    }

    public Task<IActionResult> OnPostProductAsync(string productUrl) => Execute(() => _service.GetProduct(productUrl));
    public Task<IActionResult> OnPostGenerateAsync(string productUrlOrId) => Execute(async () => new
    {
        link = await _service.GenerateAffiliateLink(productUrlOrId), links = await _service.GetGeneratedLinks()
    });
    public Task<IActionResult> OnPostDeleteLinkAsync(string trackingId) => Execute(async () =>
    {
        await _service.DeleteGeneratedLink(trackingId);
        return await _service.GetGeneratedLinks();
    });
    public Task<IActionResult> OnGetLinksAsync() => Execute(() => _service.GetGeneratedLinks());
    public Task<IActionResult> OnGetOrdersAsync() => Execute(async () => OrdersPayload(await LoadOrdersAsync()));

    public async Task<IActionResult> OnPostSyncAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        await LoadPermissionsAsync();
        if (!CanSync) return Forbid();
        return await Execute(async () =>
        {
            ValidateDates();
            var result = await _operations.SyncOrdersAsync(new TikTokManualSyncInput
            { FromUtc = WindowStart, ToUtc = WindowEnd });
            PageNumber = 1;
            return new { result, orders = OrdersPayload(await LoadOrdersAsync()) };
        });
    }

    public IActionResult OnPostDisconnect() => StatusCode(410, new { error = "Đã bỏ kết nối cục bộ. Admin quản lý Creator chung tại trang TikTok Creator." });

    private async Task LoadPermissionsAsync()
    {
        IsAdminOrderView = await _authorization.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders);
        CanManageConnection = !_tenant.Id.HasValue && User.IsInRole("admin")
            && await _authorization.IsGrantedAsync(WebHoanTienPermissions.Admin.Settings);
        CanSync = !_tenant.Id.HasValue && User.IsInRole("admin") && IsAdminOrderView
            && await _authorization.IsGrantedAsync(WebHoanTienPermissions.Admin.Sync);
    }

    private DateTimeOffset WindowStart => new(DateTime.SpecifyKind(FromDate.Date, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
    private DateTimeOffset WindowEnd => new(DateTime.SpecifyKind(ToDate.Date.AddDays(1), DateTimeKind.Unspecified), TimeSpan.FromHours(7));
    private void ValidateDates()
    {
        if (FromDate.Year < 2026 || FromDate > ToDate || ToDate - FromDate > TimeSpan.FromDays(89)
            || ToDate.Date > DateTime.UtcNow.AddHours(7).Date)
            throw new UserFriendlyException("Chọn khoảng ngày đặt đơn hợp lệ, tối đa 90 ngày và không ở tương lai.");
    }
    private Task<PagedResultDto<AffiliateOrderDto>> LoadOrdersAsync()
    {
        ValidateDates();
        PageNumber = Math.Clamp(PageNumber, 1, 100_000);
        return _service.SearchAffiliateOrders(new AffiliateOrderListInput
        {
            From = WindowStart.UtcDateTime, To = WindowEnd.UtcDateTime, StatusGroup = StatusGroup,
            SkipCount = (PageNumber - 1) * 50, MaxResultCount = 50
        });
    }
    private object OrdersPayload(PagedResultDto<AffiliateOrderDto> orders) => new
    {
        items = orders.Items, totalCount = orders.TotalCount, pageNumber = PageNumber,
        totalPages = Math.Max(1, (int)Math.Ceiling(orders.TotalCount / 50d))
    };

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        if (_tenant.Id.HasValue) return Forbid();
        Response.Headers.CacheControl = "no-store";
        if (!ModelState.IsValid) return BadRequest(new { error = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
        try { return new JsonResult(new { data = await action() }); }
        catch (AbpAuthorizationException) { return Forbid(); }
        catch (UserFriendlyException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogWarning("TikTok API workspace request failed: {ErrorType}", ex.GetType().Name);
            return StatusCode(502, new { error = "Không thể hoàn tất thao tác TikTok. Vui lòng thử lại sau." });
        }
    }
}
