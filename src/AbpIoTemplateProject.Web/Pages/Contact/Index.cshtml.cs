using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Contact;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _publicEducationAppService;
    public List<CourseCardDto> Courses { get; private set; } = new();
    [BindProperty] public SubmitLeadDto Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public Guid? CourseId { get; set; }
    public bool Submitted { get; private set; }
    public IndexModel(IPublicEducationAppService publicEducationAppService) => _publicEducationAppService = publicEducationAppService;
    public async Task OnGetAsync() { Courses = await _publicEducationAppService.GetCoursesAsync(); Input.InterestedCourseId = CourseId; }
    public async Task<IActionResult> OnPostAsync()
    {
        Courses = await _publicEducationAppService.GetCoursesAsync();
        if (!ModelState.IsValid) return Page();
        await _publicEducationAppService.SubmitLeadAsync(Input);
        Submitted = true;
        ModelState.Clear(); Input = new();
        return Page();
    }
}
