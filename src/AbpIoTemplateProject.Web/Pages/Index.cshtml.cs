namespace AbpIoTemplateProject.Web.Pages;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly global::AbpIoTemplateProject.Store.IStorefrontAppService _storefrontAppService;
    private readonly global::AbpIoTemplateProject.Store.ICartAppService _cartAppService;

    public global::AbpIoTemplateProject.Store.HomePageDto Home { get; private set; } = new();

    public IndexModel(
        global::AbpIoTemplateProject.Store.IStorefrontAppService storefrontAppService,
        global::AbpIoTemplateProject.Store.ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }

    public async System.Threading.Tasks.Task OnGetAsync()
    {
        Home = await _storefrontAppService.GetHomeAsync();
        await LoadCartSummaryAsync(_cartAppService);
    }
}
