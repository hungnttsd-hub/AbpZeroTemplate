using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Admin;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Affiliates;

[Authorize(WebHoanTienPermissions.Admin.Default)]
public class IndexModel : PageModel
{
    private readonly IAdminAffiliateSettingsAppService _settings;
    private readonly IAdminAffiliateSyncAppService _sync;
    public AffiliateConnectionStatusDto Connection { get; private set; } = new();
    public ListResultDto<AffiliateSyncStateDto> States { get; private set; } = new();
    public IndexModel(IAdminAffiliateSettingsAppService settings, IAdminAffiliateSyncAppService sync) { _settings = settings; _sync = sync; }
    public async Task OnGetAsync() { Connection = await _settings.GetAsync(); States = await _sync.GetStatesAsync(); }
    public async Task OnPostSyncAsync() { await _sync.SyncNowAsync(); }
}
