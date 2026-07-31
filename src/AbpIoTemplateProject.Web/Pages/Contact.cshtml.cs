using System.Threading.Tasks;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.Web.Pages;

public class ContactModel : AbpIoTemplateProjectPageModel
{
    private readonly ICartAppService _cartAppService;
    public ContactModel(ICartAppService cartAppService) { _cartAppService = cartAppService; }
    public Task OnGetAsync() => LoadCartSummaryAsync(_cartAppService);
}
