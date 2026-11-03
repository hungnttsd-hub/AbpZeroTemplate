using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokManualSyncInput
{
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
}

public sealed class TikTokManualSyncResult
{
    public int FetchedOrders { get; set; }
    public int InsertedOrders { get; set; }
    public int UpdatedOrders { get; set; }
    public int UnmatchedOrders { get; set; }
    public int NeedsReview { get; set; }
    public int FailedOrders { get; set; }
    public string? Notice { get; set; }
    // Server-only handoff to the scheduled job, after each order transaction has committed.
    [JsonIgnore] public List<Guid> SyncedSettledOrderIds { get; } = new();
    [JsonIgnore] public Guid SyncRunId { get; set; }
}

public interface IAdminTikTokOperationsAppService : IApplicationService
{
    Task<TikTokManualSyncResult> SyncOrdersAsync(TikTokManualSyncInput input);
}
