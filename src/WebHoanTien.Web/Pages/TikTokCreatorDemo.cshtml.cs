using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

// Preserve old bookmarks without rendering or granting a simulated Creator connection.
[Authorize(TikTokAffiliateAccess.Policy)]
public class TikTokCreatorDemoModel : PageModel
{
    public IActionResult OnGet() => LocalRedirect("/tiktok-affiliate");
    public IActionResult OnGetResult() => LocalRedirect("/tiktok-affiliate");
    public IActionResult OnPostAuthorize() => LocalRedirect("/tiktok-affiliate");
    public IActionResult OnPostCancel() => LocalRedirect("/tiktok-affiliate");
}
