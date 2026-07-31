using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Orders.View)]
public class OrdersModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public PagedResultDto<OrderDto> Orders { get; private set; } = new();
    public OrdersModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { Orders = await _admin.GetOrdersAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 60 }); }
    public async Task<IActionResult> OnPostStatusAsync(ChangeOrderStatusInput input) { await _admin.ChangeOrderStatusAsync(input); return RedirectToPage(); }
}
