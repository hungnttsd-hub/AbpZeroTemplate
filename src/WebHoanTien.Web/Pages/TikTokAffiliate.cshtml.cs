using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public TikTokAffiliateIntegrationDto Integration { get; private set; } = new("Mock", true, null);
    public string InitialStateJson { get; private set; } = "{}";
    public TikTokAffiliateModel(ITikTokAffiliateService service) => _service = service;

    public async Task OnGetAsync()
    {
        Integration = await _service.GetIntegrationInfo();
        var creator = await _service.GetCreatorProfile();
        var links = await _service.GetGeneratedLinks();
        var orders = await _service.SearchAffiliateOrders();
        InitialStateJson = JsonSerializer.Serialize(new { integration = Integration, creator, links, orders },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Response.Headers.CacheControl = "no-store";
    }

    public Task<IActionResult> OnPostConnectAsync() => Execute(async () => new
    {
        creator = await _service.ConnectCreator(), orders = await _service.SearchAffiliateOrders()
    });
    public Task<IActionResult> OnPostProductAsync(string productUrl) => Execute(() => _service.GetProduct(productUrl));
    public Task<IActionResult> OnPostDisconnectAsync() => Execute(async () =>
    {
        await _service.DisconnectCreator();
        return new { creator = await _service.GetCreatorProfile(), orders = await _service.SearchAffiliateOrders() };
    });
    public Task<IActionResult> OnPostDeleteLinkAsync(string productId) => Execute(async () =>
    {
        await _service.DeleteGeneratedLink(productId);
        return await _service.GetGeneratedLinks();
    });
    public Task<IActionResult> OnPostGenerateAsync(string productId) => Execute(async () => new
    {
        link = await _service.GenerateAffiliateLink(productId), links = await _service.GetGeneratedLinks()
    });
    public Task<IActionResult> OnGetLinksAsync() => Execute(() => _service.GetGeneratedLinks());
    public Task<IActionResult> OnGetOrdersAsync() => Execute(() => _service.SearchAffiliateOrders());

    // Razor Pages supplies cookie authentication and antiforgery validation for POSTs.
    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        Response.Headers.CacheControl = "no-store";
        if (!ModelState.IsValid) return BadRequest(new { error = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
        try { return new JsonResult(new { data = await action() }); }
        catch (UserFriendlyException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
