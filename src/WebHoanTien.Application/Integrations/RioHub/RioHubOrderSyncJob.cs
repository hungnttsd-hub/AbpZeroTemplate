using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.RioHub;

public class RioHubOrderSyncJob : ITransientDependency
{
    private readonly IRioHubAffiliateClient _client;
    private readonly RioHubOptions _options;
    private readonly IRepository<RioHubSyncCursor, Guid> _cursors;
    private readonly IRepository<RioHubOrderSku, Guid> _orders;
    private readonly IUnitOfWorkManager _uow;
    private readonly IGuidGenerator _guids;
    private readonly ILogger<RioHubOrderSyncJob> _logger;

    public RioHubOrderSyncJob(IRioHubAffiliateClient client, IOptions<RioHubOptions> options,
        IRepository<RioHubSyncCursor, Guid> cursors, IRepository<RioHubOrderSku, Guid> orders,
        IUnitOfWorkManager uow, IGuidGenerator guids, ILogger<RioHubOrderSyncJob> logger)
    { _client = client; _options = options.Value; _cursors = cursors; _orders = orders; _uow = uow; _guids = guids; _logger = logger; }

    // Hangfire's storage lock prevents concurrent workers/replicas from advancing this cursor out of order.
    [DisableConcurrentExecution(600)]
    [AutomaticRetry(Attempts = 0)] // In particular, a 422 must never be retried as a job failure.
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.SyncEnabled) return;
        var creator = _options.CreatorUsername.ToLowerInvariant();
        var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long? watermark;
        long initial;
        using (var read = _uow.Begin(requiresNew: true, isTransactional: true))
        {
            var cursor = await _cursors.FindAsync(x => x.CreatorUsername == creator, cancellationToken: cancellationToken);
            if (cursor is null)
            {
                initial = Math.Max(0, _options.InitialSyncFromUnix ?? end - (long)_options.InitialSyncLookbackDays * 86400);
                if (initial >= end) throw new InvalidOperationException("RioHub: mốc bắt đầu đồng bộ phải trước hiện tại (UTC).");
                // Persist the first-run range, not a success watermark, so failures cannot slide it forwards.
                cursor = new RioHubSyncCursor(_guids.Create(), creator, initial);
                await _cursors.InsertAsync(cursor, cancellationToken: cancellationToken);
            }
            watermark = cursor.WatermarkUnix;
            initial = cursor.InitialStartUnix;
            await read.CompleteAsync(cancellationToken);
        }
        var start = Math.Max(0, watermark.HasValue ? watermark.Value - 600 : initial);
        if (start >= end) throw new InvalidOperationException("RioHub: mốc bắt đầu đồng bộ phải trước hiện tại (UTC).");
        var fetched = 0;
        int? expectedTotal = null;
        var seenKeys = new HashSet<(string OrderId, string SkuId)>();
        try
        {
            for (var page = 1; ; page = checked(page + 1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var response = await _client.GetOrdersAsync(new RioHubOrdersQuery
                {
                    CreatorUsername = creator, UpdateTimeStart = start, UpdateTimeEnd = end, Page = page, PageSize = 200
                }, cancellationToken);
                if (!response.TryGetProperty("creator_username", out var returnedCreator) || returnedCreator.ValueKind != JsonValueKind.String ||
                    !string.Equals(returnedCreator.GetString(), creator, StringComparison.OrdinalIgnoreCase) ||
                    RioHubOrderSnapshot.Integer(response, "page") != page || RioHubOrderSnapshot.Integer(response, "page_size") != 200 ||
                    !response.TryGetProperty("orders", out var rows) || rows.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("RioHub orders: metadata phân trang/Creator không hợp lệ.");
                var total = RioHubOrderSnapshot.Integer(response, "total");
                if (total < 0 || expectedTotal.HasValue && expectedTotal != total || rows.GetArrayLength() > 200)
                    throw new InvalidDataException("RioHub orders: tổng dòng thay đổi hoặc phân trang không hợp lệ; chạy lại cùng watermark.");
                expectedTotal = total;
                var snapshots = rows.EnumerateArray().Select(RioHubOrderSnapshot.Parse).ToList();
                if (snapshots.Any(row => !seenKeys.Add((row.OrderId, row.SkuId))))
                    throw new InvalidDataException("RioHub orders: trùng khóa SKU trong cửa sổ phân trang; không tiến watermark.");
                fetched = checked(fetched + snapshots.Count);
                if (fetched > total || snapshots.Count == 0 && fetched < total)
                    throw new InvalidDataException("RioHub orders: thiếu hoặc thừa dòng so với total.");
                await SavePageAsync(creator, snapshots, cancellationToken);
                if (fetched == total) break;
                // At most 240 page requests/minute from this job, below the 300/minute shared limit.
                await Task.Delay(250, cancellationToken);
            }
            // Only this final transaction advances the watermark. Earlier page commits are idempotent on replay.
            using var write = _uow.Begin(requiresNew: true, isTransactional: true);
            var state = await _cursors.FindAsync(x => x.CreatorUsername == creator, cancellationToken: cancellationToken);
            if (state is null) throw new InvalidOperationException("RioHub sync cursor disappeared during the run.");
            state.Succeeded(end, DateTime.UtcNow, fetched);
            await _cursors.UpdateAsync(state, cancellationToken: cancellationToken);
            await write.CompleteAsync(cancellationToken);
            _logger.LogInformation("RioHub order sync succeeded: {Count} SKU rows, UTC Unix window [{Start}, {End}).", fetched, start, end);
        }
        catch (Exception exception)
        {
            // Do not log raw HTTP/DB exceptions: their messages can contain provider payloads/configuration.
            _logger.LogWarning("RioHub order sync failed ({Category}); watermark unchanged. Fetched {Count} rows.",
                exception.GetType().Name, fetched);
            throw;
        }
    }

    private async Task SavePageAsync(string creator, List<RioHubOrderSnapshot> snapshots, CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0) return;
        using var write = _uow.Begin(requiresNew: true, isTransactional: true);
        var orderIds = snapshots.Select(x => x.OrderId).Distinct().ToArray();
        var existing = (await _orders.GetListAsync(x => x.CreatorUsername == creator && orderIds.Contains(x.OrderId),
            cancellationToken: cancellationToken)).ToDictionary(x => (x.OrderId, x.SkuId));
        var fetchedAt = DateTime.UtcNow;
        var inserts = new List<RioHubOrderSku>();
        var updates = new List<RioHubOrderSku>();
        foreach (var row in snapshots)
        {
            if (!existing.TryGetValue((row.OrderId, row.SkuId), out var entity))
            {
                entity = new RioHubOrderSku(_guids.Create(), creator, row.OrderId, row.SkuId);
                row.ApplyTo(entity, fetchedAt);
                inserts.Add(entity);
                existing.Add((row.OrderId, row.SkuId), entity);
            }
            else if (row.ApplyTo(entity, fetchedAt))
                updates.Add(entity);
        }
        if (inserts.Count > 0) await _orders.InsertManyAsync(inserts, cancellationToken: cancellationToken);
        if (updates.Count > 0) await _orders.UpdateManyAsync(updates, cancellationToken: cancellationToken);
        await write.CompleteAsync(cancellationToken);
    }
}
