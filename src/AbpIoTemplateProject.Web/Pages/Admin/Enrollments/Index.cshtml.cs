using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Enrollments;

[Authorize(AbpIoTemplateProjectPermissions.Enrollments.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminEnrollmentDto> Enrollments { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Enrollments = await _adminEducationAppService.GetEnrollmentsAsync();
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteEnrollmentAsync(id); return RedirectToPage(); }
}
