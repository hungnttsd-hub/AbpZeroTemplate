using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Classes;

[Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
public class EditModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public UpsertCourseClassDto Input { get; set; } = new();
    public List<SelectOptionDto> Courses { get; private set; } = new();
    public List<SelectOptionDto> Teachers { get; private set; } = new();
    public List<SelectOptionDto> Campuses { get; private set; } = new();

    public EditModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() { if (Id.HasValue) Input = await _adminEducationAppService.GetClassForEditAsync(Id.Value); await LoadOptionsAsync(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadOptionsAsync(); return Page(); }
        if (Id.HasValue) await _adminEducationAppService.UpdateClassAsync(Id.Value, Input); else await _adminEducationAppService.CreateClassAsync(Input);
        return RedirectToPage("/Admin/Classes/Index");
    }
    private async Task LoadOptionsAsync() { Courses = await _adminEducationAppService.GetCourseOptionsAsync(); Teachers = await _adminEducationAppService.GetTeacherOptionsAsync(); Campuses = await _adminEducationAppService.GetCampusOptionsAsync(); }
}
