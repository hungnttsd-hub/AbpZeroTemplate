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
    private readonly TikTokOrderSyncOptions _options;
    private readonly ICurrentTenant _tenant;
    private readonly ILogger<TikTokOrderSyncJob> _logger;

    public TikTokOrderSyncJob(TikTokOrderSyncService sync, IOptions<TikTokOrderSyncOptions> options,
        ICurrentTenant tenant, ILogger<TikTokOrderSyncJob> logger)
    { _sync = sync; _options = options.Value; _tenant = tenant; _logger = logger; }

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
        }, cancellationToken);
        _logger.LogInformation(
            "TikTok Creator order sync finished: fetched {Fetched}, inserted {Inserted}, updated {Updated}, unmatched {Unmatched}, review {Review}, failed {Failed}.",
            result.FetchedOrders, result.InsertedOrders, result.UpdatedOrders,
            result.UnmatchedOrders, result.NeedsReview, result.FailedOrders);
        if (result.FailedOrders > 0)
            throw new InvalidOperationException("Một số đơn TikTok chưa đồng bộ được. Xem lịch sử đồng bộ; job sẽ đọc lại ở lượt chạy tiếp theo.");
    }
}
