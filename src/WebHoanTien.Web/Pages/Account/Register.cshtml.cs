using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.Account.Web.Pages.Account;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Account;

public class RegisterModel : Volo.Abp.Account.Web.Pages.Account.RegisterModel
{
    private readonly IRepository<UserLegalConsent, Guid> _consents;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;
    private readonly AffiliateCommissionRuleManager _commissionRuleManager;
    private bool _confirmationEmailSent;

    public string? RegistrationError { get; private set; }
    public decimal? CurrentUserShareRate { get; private set; }

    [BindProperty]
    public bool AcceptedTerms { get; set; }

    public RegisterModel(
        IAccountAppService accountAppService,
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
        IRepository<UserLegalConsent, Guid> consents,
        IGuidGenerator guidGenerator,
        IClock clock,
        IEmailSender emailSender,
        AffiliateCommissionRuleManager commissionRuleManager)
        : base(accountAppService, schemeProvider, accountOptions, identityDynamicClaimsPrincipalContributorCache)
    {
        _consents = consents;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _emailSender = emailSender;
        _commissionRuleManager = commissionRuleManager;
    }

    public override async Task<IActionResult> OnGetAsync()
    {
        await LoadCurrentUserShareRateAsync();
        return await base.OnGetAsync();
    }

    public override async Task<IActionResult> OnPostAsync()
    {
        if (IsExternalLogin)
        {
            ModelState.Remove("Input.Password");
        }

        if (Input is not null && !string.IsNullOrWhiteSpace(Input.EmailAddress))
        {
            Input.EmailAddress = Input.EmailAddress.Trim();
            Input.UserName = Input.EmailAddress;
            ModelState.Remove("Input.UserName");
        }

        if (!AcceptedTerms)
        {
            ModelState.AddModelError(nameof(AcceptedTerms), "Bạn cần đồng ý với Điều khoản và Chính sách riêng tư.");
        }

        if (!ModelState.IsValid)
        {
            ExternalProviders = await GetExternalProviders();
            await CheckSelfRegistrationAsync();
            await LoadCurrentUserShareRateAsync();
            return Page();
        }

        if (!IsExternalLogin && Input is not null && await UserManager.FindByEmailAsync(Input.EmailAddress) is not null)
        {
            RegistrationError = "Tài khoản với email này đã tồn tại trong hệ thống. Vui lòng đăng nhập.";
            ExternalProviders = await GetExternalProviders();
            await CheckSelfRegistrationAsync();
            await LoadCurrentUserShareRateAsync();
            return Page();
        }

        var result = await base.OnPostAsync();
        if (result is RedirectResult && Input is not null)
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
                    IsExternalLogin ? LegalConsentMethod.GoogleRegistration : LegalConsentMethod.EmailRegistration,
                    _clock.Now), autoSave: true);
            }
        }

        if (_confirmationEmailSent)
        {
            return RedirectToPage("./ConfirmEmailSent");
        }

        if (result is PageResult && ModelState.IsValid)
        {
            RegistrationError = "Không thể tạo tài khoản. Vui lòng kiểm tra lại email và yêu cầu mật khẩu.";
        }

        if (result is PageResult) await LoadCurrentUserShareRateAsync();

        return result;
    }

    private async Task LoadCurrentUserShareRateAsync()
    {
        try
        {
            CurrentUserShareRate = (await _commissionRuleManager.GetForPurchaseAsync(AffiliatePlatform.Shopee, _clock.Now)).UserShareRate;
        }
        catch (Volo.Abp.BusinessException)
        {
            CurrentUserShareRate = null;
        }
    }

    protected override async Task RegisterLocalUserAsync()
    {
        ValidateModel();
        var userDto = await AccountAppService.RegisterAsync(new RegisterDto
        {
            AppName = "MVC",
            EmailAddress = Input.EmailAddress,
            Password = Input.Password,
            UserName = Input.UserName
        });

        var user = await UserManager.GetByIdAsync(userDto.Id);
        await IdentityOptions.SetAsync();
        if (!IdentityOptions.Value.SignIn.RequireConfirmedEmail)
        {
            await SignInManager.SignInAsync(user, isPersistent: true);
            await IdentityDynamicClaimsPrincipalContributorCache.ClearAsync(user.Id, user.TenantId);
            return;
        }

        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId = user.Id, token = encodedToken, returnUrl = ReturnUrl },
            protocol: Request.Scheme);
        if (string.IsNullOrWhiteSpace(confirmationUrl))
        {
            throw new InvalidOperationException("Không thể tạo URL xác minh email.");
        }

        var safeUrl = HtmlEncoder.Default.Encode(confirmationUrl);
        await _emailSender.SendAsync(
            user.Email!,
            "Xác minh email CatsBack",
            $"<p>Chào bạn,</p><p>Nhấn vào liên kết dưới đây để xác minh email và tiếp tục:</p><p><a href=\"{safeUrl}\">Xác minh email</a></p><p>Không chia sẻ liên kết này với người khác.</p>",
            isBodyHtml: true);
        _confirmationEmailSent = true;
    }
}
