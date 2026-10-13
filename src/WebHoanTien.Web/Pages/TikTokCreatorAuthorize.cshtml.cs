using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
public class TikTokCreatorAuthorizeModel : PageModel
{
    public IActionResult OnGet() => LocalRedirect("/tiktok-affiliate");
    public IActionResult OnPostAuthorize() => LocalRedirect("/tiktok-affiliate");
}