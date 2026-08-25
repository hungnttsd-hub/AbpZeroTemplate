using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly ICustomerProfileAppService _customerProfileAppService;

    public CustomerProfileDto Profile { get; private set; } = new();
    public IReadOnlyList<PayoutBank> Banks => PayoutBankCatalog.Banks;

    [BindProperty]
    public UpdatePayoutAccountInput PayoutInput { get; set; } = new();

    [TempData]
    public string? PayoutStatusMessage { get; set; }

    public ProfileModel(ICustomerProfileAppService customerProfileAppService)
    {
        _customerProfileAppService = customerProfileAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync(populatePayoutInput: true);
    }

    public async Task<IActionResult> OnPostSavePayoutAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(populatePayoutInput: false);
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
            await LoadAsync(populatePayoutInput: false);
            return Page();
        }
    }

    private async Task LoadAsync(bool populatePayoutInput)
    {
        Profile = await _customerProfileAppService.GetAsync();
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

        if (!string.Equals(Profile.DisplayName, Profile.Email, System.StringComparison.OrdinalIgnoreCase))
            PayoutInput.AccountHolderName = Profile.DisplayName.ToUpperInvariant();
    }
}
