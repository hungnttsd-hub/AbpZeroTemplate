using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentUser _currentUser;

    public CustomerProfileDto Profile { get; private set; } = new();
    public IReadOnlyList<PayoutBank> Banks => PayoutBankCatalog.Banks;
    public bool CanChangePassword { get; private set; }
    public bool CanEditContactEmail { get; private set; }
    public string LoginEmail { get; private set; } = string.Empty;

    [BindProperty]
    public string ContactEmail { get; set; } = string.Empty;

    [BindProperty]
    public UpdatePayoutAccountInput PayoutInput { get; set; } = new();

    [TempData]
    public string? ContactEmailStatusMessage { get; set; }

    [TempData]
    public string? ContactEmailUnavailableMessage { get; set; }

    [TempData]
    public string? PayoutStatusMessage { get; set; }

    [TempData]
    public string? PasswordStatusMessage { get; set; }

    [TempData]
    public string? PasswordUnavailableMessage { get; set; }

    public ProfileModel(
        ICustomerProfileAppService customerProfileAppService,
        IdentityUserManager userManager,
        ICurrentUser currentUser)
    {
        _customerProfileAppService = customerProfileAppService;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync(populatePayoutInput: true, populateContactEmail: true);
    }

    public async Task<IActionResult> OnPostSaveContactEmailAsync()
    {
        RemoveModelStatePrefix(nameof(PayoutInput));

        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        var hasLocalPassword = await _userManager.HasPasswordAsync(user);
        var hasGoogleLogin = (await _userManager.GetLoginsAsync(user)).Any(x =>
            x.LoginProvider.Equals("Google", StringComparison.OrdinalIgnoreCase));

        if (!hasLocalPassword || hasGoogleLogin)
        {
            ContactEmailUnavailableMessage =
                "Email liên hệ chỉ áp dụng cho tài khoản đăng ký trực tiếp bằng email và mật khẩu, chưa liên kết Google.";
            return RedirectToPage();
        }

        var contactEmail = ContactEmail?.Trim() ?? string.Empty;
        ContactEmail = contactEmail;

        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            ModelState.AddModelError(nameof(ContactEmail), "Vui lòng nhập email liên hệ.");
        }
        else if (!new EmailAddressAttribute().IsValid(contactEmail))
        {
            ModelState.AddModelError(nameof(ContactEmail), "Email liên hệ không đúng định dạng.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(populatePayoutInput: true, populateContactEmail: false);
            return Page();
        }

        // A reset address must identify one account only. Do not allow a contact email
        // to collide with another account's login email or contact email.
        var userByLoginEmail = await _userManager.FindByNameAsync(contactEmail);
        if (userByLoginEmail is not null && userByLoginEmail.Id != user.Id)
        {
            ModelState.AddModelError(nameof(ContactEmail), "Email này đang được sử dụng bởi một tài khoản khác.");
        }

        var userByContactEmail = await _userManager.FindByEmailAsync(contactEmail);
        if (userByContactEmail is not null && userByContactEmail.Id != user.Id)
        {
            ModelState.AddModelError(nameof(ContactEmail), "Email này đang được sử dụng bởi một tài khoản khác.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(populatePayoutInput: true, populateContactEmail: false);
            return Page();
        }

        if (!string.Equals(user.Email, contactEmail, StringComparison.OrdinalIgnoreCase))
        {
            // UserName remains the login identity. IdentityUser.Email is used as the
            // editable contact address, so changing it must not revoke the existing
            // login-email confirmation state.
            var wasEmailConfirmed = user.EmailConfirmed;
            await _userManager.SetEmailAsync(user, contactEmail);

            if (user.EmailConfirmed != wasEmailConfirmed)
            {
                user.SetEmailConfirmed(wasEmailConfirmed);
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    ModelState.AddModelError(
                        nameof(ContactEmail),
                        string.Join(" ", updateResult.Errors.Select(x => x.Description)));
                    await LoadAsync(populatePayoutInput: true, populateContactEmail: false);
                    return Page();
                }
            }
        }

        ContactEmailStatusMessage = "Đã lưu email liên hệ.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSavePayoutAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(populatePayoutInput: false, populateContactEmail: true);
            return Page();
        }

        try
        {
            await _customerProfileAppService.UpdatePayoutAccountAsync(PayoutInput);
            PayoutStatusMessage = "Đã lưu thông tin tài khoản nhận tiền.";
            return RedirectToPage();
        }
        catch (UserFriendlyException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(populatePayoutInput: false, populateContactEmail: true);
            return Page();
        }
    }

    private async Task LoadAsync(bool populatePayoutInput, bool populateContactEmail)
    {
        Profile = await _customerProfileAppService.GetAsync();

        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        CanChangePassword = await _userManager.HasPasswordAsync(user);
        CanEditContactEmail = CanChangePassword && !Profile.HasGoogleLogin;
        LoginEmail = string.IsNullOrWhiteSpace(user.UserName)
            ? user.Email ?? string.Empty
            : user.UserName;

        if (populateContactEmail && CanEditContactEmail)
        {
            ContactEmail = string.IsNullOrWhiteSpace(user.Email)
                ? LoginEmail
                : user.Email;
        }

        if (!populatePayoutInput) return;

        if (Profile.PayoutAccount is not null)
        {
            PayoutInput = new UpdatePayoutAccountInput
            {
                BankCode = Profile.PayoutAccount.BankCode,
                AccountNumber = Profile.PayoutAccount.AccountNumber,
                AccountHolderName = Profile.PayoutAccount.AccountHolderName
            };
            return;
        }

        if (!string.Equals(Profile.DisplayName, Profile.Email, StringComparison.OrdinalIgnoreCase))
            PayoutInput.AccountHolderName = Profile.DisplayName.ToUpperInvariant();
    }

    private void RemoveModelStatePrefix(string prefix)
    {
        var keys = ModelState.Keys
            .Where(key => key.Equals(prefix, StringComparison.Ordinal) ||
                          key.StartsWith(prefix + ".", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keys)
        {
            ModelState.Remove(key);
        }
    }
}
