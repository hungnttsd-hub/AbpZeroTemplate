using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.OrderSyncPolicy)]
[UnitOfWork(IsDisabled = true)]
public class AdminTikTokOperationsAppService : WebHoanTienAppService, IAdminTikTokOperationsAppService
{
    private readonly TikTokOrderSyncService _sync;
    private readonly ICancellationTokenProvider _cancellation;

    public AdminTikTokOperationsAppService(TikTokOrderSyncService sync, ICancellationTokenProvider cancellation)
    { _sync = sync; _cancellation = cancellation; }

    public Task<TikTokManualSyncResult> SyncOrdersAsync(TikTokManualSyncInput input) =>
        _sync.SyncOrdersAsync(input, _cancellation.Token);
}
