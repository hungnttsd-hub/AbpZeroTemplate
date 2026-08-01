using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.MyLearning;

[Authorize]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IStudentPortalAppService _studentPortalAppService;
    [BindProperty] public UpdateStudentProfileDto Input { get; set; } = new();
    public List<StudentEnrollmentDto> Enrollments { get; private set; } = new();
    public bool Saved { get; private set; }
    public IndexModel(IStudentPortalAppService studentPortalAppService) => _studentPortalAppService = studentPortalAppService;
    public async Task OnGetAsync() { Input = ToInput(await _studentPortalAppService.GetMyProfileAsync()); Enrollments = await _studentPortalAppService.GetMyEnrollmentsAsync(); }
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) { Enrollments = await _studentPortalAppService.GetMyEnrollmentsAsync(); return Page(); } await _studentPortalAppService.UpdateMyProfileAsync(Input); Enrollments = await _studentPortalAppService.GetMyEnrollmentsAsync(); Saved = true; return Page(); }
    private static UpdateStudentProfileDto ToInput(StudentProfileDto source) => new() { FullName = source.FullName, PhoneNumber = source.PhoneNumber, Email = source.Email, CurrentLevel = source.CurrentLevel, Target = source.Target };
}
