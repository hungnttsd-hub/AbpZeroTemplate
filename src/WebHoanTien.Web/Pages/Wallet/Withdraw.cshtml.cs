using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Wallet;

[Authorize]
public class WithdrawModel : PageModel
{
    private readonly ICustomerWalletAppService _wallet;

    public WithdrawalPreparationDto Preparation { get; private set; } = new();

    [BindProperty]
    public CreateWithdrawalRequestInput Input { get; set; } = new();

    public WithdrawModel(ICustomerWalletAppService wallet) => _wallet = wallet;

    public async Task OnGetAsync() => Preparation = await _wallet.GetWithdrawalPreparationAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Số tiền rút tối thiểu là 10.000đ." });

        try
        {
            var request = await _wallet.CreateWithdrawalRequestAsync(Input);
            var overview = await _wallet.GetOverviewAsync();
            return new JsonResult(new
            {
                success = true,
                message = "Yêu cầu rút tiền đã được gửi. CatBack sẽ xử lý trong 1–3 ngày làm việc.",
                request,
                availableBalance = overview.AvailableBalance
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = WalletPageUi.ErrorMessage(exception) });
        }
    }
}
