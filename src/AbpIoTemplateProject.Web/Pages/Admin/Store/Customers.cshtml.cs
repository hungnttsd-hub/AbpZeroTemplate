using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Customers.View)]
public class CustomersModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public List<AdminCustomerDto> Customers { get; private set; } = new();
    public CustomersModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { Customers = await _admin.GetCustomersAsync(); }
}
