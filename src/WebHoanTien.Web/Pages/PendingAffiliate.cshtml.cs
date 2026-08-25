using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class PendingAffiliateModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IDistributedCache _cache;
    public PendingAffiliateModel(IAffiliateLinkAppService links, IDataProtectionProvider dataProtection, IDistributedCache cache)
    { _links = links; _cache = cache; _protector = dataProtection.CreateProtector("WebHoanTien.PendingAffiliate.v1").ToTimeLimitedDataProtector(); }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Request.Cookies.TryGetValue("wht.pending", out var protectedValue)) return RedirectToPage("/Index");
        try
        {
            var value = _protector.Unprotect(protectedValue, out _);
            var action = JsonSerializer.Deserialize<IndexModel.PendingAffiliateAction>(value);
            if (action is null || await _cache.GetStringAsync("affiliate:pending:" + action.Nonce) is null) return RedirectToPage("/Index");
            await _cache.RemoveAsync("affiliate:pending:" + action.Nonce);
            Response.Cookies.Delete("wht.pending");
            var result = await _links.CreateAsync(new CreateAffiliateLinkInput { Url = action.Url });
            TempData["AffiliateCreatedLinkId"] = result.Id.ToString();
            TempData["AffiliateLinkSuccess"] = result.WasRestored
                ? "Link đã được đưa trở lại danh sách của bạn."
                : result.IsExisting
                    ? "Link này đã có trong danh sách của bạn."
                    : "Link mua hàng đã được thêm vào danh sách của bạn.";
            return RedirectToPage("/Index");
        }
        catch (UserFriendlyException exception)
        {
            Response.Cookies.Delete("wht.pending");
            TempData["AffiliateLinkError"] = exception.Message;
            return RedirectToPage("/Index");
        }
        catch
        {
            Response.Cookies.Delete("wht.pending");
            TempData["AffiliateLinkError"] = "Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.";
            return RedirectToPage("/Index");
        }
    }
}
