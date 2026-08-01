using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Enrollments;

[Authorize(AbpIoTemplateProjectPermissions.Enrollments.Default)]
public class CreateModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    [BindProperty] public UpsertEnrollmentDto Input { get; set; } = new();
    public List<AdminStudentDto> Students { get; private set; } = new();
    public List<AdminCourseClassDto> Classes { get; private set; } = new();
    public CreateModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) { await LoadAsync(); return Page(); } await _adminEducationAppService.CreateEnrollmentAsync(Input); return RedirectToPage("/Admin/Enrollments/Index"); }
    private async Task LoadAsync() { Students = await _adminEducationAppService.GetStudentsAsync(); Classes = await _adminEducationAppService.GetClassesAsync(); }
}
