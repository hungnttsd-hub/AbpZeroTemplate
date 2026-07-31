using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Products.View)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _adminAppService;
    public StoreAdminDashboardDto Dashboard { get; private set; } = new();
    public IndexModel(IStoreAdminAppService adminAppService) { _adminAppService = adminAppService; }
    public async Task OnGetAsync() { Dashboard = await _adminAppService.GetDashboardAsync(); }
}
