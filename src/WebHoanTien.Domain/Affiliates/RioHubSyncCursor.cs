using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class RioHubSyncCursor : AuditedAggregateRoot<Guid>
{
    public string CreatorUsername { get; private set; } = string.Empty;
    public long InitialStartUnix { get; private set; }
    public long? WatermarkUnix { get; private set; }
    public DateTime? LastSucceededAtUtc { get; private set; }
    public int LastFetchedCount { get; private set; }
    protected RioHubSyncCursor() { }
    public RioHubSyncCursor(Guid id, string creatorUsername, long initialStartUnix) : base(id)
    { CreatorUsername = creatorUsername; InitialStartUnix = initialStartUnix; }
    public void Succeeded(long watermarkUnix, DateTime succeededAtUtc, int fetchedCount)
    {
        if (WatermarkUnix.HasValue && watermarkUnix < WatermarkUnix) throw new InvalidOperationException("RioHub watermark cannot move backwards.");
        WatermarkUnix = watermarkUnix; LastSucceededAtUtc = succeededAtUtc; LastFetchedCount = fetchedCount;
    }
}
