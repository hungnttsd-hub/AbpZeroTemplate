using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Auditing;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
[DisableAuditing]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TikTokCreatorDemoModel : PageModel
{
    private readonly ITikTokAffiliateService _service;
    private readonly IConfiguration _configuration;
    public bool ShowDemoLabel => _configuration.GetValue("TikTokAffiliate:ShowDemoLabel", true);
    public string Step { get; private set; } = "consent";
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public bool Consent { get; set; }

    public TikTokCreatorDemoModel(ITikTokAffiliateService service, IConfiguration configuration)
    { _service = service; _configuration = configuration; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await IsDemoAsync()) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAuthorizeAsync()
    {
        if (!await IsDemoAsync()) return NotFound();
        Step = "consent";
        if (!ModelState.IsValid || !Consent)
        {
            ErrorMessage = "Vui lòng xác nhận đồng ý để tiếp tục cấp quyền demo.";
            return Page();
        }

        try
        {
            await _service.ConnectCreator();
            TempData["TikTokDemoAuthorized"] = true;
            return RedirectToPage("/TikTokCreatorDemo", "Result");
        }
        catch (UserFriendlyException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnGetResultAsync()
    {
        if (!await IsDemoAsync()) return NotFound();
        if (TempData["TikTokDemoAuthorized"] is true && await _service.GetCreatorProfile() is not null)
            Step = "success";
        else
            return LocalRedirect("/tiktok-affiliate");
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (!await IsDemoAsync()) return NotFound();
        TempData["TikTokDemoAuthMessage"] = "Bạn đã hủy cấp quyền demo. Trạng thái kết nối trước đó được giữ nguyên.";
        return LocalRedirect("/tiktok-affiliate");
    }

    private async Task<bool> IsDemoAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'self'; script-src 'self'; img-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
        return (await _service.GetIntegrationInfo()).IsDemo;
    }
}
