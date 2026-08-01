using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Knowledge;

public class DetailsModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _contentAppService;
    public ArticleDetailDto? Article { get; private set; }
    public DetailsModel(IPublicContentAppService contentAppService) => _contentAppService = contentAppService;
    public async Task<IActionResult> OnGetAsync(string slug) { Article = await _contentAppService.GetArticleBySlugAsync(slug); return Article is null ? NotFound() : Page(); }
}
