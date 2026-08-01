using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Schedule;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _educationAppService;
    public List<CourseClassDto> Classes { get; private set; } = new();

    public IndexModel(IPublicEducationAppService educationAppService) => _educationAppService = educationAppService;

    public async Task OnGetAsync() => Classes = await _educationAppService.GetUpcomingClassesAsync();
}
