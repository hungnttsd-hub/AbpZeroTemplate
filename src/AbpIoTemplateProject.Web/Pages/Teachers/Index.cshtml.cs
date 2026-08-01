using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Teachers;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _publicEducationAppService;
    public List<TeacherCardDto> Teachers { get; private set; } = new();
    public IndexModel(IPublicEducationAppService publicEducationAppService) => _publicEducationAppService = publicEducationAppService;
    public async Task OnGetAsync() => Teachers = await _publicEducationAppService.GetTeachersAsync();
}
