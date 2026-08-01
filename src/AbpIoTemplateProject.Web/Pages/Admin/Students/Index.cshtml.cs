using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Students;

[Authorize(AbpIoTemplateProjectPermissions.Students.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminStudentDto> Students { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Students = await _adminEducationAppService.GetStudentsAsync();
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteStudentAsync(id); return RedirectToPage(); }
}
