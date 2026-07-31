using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Checkout;

public class SuccessModel : AbpIoTemplateProjectPageModel
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICartAppService _cartAppService;
    public OrderDto Order { get; private set; } = new();

    public SuccessModel(IOrderAppService orderAppService, ICartAppService cartAppService)
    {
        _orderAppService = orderAppService;
        _cartAppService = cartAppService;
    }

    public async Task<IActionResult> OnGetAsync(string orderNumber, string verification)
    {
        if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(verification))
        {
            return RedirectToPage("/Index");
        }

        Order = await _orderAppService.TrackAsync(new TrackOrderInput
        {
            OrderNumber = orderNumber,
            Verification = verification
        });
        await LoadCartSummaryAsync(_cartAppService);
        return Page();
    }
}
