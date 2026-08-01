using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Achievements;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _publicContentAppService;
    public List<StudentAchievementDto> Achievements { get; private set; } = new();
    public IndexModel(IPublicContentAppService publicContentAppService) => _publicContentAppService = publicContentAppService;
    public async Task OnGetAsync() => Achievements = await _publicContentAppService.GetFeaturedAchievementsAsync();
}
