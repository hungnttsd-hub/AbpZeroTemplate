using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Pages.Developer;

[Authorize(TikTokDeveloperAccess.Policy)]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TikTokModel : PageModel
{
    private readonly TikTokDeveloperService _developers;
    private readonly ICurrentUser _user;
    public TikTokDeveloperStatus Connection { get; private set; } = new("NotConnected", null, null, null, Array.Empty<string>());
    public IReadOnlyList<TikTokDeveloperKeyInfo> Keys { get; private set; } = Array.Empty<TikTokDeveloperKeyInfo>();
    public string? NewApiKey { get; private set; }
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public bool IsConfigured => _developers.IsConfigured;
    public int RequestsPerMinute => _developers.RequestsPerMinute;
    public int KeyLifetimeDays => _developers.KeyLifetimeDays;
    public TikTokModel(TikTokDeveloperService developers, ICurrentUser user) { _developers = developers; _user = user; }

    public async Task OnGetAsync()
    {
        Error = TempData["TikTokDeveloperError"] as string; Notice = TempData["TikTokDeveloperNotice"] as string;
        await LoadAsync();
    }
    public async Task<IActionResult> OnPostConnectAsync()
    {
        PrivateHeaders();
        if (!string.Equals(Request.Host.Host, "catback.id.vn", StringComparison.OrdinalIgnoreCase) || Request.Host.Port is not (null or 443))
            return Redirect("https://catback.id.vn" + TikTokDeveloperAccess.Portal);
        try
        {
            var started = await _developers.BeginAuthorizationAsync(_user.GetId(), HttpContext.RequestAborted);
            Response.Cookies.Append(TikTokDeveloperAccess.StateCookie, started.State, new CookieOptions
            { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, IsEssential = true, Path = "/", MaxAge = TimeSpan.FromMinutes(10) });
            return Redirect(started.Url);
        }
        catch (UserFriendlyException ex) { Error = ex.Message; await LoadAsync(); return Page(); }
    }
    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        await _developers.DisconnectAsync(_user.GetId(), HttpContext.RequestAborted);
        Response.Cookies.Delete(TikTokDeveloperAccess.StateCookie, new CookieOptions { Path = "/", Secure = true, HttpOnly = true });
        TempData["TikTokDeveloperNotice"] = "Đã ngắt Creator riêng tại CatBack. Creator dùng chung và các tài khoản khác giữ nguyên.";
        return LocalRedirect(TikTokDeveloperAccess.Portal);
    }
    public async Task<IActionResult> OnPostCreateKeyAsync(string? keyName)
    {
        try { NewApiKey = await _developers.CreateKeyAsync(_user.GetId(), keyName, HttpContext.RequestAborted); }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        await LoadAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostRevokeKeyAsync(Guid keyId)
    {
        if (!ModelState.IsValid || keyId == Guid.Empty) return BadRequest();
        await _developers.RevokeKeyAsync(_user.GetId(), keyId, HttpContext.RequestAborted);
        TempData["TikTokDeveloperNotice"] = "Đã thu hồi API key.";
        return LocalRedirect(TikTokDeveloperAccess.Portal);
    }
    private async Task LoadAsync()
    {
        PrivateHeaders();
        Connection = await _developers.GetStatusAsync(_user.GetId(), HttpContext.RequestAborted);
        Keys = await _developers.GetKeysAsync(_user.GetId(), HttpContext.RequestAborted);
    }
    private void PrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store"; Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
}
