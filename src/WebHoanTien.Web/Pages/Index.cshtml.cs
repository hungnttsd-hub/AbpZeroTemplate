using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    private readonly ICurrentUser _currentUser;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IDistributedCache _cache;

    [BindProperty] public string LinkUrl { get; set; } = string.Empty;
    public string? Error { get; set; }

    public IndexModel(IAffiliateLinkAppService links, ICurrentUser currentUser, IDataProtectionProvider dataProtection, IDistributedCache cache)
    {
        _links = links; _currentUser = currentUser; _cache = cache;
        _protector = dataProtection.CreateProtector("WebHoanTien.PendingAffiliate.v1").ToTimeLimitedDataProtector();
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostPrepareAsync()
    {
        var validation = await _links.ValidateAsync(new ValidateAffiliateUrlInput { Url = LinkUrl });
        if (!validation.IsValid)
        {
            Error = validation.Error;
            return Page();
        }
        if (_currentUser.IsAuthenticated)
        {
            var result = await _links.CreateAsync(new CreateAffiliateLinkInput { Url = LinkUrl });
            return RedirectToPage("/LinkResult", new { id = result.Id });
        }

        var nonce = Guid.NewGuid().ToString("N");
        var payload = JsonSerializer.Serialize(new PendingAffiliateAction(LinkUrl, nonce));
        Response.Cookies.Append("wht.pending", _protector.Protect(payload, TimeSpan.FromMinutes(20)), new CookieOptions
        {
            HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromMinutes(20), IsEssential = true
        });
        await _cache.SetStringAsync("affiliate:pending:" + nonce, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20) });
        return Redirect("/Account/Login?returnUrl=%2FPendingAffiliate");
    }

    public sealed record PendingAffiliateAction(string Url, string Nonce);
}
