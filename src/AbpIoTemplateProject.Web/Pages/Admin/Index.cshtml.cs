using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Admin;

[Authorize(AbpIoTemplateProjectPermissions.Courses.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public EducationDashboardDto Dashboard { get; private set; } = new();

    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;

    public async Task OnGetAsync() => Dashboard = await _adminEducationAppService.GetDashboardAsync();
}
