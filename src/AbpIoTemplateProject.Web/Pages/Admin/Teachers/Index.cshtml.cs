using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Admin.Teachers;

[Authorize(AbpIoTemplateProjectPermissions.Teachers.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminTeacherDto> Teachers { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Teachers = await _adminEducationAppService.GetTeachersAsync();
    public async Task OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteTeacherAsync(id); }
}
