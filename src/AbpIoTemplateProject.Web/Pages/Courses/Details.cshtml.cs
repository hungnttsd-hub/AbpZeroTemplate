using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Courses;

public class DetailsModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _publicEducationAppService;
    public CourseDetailDto? Course { get; private set; }
    public DetailsModel(IPublicEducationAppService publicEducationAppService) => _publicEducationAppService = publicEducationAppService;
    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Course = await _publicEducationAppService.GetCourseBySlugAsync(slug);
        return Course == null ? NotFound() : Page();
    }
}
