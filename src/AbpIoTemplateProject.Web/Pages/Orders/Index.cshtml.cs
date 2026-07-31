using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Orders;

[Authorize]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICartAppService _cartAppService;
    public List<OrderDto> Orders { get; private set; } = new();

    public IndexModel(IOrderAppService orderAppService, ICartAppService cartAppService)
    {
        _orderAppService = orderAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync()
    {
        Orders = await _orderAppService.GetMyOrdersAsync();
        await LoadCartSummaryAsync(_cartAppService);
    }
}
