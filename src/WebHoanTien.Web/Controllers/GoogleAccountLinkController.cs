using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace WebHoanTien.Web.Controllers;

[Authorize]
[Route("account/google")]
public class GoogleAccountLinkController : Controller
{
    private readonly Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> _signInManager;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentUser _currentUser;
    public GoogleAccountLinkController(Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> signInManager, IdentityUserManager userManager, ICurrentUser currentUser)
    { _signInManager = signInManager; _userManager = userManager; _currentUser = currentUser; }

    [HttpGet("connect")]
    public IActionResult Connect()
    {
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, "/account/google/callback", _currentUser.GetId().ToString());
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync(_currentUser.GetId().ToString());
        if (info is null) return Redirect("/Account/Manage?googleLink=failed");
        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        var externalEmail = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(externalEmail) || !string.Equals(externalEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            return Redirect("/Account/Manage?googleLink=email-mismatch");
        var owner = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (owner is not null && owner.Id != user.Id) return Redirect("/Account/Manage?googleLink=already-used");
        if (!user.Logins.Any(x => x.LoginProvider == info.LoginProvider && x.ProviderKey == info.ProviderKey))
        {
            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded) return Redirect("/Account/Manage?googleLink=failed");
        }
        var avatar = info.Principal.FindFirst("google_avatar")?.Value;
        if (!string.IsNullOrWhiteSpace(avatar)) { user.SetProperty("GoogleAvatarUrl", avatar); await _userManager.UpdateAsync(user); }
        return Redirect("/Account/Manage?googleLink=success");
    }
}
