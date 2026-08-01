using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _educationAppService;
    private readonly IPublicContentAppService _contentAppService;

    public List<CourseCardDto> Courses { get; private set; } = new();
    public List<TeacherCardDto> Teachers { get; private set; } = new();
    public List<ArticleCardDto> Articles { get; private set; } = new();
    public List<StudentAchievementDto> Achievements { get; private set; } = new();

    [BindProperty]
    public SubmitLeadDto Input { get; set; } = new();

    public IndexModel(IPublicEducationAppService educationAppService, IPublicContentAppService contentAppService)
    {
        _educationAppService = educationAppService;
        _contentAppService = contentAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostLeadAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        await _educationAppService.SubmitLeadAsync(Input);
        TempData["LeadSubmitted"] = true;
        return RedirectToPage("/Index", pageHandler: null, routeValues: null, fragment: "dang-ky");
    }

    private async Task LoadAsync()
    {
        Courses = await _educationAppService.GetFeaturedCoursesAsync();
        Teachers = await _educationAppService.GetFeaturedTeachersAsync();
        Articles = await _contentAppService.GetArticlesAsync();
        Achievements = await _contentAppService.GetFeaturedAchievementsAsync();
    }
}
