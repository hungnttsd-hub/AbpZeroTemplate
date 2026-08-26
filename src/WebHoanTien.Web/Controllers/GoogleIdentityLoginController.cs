using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Controllers;

[AllowAnonymous]
[Route("account/google/identity")]
public class GoogleIdentityLoginController : AbpController
{
    private readonly Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> _signInManager;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityDynamicClaimsPrincipalContributorCache _dynamicClaimsCache;
    private readonly IRepository<UserLegalConsent, Guid> _consents;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly string _clientId;
    private readonly ILogger<GoogleIdentityLoginController> _logger;

    public GoogleIdentityLoginController(
        Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> signInManager,
        IdentityUserManager userManager,
        IdentityDynamicClaimsPrincipalContributorCache dynamicClaimsCache,
        IRepository<UserLegalConsent, Guid> consents,
        IGuidGenerator guidGenerator,
        IClock clock,
        IConfiguration configuration,
        ILogger<GoogleIdentityLoginController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dynamicClaimsCache = dynamicClaimsCache;
        _consents = consents;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _logger = logger;
        _clientId = configuration["Authentication:Google:ClientId"]
            ?? throw new AbpException("Authentication:Google:ClientId chưa được cấu hình.");
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [UnitOfWork]
    public async Task<IActionResult> LoginAsync(
        [FromForm] string credential,
        [FromForm] bool acceptedTerms = false,
        [FromForm] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(credential) || credential.Length > 8192)
        {
            return BadRequest(new { message = "Phản hồi đăng nhập Google không hợp lệ." });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(credential, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId }
            });
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogWarning(exception, "Google Identity Services returned an invalid ID token");
            return Unauthorized(new { message = "Phiên xác minh Google không hợp lệ hoặc đã hết hạn." });
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
        {
            return Unauthorized(new { message = "Google chưa xác minh email của tài khoản này." });
        }

        var email = payload.Email.Trim();
        var user = await _userManager.FindByLoginAsync(GoogleDefaults.AuthenticationScheme, payload.Subject)
            ?? await _userManager.FindByEmailAsync(email);

        if (user is null && !acceptedTerms)
        {
            return StatusCode(409, new
            {
                code = "registration_required",
                message = "Hãy đồng ý Điều khoản và Chính sách riêng tư để hoàn tất đăng ký.",
                redirectUrl = Url.Page("/Account/Register", values: new { returnUrl = GetSafeReturnUrl(returnUrl) })
            });
        }

        if (user is null)
        {
            user = new IdentityUser(_guidGenerator.Create(), email, email);
            user.SetEmailConfirmed(true);
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("GIS user creation failed for {Email}: {Errors}", email,
                    string.Join(", ", createResult.Errors.Select(error => error.Code)));
                return BadRequest(new { message = "Không thể tạo tài khoản bằng Google lúc này." });
            }
        }

        if (!user.EmailConfirmed && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.SetEmailConfirmed(true);
            var confirmEmailResult = await _userManager.UpdateAsync(user);
            if (!confirmEmailResult.Succeeded)
            {
                _logger.LogWarning("GIS email confirmation failed for user {UserId}: {Errors}", user.Id,
                    string.Join(", ", confirmEmailResult.Errors.Select(error => error.Code)));
                return BadRequest(new { message = "Không thể xác nhận email Google lúc này." });
            }
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Unauthorized(new { message = "Tài khoản đang tạm khóa." });
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            return Unauthorized(new { message = "Tài khoản chưa được phép đăng nhập." });
        }

        var loginOwner = await _userManager.FindByLoginAsync(GoogleDefaults.AuthenticationScheme, payload.Subject);
        if (loginOwner is not null && loginOwner.Id != user.Id)
        {
            return Conflict(new { message = "Tài khoản Google này đã được liên kết với tài khoản khác." });
        }

        if (loginOwner is null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(user,
                new UserLoginInfo(GoogleDefaults.AuthenticationScheme, payload.Subject, "Google"));
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("GIS login link failed for user {UserId}: {Errors}", user.Id,
                    string.Join(", ", addLoginResult.Errors.Select(error => error.Code)));
                return BadRequest(new { message = "Không thể liên kết tài khoản Google lúc này." });
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.Picture))
        {
            user.SetProperty("GoogleAvatarUrl", payload.Picture);
            await _userManager.UpdateAsync(user);
        }

        if (acceptedTerms && !await _consents.AnyAsync(consent => consent.UserId == user.Id
                && consent.TermsVersion == WebHoanTienConsts.TermsVersion
                && consent.PrivacyVersion == WebHoanTienConsts.PrivacyVersion))
        {
            await _consents.InsertAsync(new UserLegalConsent(
                _guidGenerator.Create(),
                user.Id,
                WebHoanTienConsts.TermsVersion,
                WebHoanTienConsts.PrivacyVersion,
                LegalConsentMethod.GoogleRegistration,
                _clock.Now));
        }

        await _dynamicClaimsCache.ClearAsync(user.Id, user.TenantId);
        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(new { redirectUrl = GetSafeReturnUrl(returnUrl) });
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
    }
}
