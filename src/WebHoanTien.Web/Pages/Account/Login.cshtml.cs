using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account.Settings;
using Volo.Abp.Account.Web;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Pages.Account;

public class LoginModel : Volo.Abp.Account.Web.Pages.Account.LoginModel
{
    [BindProperty(SupportsGet = true)]
    public bool LinkExternalLogin { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? GoogleLoginError { get; set; }

    public string? LinkError { get; private set; }

    public string? GoogleLoginErrorMessage => GoogleLoginError switch
    {
        "callback" => "Phiên đăng nhập Google không còn hợp lệ. Hãy mở CatsBack trong Chrome và thử lại.",
        "link" => "Không thể liên kết tài khoản Google lúc này. Vui lòng thử lại.",
        _ => null
    };

    public LoginModel(
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IOptions<IdentityOptions> identityOptions,
        IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache)
        : base(schemeProvider, accountOptions, identityOptions, identityDynamicClaimsPrincipalContributorCache)
    {
    }

    public override async Task<IActionResult> OnGetExternalLoginCallbackAsync(
        string returnUrl = "",
        string returnUrlHash = "",
        string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            Logger.LogWarning("External login callback error: {RemoteError}", remoteError);
            return RedirectToPage("./Login");
        }

        await IdentityOptions.SetAsync();
        var loginInfo = await SignInManager.GetExternalLoginInfoAsync();
        if (loginInfo is null)
        {
            Logger.LogWarning("External login info is not available");
            return RedirectToPage("./Login");
        }

        var signInResult = await SignInManager.ExternalLoginSignInAsync(
            loginInfo.LoginProvider,
            loginInfo.ProviderKey,
            isPersistent: true,
            bypassTwoFactor: true);

        if (signInResult.IsLockedOut)
        {
            throw new UserFriendlyException("Tài khoản đang tạm khóa.");
        }

        if (signInResult.IsNotAllowed)
        {
            throw new UserFriendlyException("Tài khoản chưa được phép đăng nhập.");
        }

        if (signInResult.Succeeded)
        {
            var linkedUser = await UserManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
            if (linkedUser is not null)
            {
                await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(linkedUser.Id, linkedUser.TenantId);
            }

            return await RedirectSafelyAsync(returnUrl, returnUrlHash);
        }

        var externalEmail = GetExternalEmail(loginInfo);

        // IdentityUser.Email is an editable contact address for local accounts.
        // Google may only auto-link when its verified email matches the immutable
        // login email stored in UserName.
        var localUser = externalEmail is null ? null : await UserManager.FindByNameAsync(externalEmail);
        if (localUser is null)
        {
            return RedirectToPage("./Register", new
            {
                IsExternalLogin = true,
                ExternalLoginAuthSchema = loginInfo.LoginProvider,
                ReturnUrl = returnUrl,
                ReturnUrlHash = returnUrlHash
            });
        }

        var isVerifiedGoogleEmail = string.Equals(loginInfo.LoginProvider, GoogleDefaults.AuthenticationScheme, StringComparison.Ordinal)
            && string.Equals(loginInfo.Principal.FindFirst("google_email_verified")?.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        if (isVerifiedGoogleEmail)
        {
            var existingOwner = await UserManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
            if (existingOwner is not null && existingOwner.Id != localUser.Id)
            {
                throw new UserFriendlyException("Tài khoản Google này đã được liên kết với một tài khoản khác.");
            }

            if (existingOwner is null)
            {
                var addLoginResult = await UserManager.AddLoginAsync(localUser, loginInfo);
                if (!addLoginResult.Succeeded)
                {
                    Logger.LogWarning("Automatic Google login link failed for user {UserId}: {Errors}", localUser.Id,
                        string.Join(", ", addLoginResult.Errors.Select(x => x.Code)));
                    return RedirectToPage("./Login", new { GoogleLoginError = "link" });
                }
            }

            var avatar = loginInfo.Principal.FindFirst("google_avatar")?.Value;
            if (!string.IsNullOrWhiteSpace(avatar))
            {
                localUser.SetProperty("GoogleAvatarUrl", avatar);
                await UserManager.UpdateAsync(localUser);
            }

            await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(localUser.Id, localUser.TenantId);
            await SignInManager.SignInAsync(localUser, isPersistent: true);
            return await RedirectSafelyAsync(returnUrl, returnUrlHash);
        }

        return RedirectToPage("./Login", new
        {
            ReturnUrl = returnUrl,
            ReturnUrlHash = returnUrlHash,
            LinkExternalLogin = true
        });
    }

    public override async Task<IActionResult> OnPostAsync(string action)
    {
        ExternalLoginInfo? pendingLogin = null;
        IdentityUser? localUser = null;

        if (LinkExternalLogin)
        {
            pendingLogin = await SignInManager.GetExternalLoginInfoAsync();
            if (pendingLogin is null)
            {
                return await LinkErrorPageAsync("Phiên liên kết Google đã hết hạn. Vui lòng thử lại.");
            }

            localUser = await UserManager.FindByNameAsync(LoginInput.UserNameOrEmailAddress) ??
                        await UserManager.FindByEmailAsync(LoginInput.UserNameOrEmailAddress);
            var externalEmail = GetExternalEmail(pendingLogin);
            if (localUser is null || externalEmail is null ||
                !string.Equals(localUser.UserName, externalEmail, StringComparison.OrdinalIgnoreCase))
            {
                return await LinkErrorPageAsync("Hãy đăng nhập đúng tài khoản local có email đăng nhập trùng với Google.");
            }

            var existingOwner = await UserManager.FindByLoginAsync(pendingLogin.LoginProvider, pendingLogin.ProviderKey);
            if (existingOwner is not null && existingOwner.Id != localUser.Id)
            {
                return await LinkErrorPageAsync("Tài khoản Google này đã được liên kết với một tài khoản khác.");
            }
        }

        var result = await base.OnPostAsync(action);
        if (result is PageResult && ModelState.IsValid && string.IsNullOrWhiteSpace(LinkError))
        {
            LinkError = "Đăng nhập chưa thành công. Hãy kiểm tra email, mật khẩu và trạng thái xác minh email.";
        }

        if (!LinkExternalLogin || result is not RedirectResult || pendingLogin is null || localUser is null)
        {
            return result;
        }

        var addLoginResult = await UserManager.AddLoginAsync(localUser, pendingLogin);
        if (!addLoginResult.Succeeded)
        {
            Logger.LogWarning("Google login link failed for user {UserId}: {Errors}", localUser.Id,
                string.Join(", ", addLoginResult.Errors.Select(x => x.Code)));
            return Redirect("/Account/Manage?googleLink=failed");
        }

        var avatar = pendingLogin.Principal.FindFirst("google_avatar")?.Value;
        if (!string.IsNullOrWhiteSpace(avatar))
        {
            localUser.SetProperty("GoogleAvatarUrl", avatar);
            await UserManager.UpdateAsync(localUser);
        }

        await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(localUser.Id, localUser.TenantId);
        return result;
    }

    private async Task<IActionResult> LinkErrorPageAsync(string message)
    {
        LinkError = message;
        ExternalProviders = await GetExternalProviders();
        EnableLocalLogin = await SettingProvider.IsTrueAsync(AccountSettingNames.EnableLocalLogin);
        return Page();
    }

    private static string? GetExternalEmail(ExternalLoginInfo loginInfo)
    {
        return loginInfo.Principal.FindFirstValue(AbpClaimTypes.Email) ??
               loginInfo.Principal.FindFirstValue(ClaimTypes.Email);
    }
}
