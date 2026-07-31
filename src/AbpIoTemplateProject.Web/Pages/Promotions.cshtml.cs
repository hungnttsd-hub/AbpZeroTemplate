using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages;

public class PromotionsModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;
    public Volo.Abp.Application.Dtos.PagedResultDto<ProductListItemDto> Products { get; private set; } = new();
    public PromotionsModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService) { _storefrontAppService = storefrontAppService; _cartAppService = cartAppService; }
    public async Task OnGetAsync() { Products = await _storefrontAppService.GetProductsAsync(new ProductListInput { OnSale = true, MaxResultCount = StoreConsts.MaxPageSize }); await LoadCartSummaryAsync(_cartAppService); }
}
