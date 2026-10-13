using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
public class TikTokCreatorAuthorizationSuccessModel : PageModel
{
    public IActionResult OnGet() => LocalRedirect("/tiktok-affiliate");
}