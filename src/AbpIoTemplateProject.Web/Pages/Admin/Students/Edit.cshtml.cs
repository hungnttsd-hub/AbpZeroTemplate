using System;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Students;

[Authorize(AbpIoTemplateProjectPermissions.Students.Default)]
public class EditModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public UpsertStudentDto Input { get; set; } = new();
    public EditModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() { if (Id.HasValue) Input = await _adminEducationAppService.GetStudentForEditAsync(Id.Value); }
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); if (Id.HasValue) await _adminEducationAppService.UpdateStudentAsync(Id.Value, Input); else await _adminEducationAppService.CreateStudentAsync(Input); return RedirectToPage("/Admin/Students/Index"); }
}
