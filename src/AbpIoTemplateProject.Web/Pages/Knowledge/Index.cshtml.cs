using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Knowledge;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _contentAppService;
    public List<ArticleCardDto> Articles { get; private set; } = new();
    public IndexModel(IPublicContentAppService contentAppService) => _contentAppService = contentAppService;
    public async Task OnGetAsync() => Articles = await _contentAppService.GetArticlesAsync();
}
