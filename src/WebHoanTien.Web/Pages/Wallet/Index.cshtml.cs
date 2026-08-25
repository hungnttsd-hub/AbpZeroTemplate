using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Wallet;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICustomerWalletAppService _wallet;

    public CustomerWalletOverviewDto Overview { get; private set; } = new();

    public IndexModel(ICustomerWalletAppService wallet) => _wallet = wallet;

    public async Task OnGetAsync() => Overview = await _wallet.GetOverviewAsync();

    public async Task<IActionResult> OnPostCancelAsync(Guid requestId)
    {
        try
        {
            var request = await _wallet.CancelWithdrawalRequestAsync(requestId);
            var overview = await _wallet.GetOverviewAsync();
            return new JsonResult(new
            {
                success = true,
                message = "Đã hủy yêu cầu và hoàn lại số dư khả dụng.",
                request,
                availableBalance = overview.AvailableBalance
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = WalletPageUi.ErrorMessage(exception) });
        }
    }
}
