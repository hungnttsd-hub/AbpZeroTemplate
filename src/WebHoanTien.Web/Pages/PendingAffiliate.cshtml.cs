using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class PendingAffiliateModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PendingAffiliateModel> _logger;
    public PendingAffiliateModel(IAffiliateLinkAppService links, IDataProtectionProvider dataProtection,
        IDistributedCache cache, ILogger<PendingAffiliateModel> logger)
    { _links = links; _cache = cache; _logger = logger; _protector = dataProtection.CreateProtector("WebHoanTien.PendingAffiliate.v1").ToTimeLimitedDataProtector(); }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Request.Cookies.TryGetValue("wht.pending", out var protectedValue)) return RedirectToPage("/Index");
        IndexModel.PendingAffiliateAction? action = null;
        try
        {
            var value = _protector.Unprotect(protectedValue, out _);
            action = JsonSerializer.Deserialize<IndexModel.PendingAffiliateAction>(value);
            if (action is null || await _cache.GetStringAsync("affiliate:pending:" + action.Nonce) is null) return RedirectToPage("/Index");
            await _cache.RemoveAsync("affiliate:pending:" + action.Nonce);
            Response.Cookies.Delete("wht.pending");
            var result = await _links.CreateAsync(new CreateAffiliateLinkInput
            {
                Url = action.Url,
                TargetType = action.TargetType
            });
            TempData["AffiliateCreatedLinkId"] = result.Id.ToString();
            TempData["AffiliateLinkSuccess"] = IndexModel.SuccessMessageFor(result);
            TempData["AffiliateLinkUrl"] = action.Url;
            return RedirectToPage("/Index");
        }
        catch (UserFriendlyException exception)
        {
            Response.Cookies.Delete("wht.pending");
            RestorePendingContext(action);
            TempData["AffiliateLinkError"] = exception.Message;
            return RedirectToPage("/Index");
        }
        catch (BusinessException exception)
        {
            _logger.LogWarning(exception, "Pending affiliate link creation failed. Code: {ErrorCode}; Reason: {Reason}.",
                exception.Code, exception.Data["Reason"]);
            Response.Cookies.Delete("wht.pending");
            RestorePendingContext(action);
            TempData["AffiliateLinkError"] = AffiliateLinkErrorUi.MessageFor(exception);
            return RedirectToPage("/Index");
        }
        catch (System.Exception exception)
        {
            _logger.LogError(exception, "Unable to complete pending affiliate link creation.");
            Response.Cookies.Delete("wht.pending");
            RestorePendingContext(action);
            TempData["AffiliateLinkError"] = AffiliateLinkErrorUi.DefaultMessage;
            return RedirectToPage("/Index");
        }
    }

    private void RestorePendingContext(IndexModel.PendingAffiliateAction? action)
    {
        if (action is null) return;
        TempData["AffiliateLinkUrl"] = action.Url;
    }
}
