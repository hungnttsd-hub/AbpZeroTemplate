using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages.Products;

public class DetailsModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;

    public ProductDetailDto Product { get; private set; } = new();

    public DetailsModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync(string slug)
    {
        Product = await _storefrontAppService.GetProductAsync(slug);
        await LoadCartSummaryAsync(_cartAppService);
    }
}
