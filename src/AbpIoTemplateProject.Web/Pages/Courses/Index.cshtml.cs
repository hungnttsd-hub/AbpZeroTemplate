using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Courses;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _educationAppService;

    public List<CourseCardDto> Courses { get; private set; } = new();

    public IndexModel(IPublicEducationAppService educationAppService)
    {
        _educationAppService = educationAppService;
    }

    public async Task OnGetAsync()
    {
        Courses = await _educationAppService.GetCoursesAsync();
    }
}
