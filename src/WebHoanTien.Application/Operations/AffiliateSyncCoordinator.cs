using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Operations;

public class AffiliateSyncCoordinator : IAffiliateSyncCoordinator, ITransientDependency
{
    private readonly IBackgroundJobManager _jobs;
    private readonly IDistributedCache _cache;

    public AffiliateSyncCoordinator(IBackgroundJobManager jobs, IDistributedCache cache)
    { _jobs = jobs; _cache = cache; }

    public async Task RequestPrioritySyncAsync(Guid userId)
    {
        var userKey = $"affiliate:sync:user:{userId:N}";
        if (await _cache.GetStringAsync(userKey) is not null)
            throw new BusinessException(WebHoanTienDomainErrorCodes.SyncRequestCooldown);
        await _cache.SetStringAsync(userKey, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });

        const string coalesceKey = "affiliate:sync:priority:queued";
        if (await _cache.GetStringAsync(coalesceKey) is not null) return;
        await _cache.SetStringAsync(coalesceKey, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        await EnqueueSyncAsync(AffiliateSyncKind.Conversion);
    }

    public Task EnqueueSyncAsync(AffiliateSyncKind kind, DateTime? from = null, DateTime? to = null) =>
        _jobs.EnqueueAsync(new AffiliateSyncJobArgs { Platform = AffiliatePlatform.Shopee, Kind = kind, From = from, To = to });
}
