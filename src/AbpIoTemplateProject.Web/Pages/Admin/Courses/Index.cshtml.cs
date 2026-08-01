using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace AbpIoTemplateProject.Web.Pages.Admin.Courses;

[Authorize(AbpIoTemplateProjectPermissions.Courses.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminCourseDto> Courses { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Courses = await _adminEducationAppService.GetCoursesAsync();
    public async Task OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteCourseAsync(id); }
}
