using Volo.Abp.Users;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.IdentityExtensions;

namespace WebHoanTien.Web.Pages.Account;

[DisableAuditing]
public class RegisterModel : Volo.Abp.Account.Web.Pages.Account.RegisterModel
{
    private readonly IRepository<UserLegalConsent, Guid> _consents;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly AdminNewUserRegistrationNotifier _adminRegistrationNotifier;

    public string? RegistrationError { get; private set; }

    [BindProperty]
    public bool AcceptedTerms { get; set; }

    public override async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.IsAuthenticated)
        {
            var user = await UserManager.GetByIdAsync(CurrentUser.GetId());
            return user.IsAnonymous() ? Redirect("/Account/Upgrade?returnUrl=" + Uri.EscapeDataString(ReturnUrl ?? "/")) : Redirect("/Account/Profile");
        }
        return await base.OnGetAsync();
    }

    public RegisterModel(
        IAccountAppService accountAppService,
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
        IRepository<UserLegalConsent, Guid> consents,
        IGuidGenerator guidGenerator,
        IClock clock,
        IUnitOfWorkManager unitOfWorkManager,
        AdminNewUserRegistrationNotifier adminRegistrationNotifier)
        : base(accountAppService, schemeProvider, accountOptions, identityDynamicClaimsPrincipalContributorCache)
    {
        _consents = consents;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _unitOfWorkManager = unitOfWorkManager;
        _adminRegistrationNotifier = adminRegistrationNotifier;
    }

    public override async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.IsAuthenticated) return Redirect("/Account/Upgrade");
        if (IsExternalLogin)
        {
            ModelState.Remove("Input.Password");
        }
        else
        {
            // ABP's shared input requires email for external registration only.
            ModelState.Remove("Input.EmailAddress");
            if (Input is not null) Input.EmailAddress = string.Empty;
        }

        if (Input is not null)
        {
            Input.UserName = (Input.UserName ?? string.Empty).Trim();
            if (IsExternalLogin) Input.EmailAddress = (Input.EmailAddress ?? string.Empty).Trim();
        }
        else
        {
            ModelState.AddModelError("Input.UserName", "Vui lòng nhập username và mật khẩu.");
        }
        if (!AcceptedTerms)
        {
            ModelState.AddModelError(nameof(AcceptedTerms), "Bạn cần đồng ý với Điều khoản và Chính sách riêng tư.");
        }

        if (!ModelState.IsValid)
        {
            ExternalProviders = await GetExternalProviders();
            await CheckSelfRegistrationAsync();
            return Page();
        }

        var existingUser = Input is null ? null : IsExternalLogin
            ? await UserManager.FindByEmailAsync(Input.EmailAddress)
            : await UserManager.FindByNameAsync(Input.UserName);
        if (!IsExternalLogin && existingUser is not null)
        {
            RegistrationError = "Username này đã được sử dụng. Vui lòng chọn username khác hoặc đăng nhập.";
            ExternalProviders = await GetExternalProviders();
            await CheckSelfRegistrationAsync();
            return Page();
        }

        var result = await base.OnPostAsync();
        if (IsExternalLogin && result is RedirectResult && Input is not null)
        {
            var user = await UserManager.FindByEmailAsync(Input.EmailAddress);
            if (user is not null && !await _consents.AnyAsync(x => x.UserId == user.Id &&
                    x.TermsVersion == WebHoanTienConsts.TermsVersion &&
                    x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion))
            {
                await _consents.InsertAsync(new UserLegalConsent(
                    _guidGenerator.Create(),
                    user.Id,
                    WebHoanTienConsts.TermsVersion,
                    WebHoanTienConsts.PrivacyVersion,
                    LegalConsentMethod.GoogleRegistration,
                    _clock.Now), autoSave: true);
            }

            if (user is not null && existingUser is null && IsExternalLogin)
            {
                await _adminRegistrationNotifier.EnqueueAsync(
                    user.Id,
                    UserSelfRegistrationMethod.ExternalProvider);
            }
        }

        if (result is RedirectResult)
        {
            return LocalRedirect(!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/");
        }

        if (result is PageResult && ModelState.IsValid && string.IsNullOrWhiteSpace(RegistrationError))
        {
            RegistrationError = "Không thể tạo tài khoản. Vui lòng kiểm tra lại username và yêu cầu mật khẩu.";
        }

        return result;
    }

    protected override async Task RegisterLocalUserAsync()
    {
        try
        {
            // ModelState above validates username/password. The base DTO would
            // reintroduce mandatory email, so create through Identity directly.
            await IdentityOptions.SetAsync();
            using var transaction = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var user = new IdentityUser(_guidGenerator.Create(), Input.UserName, string.Empty, CurrentTenant.Id);
            // Persist the registered type explicitly; an anonymous type makes the
            // session middleware revoke this password login on the next request.
            user.SetProperty(CatBackAccountProperties.Type, (int)AccountType.Registered);
            user.SetProperty(CatBackAccountProperties.UserNameRegistration, true);
            AnonymousAccountManager.Check(await UserManager.CreateAsync(user, Input.Password));
            AnonymousAccountManager.Check(await UserManager.AddDefaultRolesAsync(user));
            await _consents.InsertAsync(new UserLegalConsent(
                _guidGenerator.Create(), user.Id,
                WebHoanTienConsts.TermsVersion, WebHoanTienConsts.PrivacyVersion,
                LegalConsentMethod.UserNameRegistration, _clock.Now));
            await _adminRegistrationNotifier.EnqueueAsync(user.Id, UserSelfRegistrationMethod.UserName);
            await transaction.CompleteAsync();

            await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(user.Id, user.TenantId);
            await SignInManager.SignInAsync(user, isPersistent: true);
        }
        catch (BusinessException exception)
        {
            RegistrationError = GetLocalizeExceptionMessage(exception);
            throw;
        }
    }
}
