using AbpIoTemplateProject.Localization;
using System;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Http;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace AbpIoTemplateProject.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class AbpIoTemplateProjectPageModel : AbpPageModel
{
    private const string CartCookieName = "aq_cart";

    protected AbpIoTemplateProjectPageModel()
    {
        LocalizationResourceType = typeof(AbpIoTemplateProjectResource);
    }

    protected string GetOrCreateCartKey()
    {
        if (Request.Cookies.TryGetValue(CartCookieName, out var cartKey) &&
            !string.IsNullOrWhiteSpace(cartKey))
        {
            return cartKey;
        }

        cartKey = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(
            CartCookieName,
            cartKey,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        return cartKey;
    }

    protected async Task LoadCartSummaryAsync(ICartAppService cartAppService)
    {
        var cart = await cartAppService.GetAsync(GetOrCreateCartKey());
        ViewData["StoreCartCount"] = cart.TotalQuantity;
        ViewData["StoreCartTotal"] = cart.GrandTotal;
    }
}
