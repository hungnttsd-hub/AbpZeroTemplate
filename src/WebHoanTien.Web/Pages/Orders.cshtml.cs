using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class OrdersModel : PageModel
{
    private readonly IAffiliateOrderAppService _orders;
    public PagedResultDto<AffiliateOrderDto> Orders { get; private set; } = new();
    public string? Message { get; private set; }
    public OrdersModel(IAffiliateOrderAppService orders) => _orders = orders;
    public async Task OnGetAsync() => Orders = await _orders.GetListAsync(new AffiliateOrderListInput { MaxResultCount = 50 });
    public async Task<IActionResult> OnPostSyncAsync()
    {
        await _orders.RequestSyncAsync();
        TempData["Message"] = "Đã yêu cầu kiểm tra. Các yêu cầu gần nhau sẽ được gộp lại.";
        return RedirectToPage();
    }
}
