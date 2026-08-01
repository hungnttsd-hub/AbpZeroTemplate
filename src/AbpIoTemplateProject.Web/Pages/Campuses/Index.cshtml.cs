using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Campuses;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _publicContentAppService;
    public List<CampusDto> Campuses { get; private set; } = new();
    public IndexModel(IPublicContentAppService publicContentAppService) => _publicContentAppService = publicContentAppService;
    public async Task OnGetAsync() => Campuses = await _publicContentAppService.GetCampusesAsync();
}
