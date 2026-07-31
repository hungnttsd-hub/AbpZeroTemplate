using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AbpIoTemplateProject.Web.Pages;

public class TrackOrderModel : AbpIoTemplateProjectPageModel
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICartAppService _cartAppService;

    [BindProperty(SupportsGet = true)]
    public string? OrderNumber { get; set; }

    [BindProperty]
    public string Verification { get; set; } = string.Empty;

    public OrderDto? Order { get; private set; }

    public TrackOrderModel(IOrderAppService orderAppService, ICartAppService cartAppService)
    {
        _orderAppService = orderAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadCartSummaryAsync(_cartAppService);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderNumber) || string.IsNullOrWhiteSpace(Verification))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập mã đơn hàng và email hoặc số điện thoại.");
        }
        else
        {
            try
            {
                Order = await _orderAppService.TrackAsync(new TrackOrderInput
                {
                    OrderNumber = OrderNumber,
                    Verification = Verification
                });
            }
            catch (UserFriendlyException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        await LoadCartSummaryAsync(_cartAppService);
        return Page();
    }
}
