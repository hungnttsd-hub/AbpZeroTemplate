using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Classes;

[Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminCourseClassDto> Classes { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Classes = await _adminEducationAppService.GetClassesAsync();
    public async Task<IActionResult> OnPostDeleteAsync(System.Guid id)
    {
        await _adminEducationAppService.DeleteClassAsync(id);
        return RedirectToPage();
    }
}
