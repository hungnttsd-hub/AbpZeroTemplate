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
public class TikTokCreatorAuthorizeModel : PageModel
{
    private readonly ITikTokAffiliateService _service;
    private readonly TikTokCreatorSelection _selection;
    public string? Error { get; private set; }

    public TikTokCreatorAuthorizeModel(ITikTokAffiliateService service, TikTokCreatorSelection selection)
    { _service = service; _selection = selection; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TikTokCreatorSelection.IsLocal(await _selection.GetPendingAsync()))
            return LocalRedirect("/tiktok-affiliate");
        if (await _service.GetCreatorProfile() is not null) return LocalRedirect("/tiktok-affiliate");
        return Page();
    }

    public async Task<IActionResult> OnPostAuthorizeAsync()
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!TikTokCreatorSelection.IsLocal(await _selection.GetPendingAsync()))
            return LocalRedirect("/tiktok-affiliate");
        try
        {
            await _service.ConnectCreator();
            TempData["TikTokConnectionNotice"] = "Kết nối tài khoản thành công.";
            return LocalRedirect("/tiktok-affiliate/authorization-success");
        }
        catch (UserFriendlyException exception)
        {
            Error = exception.Message;
            return Page();
        }
    }
}
