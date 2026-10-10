using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
[DisableAuditing]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TikTokAffiliateModel : PageModel
{
    private readonly ITikTokAffiliateService _service;
    private readonly ILogger<TikTokAffiliateModel> _logger;
    public TikTokAffiliateIntegrationDto Integration { get; private set; } = new("RioHub", false, null, false);
    public TikTokCreatorDto? Creator { get; private set; }
    public string InitialStateJson { get; private set; } = "{}";
    public string? ConnectionNotice { get; private set; }
    public string? ConnectionError { get; private set; }
    public string? CreatorUsername { get; private set; }

    public TikTokAffiliateModel(ITikTokAffiliateService service, ILogger<TikTokAffiliateModel> logger)
    { _service = service; _logger = logger; }

    public async Task OnGetAsync(string? creatorUsername)
    {
        ConnectionNotice = TempData["TikTokConnectionNotice"] as string;
        if (!string.IsNullOrWhiteSpace(creatorUsername))
        {
            try
            {
                CreatorUsername = TikTokCreatorSelection.ValidateUsername(creatorUsername);
                ConnectionNotice ??= "Bấm Kết nối tài khoản để tiếp tục cấp quyền TikTok trên domain này.";
            }
            catch (UserFriendlyException exception) { ConnectionError = exception.Message; }
        }
        Integration = await _service.GetIntegrationInfo();
        try { Creator = await _service.GetCreatorProfile(); }
        catch (UserFriendlyException exception) { ConnectionError = exception.Message; }
        catch (Exception exception)
        {
            _logger.LogWarning("Cannot load TikTok Creator profile ({Category}).", exception.GetType().Name);
            ConnectionError = "Chưa xác minh được kết nối TikTok. Vui lòng thử kết nối lại.";
        }
        var links = await _service.GetGeneratedLinks();
        IReadOnlyList<TikTokAffiliateOrderDto> orders = Array.Empty<TikTokAffiliateOrderDto>();
        string? ordersError = null;
        try { orders = await _service.SearchAffiliateOrders(); }
        catch (Exception exception)
        {
            _logger.LogWarning("Cannot load RioHub order ledger ({Category}).", exception.GetType().Name);
            ordersError = "Chưa tải được đơn hàng. Vui lòng thử lại sau.";
        }
        InitialStateJson = JsonSerializer.Serialize(new { integration = Integration, creator = Creator, links, orders, ordersError },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Response.Headers.CacheControl = "no-store";
    }

    public Task<IActionResult> OnPostProductAsync(string productUrl) => Execute(() => _service.GetProduct(productUrl));
    public Task<IActionResult> OnPostDeleteLinkAsync(string trackingId) => Execute(async () =>
    {
        await _service.DeleteGeneratedLink(trackingId);
        return await _service.GetGeneratedLinks();
    });
    public Task<IActionResult> OnPostGenerateAsync(string productUrlOrId) => Execute(async () => new
    {
        link = await _service.GenerateAffiliateLink(productUrlOrId), links = await _service.GetGeneratedLinks()
    });
    public Task<IActionResult> OnGetLinksAsync() => Execute(() => _service.GetGeneratedLinks());
    public Task<IActionResult> OnGetOrdersAsync() => Execute(() => _service.SearchAffiliateOrders());

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        await _service.DisconnectCreator();
        TempData["TikTokConnectionNotice"] = "Đã ngắt kết nối tài khoản tại CatBack.";
        return LocalRedirect("/tiktok-affiliate");
    }

    // Razor Pages supplies cookie authentication and antiforgery validation for POSTs.
    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        Response.Headers.CacheControl = "no-store";
        if (!ModelState.IsValid) return BadRequest(new { error = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
        try { return new JsonResult(new { data = await action() }); }
        catch (UserFriendlyException exception) { return BadRequest(new { error = exception.Message }); }
        catch (Exception exception)
        {
            _logger.LogWarning("RioHub workspace request failed ({Category}).", exception.GetType().Name);
            return StatusCode(502, new { error = "Không thể hoàn tất thao tác. Vui lòng thử lại sau." });
        }
    }
}
