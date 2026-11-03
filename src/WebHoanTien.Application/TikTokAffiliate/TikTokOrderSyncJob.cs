using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace WebHoanTien.TikTokAffiliate;

[UnitOfWork(IsDisabled = true)]
public class TikTokOrderSyncJob : ITransientDependency
{
    public const string RecurringJobId = "tiktok-creator-orders-hourly";
    private readonly TikTokOrderSyncService _sync;
    private readonly TikTokSettlementService _settlements;
    private readonly TikTokOrderSyncOptions _options;
    private readonly ICurrentTenant _tenant;
    private readonly ILogger<TikTokOrderSyncJob> _logger;

    public TikTokOrderSyncJob(TikTokOrderSyncService sync, TikTokSettlementService settlements, IOptions<TikTokOrderSyncOptions> options,
        ICurrentTenant tenant, ILogger<TikTokOrderSyncJob> logger)
    { _sync = sync; _settlements = settlements; _options = options.Value; _tenant = tenant; _logger = logger; }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;
        cancellationToken.ThrowIfCancellationRequested();
        using var host = _tenant.Change(null);
        var to = DateTimeOffset.UtcNow;
        // Search filters by creation time. Revisit older orders to pick up status/commission changes.
        var from = to.AddDays(-_options.LookbackDays);
        var firstSupportedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        if (from < firstSupportedDate) from = firstSupportedDate;
        if (from >= to) return;

        _logger.LogInformation("Starting TikTok Creator order sync for UTC window [{From}, {To}).", from, to);
        // The shared service also holds the PostgreSQL lock used by manual sync and settlement.
        var result = await _sync.SyncOrdersAsync(new TikTokManualSyncInput
        {
            FromUtc = from,
            ToUtc = to
        }, cancellationToken, fromScheduledJob: true);
        _logger.LogInformation(
            "TikTok Creator order sync finished: fetched {Fetched}, inserted {Inserted}, updated {Updated}, unmatched {Unmatched}, review {Review}, failed {Failed}.",
            result.FetchedOrders, result.InsertedOrders, result.UpdatedOrders,
            result.UnmatchedOrders, result.NeedsReview, result.FailedOrders);
        // Only the orders committed successfully by this sync may enter automatic reconciliation.
        // The shared settlement service rechecks SETTLED/Creator/SKU/recipients under the ledger locks.
        var settlement = await _settlements.AutoSettleSyncedOrdersAsync(result.SyncedSettledOrderIds, cancellationToken);
        await _sync.RecordAutoSettlementResultAsync(result, settlement);
        _logger.LogInformation(
            "TikTok automatic settlement finished: considered {Considered}, approved {Approved}, already settled {AlreadySettled}, skipped {Skipped}, failed {Failed}, credited {Credited} VND.",
            settlement.ConsideredCount, settlement.ApprovedCount, settlement.AlreadySettledCount,
            settlement.SkippedCount, settlement.FailedCount, settlement.CreditedAmount);
        if (result.FailedOrders > 0 || settlement.FailedCount > 0)
            throw new InvalidOperationException("Một số đơn TikTok chưa đồng bộ hoặc đối soát được. Xem lịch sử đồng bộ; job sẽ kiểm tra lại ở lượt tiếp theo.");
    }
}
