using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

public interface ITikTokManualSyncLock
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken ct = default);
}
