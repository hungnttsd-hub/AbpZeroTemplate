using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages.Articles;

public class DetailsModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;
    public ArticleDetailDto Article { get; private set; } = new();

    public DetailsModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync(string slug)
    {
        Article = await _storefrontAppService.GetArticleAsync(slug);
        await LoadCartSummaryAsync(_cartAppService);
    }
}
