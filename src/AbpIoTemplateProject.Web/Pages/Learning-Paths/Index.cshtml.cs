using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.LearningPaths;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _contentAppService;
    public List<LearningPathDto> Paths { get; private set; } = new();
    public IndexModel(IPublicContentAppService contentAppService) => _contentAppService = contentAppService;
    public async Task OnGetAsync() => Paths = await _contentAppService.GetLearningPathsAsync();
}
