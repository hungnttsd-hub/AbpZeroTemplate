using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace WebHoanTien.TikTokAffiliate;

// Only ephemeral review state. Never writes to real affiliate, wallet or order tables.
public sealed class TikTokAffiliateDemoStore
{
    private readonly IMemoryCache _cache;
    private readonly object _creationLock = new();

    public TikTokAffiliateDemoStore(IMemoryCache cache) => _cache = cache;

    internal DemoState Get(Guid? tenantId, Guid userId)
    {
        var key = (typeof(TikTokAffiliateDemoStore), tenantId, userId);
        lock (_creationLock)
        {
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(2);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
                return new DemoState();
            })!;
        }
    }

    internal sealed class DemoState
    {
        internal readonly object Sync = new();
        internal bool Connected;
        internal bool ProductChecked;
        internal readonly List<TikTokGeneratedLinkDto> Links = new();
    }
}
