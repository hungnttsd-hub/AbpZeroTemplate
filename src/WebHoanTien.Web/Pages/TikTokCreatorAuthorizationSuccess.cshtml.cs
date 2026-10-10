using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Auditing;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Pages;

[Authorize(TikTokAffiliateAccess.Policy)]
[DisableAuditing]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TikTokCreatorAuthorizationSuccessModel : PageModel
{
    private readonly TikTokCreatorSelection _selection;

    public TikTokCreatorAuthorizationSuccessModel(TikTokCreatorSelection selection) => _selection = selection;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _selection.IsLocalConnectedAsync())
            return LocalRedirect("/tiktok-affiliate");
        TempData.Keep("TikTokConnectionNotice");
        return Page();
    }
}
