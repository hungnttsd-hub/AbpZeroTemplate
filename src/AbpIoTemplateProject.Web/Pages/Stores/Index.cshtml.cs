using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages.Stores;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;
    public List<StoreLocationDto> Stores { get; private set; } = new();
    public IndexModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }
    public async Task OnGetAsync()
    {
        Stores = await _storefrontAppService.GetStoresAsync();
        await LoadCartSummaryAsync(_cartAppService);
    }
}
