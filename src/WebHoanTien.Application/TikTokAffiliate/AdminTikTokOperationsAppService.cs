using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;
using WebHoanTien.Operations;
using WebHoanTien.Permissions;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.OrderSyncPolicy)]
[UnitOfWork(IsDisabled = true)]
public class AdminTikTokOperationsAppService : WebHoanTienAppService, IAdminTikTokOperationsAppService
{
    private const string CreatorProperty = TikTokAffiliateAccess.OrderCreatorProperty;
    private const string FingerprintProperty = "TikTokOrderFingerprint";
    private const string ReviewProperty = "TikTokOrderReview";
    private readonly ITikTokCreatorApi _api;
    private readonly ITikTokManualSyncLock _syncLock;
    private readonly IUnitOfWorkManager _uow;
    private readonly ICurrentTenant _tenant;
    private readonly ICancellationTokenProvider _cancellation;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateClick, Guid> _clicks;
    private readonly IRepository<AffiliateCommissionRule, Guid> _rules;
    private readonly IRepository<AffiliateSyncRun, Guid> _runs;
    private readonly AffiliateConversionUpserter _upserter;

    public AdminTikTokOperationsAppService(ITikTokCreatorApi api, ITikTokManualSyncLock syncLock,
        IUnitOfWorkManager uow, ICurrentTenant tenant, ICancellationTokenProvider cancellation,
        IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateTracking, Guid> trackings, IRepository<AffiliateClick, Guid> clicks,
        IRepository<AffiliateCommissionRule, Guid> rules, IRepository<AffiliateSyncRun, Guid> runs,
        AffiliateConversionUpserter upserter)
    {
        _api = api; _syncLock = syncLock; _uow = uow; _tenant = tenant; _cancellation = cancellation;
        _orders = orders; _trackings = trackings;
        _clicks = clicks; _rules = rules; _runs = runs; _upserter = upserter;
    }

    public async Task<TikTokManualSyncResult> SyncOrdersAsync(TikTokManualSyncInput input)
    {
        if (_tenant.Id.HasValue) throw new UserFriendlyException("Chức năng này chỉ dành cho tài khoản hệ thống CatBack.");
        if (input.FromUtc >= input.ToUtc || input.ToUtc - input.FromUtc > TimeSpan.FromDays(90)
            || input.ToUtc > DateTimeOffset.UtcNow.AddDays(1) || input.FromUtc.Year < 2026)
            throw new UserFriendlyException("Chọn khoảng ngày hợp lệ, tối đa 90 ngày mỗi lần và từ năm 2026.");
        var from = input.FromUtc.ToUnixTimeSeconds();
        var to = input.ToUtc.ToUnixTimeSeconds();
        if (from >= to) throw new UserFriendlyException("Khoảng thời gian phải dài ít nhất một giây.");
        var ct = _cancellation.Token;
        await using var syncLock = await _syncLock.AcquireAsync(ct);
        var runId = GuidGenerator.Create();
        using (var work = _uow.Begin(requiresNew: true, isTransactional: true))
        {
            await _runs.InsertAsync(new AffiliateSyncRun(runId, AffiliatePlatform.TikTok,
                AffiliateSyncKind.Conversion,
                input.FromUtc.UtcDateTime, input.ToUtc.UtcDateTime, Clock.Now));
            await work.CompleteAsync(ct);
        }
        var result = new TikTokManualSyncResult();
        var review = new HashSet<string>(StringComparer.Ordinal);
        var unmatched = new HashSet<string>(StringComparer.Ordinal);
        var failures = 0;
        try
        {
            var (snapshots, creator) = await FetchAsync(from, to, ct);
            result.FetchedOrders = snapshots.Count;
            using (var work = _uow.Begin(requiresNew: true, isTransactional: true))
            {
                if (!await _rules.AnyAsync(x => x.Platform == AffiliatePlatform.TikTok && x.IsActive, ct))
                    await _rules.InsertAsync(new AffiliateCommissionRule(GuidGenerator.Create(), AffiliatePlatform.TikTok,
                        WebHoanTienConsts.DefaultUserShareRate, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));
                await work.CompleteAsync(ct);
            }
            // Reuse the existing order ledger. Syncing never settles an order or credits a wallet.
            foreach (var snapshot in snapshots)
            {
                ct.ThrowIfCancellationRequested();
                using var work = _uow.Begin(requiresNew: true, isTransactional: true);
                try
                {
                    var normalized = await NormalizeAsync(snapshot, creator);
                    var order = await _orders.FindAsync(x => x.Platform == AffiliatePlatform.TikTok && x.ExternalOrderId == snapshot.Id);
                    if (order is not null && order.GetProperty<string>(CreatorProperty) != creator)
                        throw new UserFriendlyException("Creator của đơn hàng đã thay đổi; cần kiểm tra kết nối.");
                    if (order?.SettledAt is not null)
                    {
                        order.SetProperty("TikTokProviderStatus", snapshot.Status);
                        order.SetProperty("TikTokLastCheckedAt", Clock.Now);
                        if (order.GetProperty<string>(FingerprintProperty) != normalized.Fingerprint)
                        {
                            order.SetProperty(ReviewProperty, "Dữ liệu TikTok thay đổi sau khi đã cộng ví; cần admin kiểm tra.");
                            review.Add(snapshot.Id);
                        }
                        await _orders.UpdateAsync(order);
                        await work.CompleteAsync(ct);
                        continue;
                    }
                    var upsert = await _upserter.UpsertAsync(AffiliatePlatform.TikTok, normalized.Conversion);
                    order = await _orders.GetAsync(x => x.Platform == AffiliatePlatform.TikTok && x.ExternalOrderId == snapshot.Id);
                    order.SetProperty(CreatorProperty, creator);
                    order.SetProperty(FingerprintProperty, normalized.Fingerprint);
                    order.SetProperty("TikTokProviderStatus", snapshot.Status);
                    order.SetProperty("TikTokLastCheckedAt", Clock.Now);
                    // Preserve API order/SKU fields for later reconciliation without storing tokens or request headers.
                    order.SetProperty("TikTokOrderSnapshot", snapshot.ToJson());
                    order.SetProperty("TikTokEstimatedCommission", TotalMoney(snapshot, "estimated_commission"));
                    order.SetProperty("TikTokActualCommission", snapshot.Status == "SETTLED" ? TotalMoney(snapshot, "actual_commission") : null);
                    order.SetProperty(ReviewProperty, normalized.ReviewNote);
                    await _orders.UpdateAsync(order);
                    await work.CompleteAsync(ct);
                    if (upsert.Inserted) result.InsertedOrders++; else result.UpdatedOrders++;
                    if (upsert.UnmatchedItemCount > 0) unmatched.Add(snapshot.Id);
                    if (normalized.NeedsReview || upsert.ConflictMessage is not null) review.Add(snapshot.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Log only the local order identifier and exception type, never a raw API response or credential.
                    Logger.LogWarning("TikTok manual import failed for order {OrderId}: {ErrorType}", snapshot.Id, ex.GetType().Name);
                    review.Add(snapshot.Id); failures++;
                }
            }
            result.UnmatchedOrders = unmatched.Count;
            result.NeedsReview = review.Count;
            result.Notice = "Đã lấy và lưu đơn hàng TikTok. Chức năng đối soát và cộng ví sẽ triển khai sau.";
            if (review.Count > 0) result.Notice += " Cần kiểm tra đơn: " + string.Join(", ", review.Take(10)) + ".";
            await CompleteRunAsync(runId, result, failures, result.Notice);
            return result;
        }
        catch
        {
            await CompleteRunAsync(runId, result, Math.Max(1, failures), "Lượt lấy đơn chưa hoàn tất. Có thể bấm lại để cập nhật; không tạo đơn trùng.");
            throw;
        }
    }

    private async Task<(IReadOnlyList<TikTokCreatorOrderSnapshot> Orders, string Creator)> FetchAsync(long from, long to, CancellationToken ct)
    {
        var rows = new List<JsonElement>();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);
        string? pageToken = null, creator = null;
        for (var page = 0; page < 100; page++)
        {
            var response = await _api.SearchOrdersAsync(from, to, pageToken, ct);
            creator ??= response.CreatorOpenId;
            if (creator != response.CreatorOpenId) throw new UserFriendlyException("Creator thay đổi trong khi đồng bộ. Hãy bấm lại.");
            rows.AddRange(response.Orders);
            if (string.IsNullOrEmpty(response.NextPageToken))
                return (TikTokCreatorOrderSnapshot.Merge(rows, from, to), creator);
            if (!seenTokens.Add(response.NextPageToken)) throw new UserFriendlyException("TikTok trả vòng lặp phân trang. Chưa cập nhật đơn hoặc ví.");
            pageToken = response.NextPageToken;
        }
        throw new UserFriendlyException("Khoảng ngày vượt 100 trang TikTok. Chọn khoảng nhỏ hơn; chưa cập nhật đơn hoặc ví.");
    }

    private async Task<NormalizedSnapshot> NormalizeAsync(TikTokCreatorOrderSnapshot source, string creator)
    {
        var (status, conversionStatus) = source.Status switch
        {
            "AWAITING PAYMENT" => (AffiliateOrderStatus.Unpaid, AffiliateConversionStatus.Estimated),
            "To-SETTLE" => (AffiliateOrderStatus.Pending, AffiliateConversionStatus.Pending),
            "SETTLED" => (AffiliateOrderStatus.Completed, AffiliateConversionStatus.Approved),
            "REFUNDED" => (AffiliateOrderStatus.Refunded, AffiliateConversionStatus.Refunded),
            "FROZEN" => (AffiliateOrderStatus.Rejected, AffiliateConversionStatus.Rejected),
            _ => (AffiliateOrderStatus.Pending, AffiliateConversionStatus.Pending)
        };
        var knownStatus = source.Status is "AWAITING PAYMENT" or "To-SETTLE" or "SETTLED" or "REFUNDED" or "FROZEN";
        var reviewReasons = new HashSet<string>(StringComparer.Ordinal);
        if (!knownStatus) reviewReasons.Add("Trạng thái TikTok chưa được hỗ trợ; cần admin kiểm tra.");
        var items = new List<NormalizedAffiliateOrderItem>();
        DateTime? clickTime = null;
        var fingerprint = new StringBuilder(source.Id + "|" + source.Status + "|" + creator);
        foreach (var (skuId, sku) in source.Skus.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var quantity = TikTokCreatorOrderSnapshot.Quantity(sku);
            var paid = source.Status == "SETTLED";
            var validMoney = TikTokCreatorOrderSnapshot.TryMoney(sku, paid ? "actual_commission" : "estimated_commission", out var commission);
            // These aggregate fields already include additions and deductions. Do not subtract pit or agency fees again.
            if (!validMoney) { commission = 0; reviewReasons.Add("TikTok chưa trả số tiền hoa hồng VND hợp lệ; chưa có khoản hoàn tiền để ghi nhận."); }
            if (!knownStatus || source.Status is "REFUNDED" or "FROZEN") commission = 0;
            var validPrice = TikTokCreatorOrderSnapshot.TryMoney(sku, "price", out var unitPrice);
            var purchase = validPrice ? unitPrice * quantity : 0;
            if (!validPrice) reviewReasons.Add("TikTok chưa trả giá sản phẩm VND hợp lệ; cần kiểm tra giá trị đơn hàng.");
            string traceType = "", traceId = "";
            if (sku.TryGetProperty("trace_info", out var trace) && trace.ValueKind == JsonValueKind.Object)
            { traceType = TikTokCreatorOrderSnapshot.String(trace, "type") ?? ""; traceId = TikTokCreatorOrderSnapshot.String(trace, "id") ?? ""; }
            var attribution = "tt:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(traceType + ":" + traceId + ":" + source.Id + ":" + skuId))).ToLowerInvariant();
            var matchedTrace = false;
            if (traceType == "GENERAL" && TikTokLinkAttribution.TryGetClickId(traceId, out var clickId))
            {
                var click = await _clicks.FindAsync(clickId);
                var tracking = click is null ? null : await _trackings.FindAsync(click.TrackingId);
                if (tracking?.Platform == AffiliatePlatform.TikTok
                    && tracking.GetProperty<string>(TikTokLinkAttribution.CreatorProperty) == creator
                    && tracking.GetProperty<string>(TikTokLinkAttribution.SourceProperty) == TikTokLinkAttribution.Source
                    // The API reports create_time in whole seconds; local clicks retain milliseconds.
                    && click!.ClickedAt < source.CreatedAt.AddSeconds(1))
                { attribution = tracking.TrackingToken; clickTime = click.ClickedAt; matchedTrace = true; }
            }
            //if (!matchedTrace)
            //    reviewReasons.Add(string.IsNullOrWhiteSpace(traceId)
            //        ? "TikTok chưa trả mã theo dõi (trace_info) để ghép đơn với user. Đồng bộ lại khi sàn cập nhật dữ liệu."
            //        : traceType == "SPECIFIC"
            //            ? "TikTok trả mã publisher SPECIFIC; chưa có mapping publisher với user CatBack."
            //            : "Mã theo dõi TikTok không khớp lượt bấm CatBack đã lưu; chưa xác định được người nhận.");
            fingerprint.Append('|').Append(skuId).Append(':').Append(traceType).Append(':').Append(traceId)
                .Append(':').Append(validMoney).Append(':').Append(commission.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(validPrice).Append(':').Append(purchase.ToString(CultureInfo.InvariantCulture));
            items.Add(new(skuId, TikTokCreatorOrderSnapshot.String(sku, "product_id"),
                TikTokCreatorOrderSnapshot.String(sku, "product_name"), purchase, quantity, commission, 0,
                source.Status == "FROZEN", source.Status,
                new[] { new NormalizedAffiliateOrderItemAttribution(attribution, purchase, quantity, commission,
                    0, source.Status == "FROZEN", source.Status) }));
        }
        var net = items.Sum(x => x.ItemTotalCommission);
        var purchaseTotal = items.Sum(x => x.PurchaseAmount);
        var order = new NormalizedAffiliateOrder(source.Id, status, "TikTok Shop", purchaseTotal, net, items);
        return new(new(source.Id, null, source.CreatedAt, clickTime, conversionStatus, net, net,
            CommissionSource.NetCommission, new[] { order }), reviewReasons.Count > 0,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString()))), string.Join(" ", reviewReasons));
    }

    private async Task CompleteRunAsync(Guid id, TikTokManualSyncResult result, int errors, string? notice)
    {
        using var work = _uow.Begin(requiresNew: true, isTransactional: true);
        var run = await _runs.GetAsync(id);
        run.Complete(Clock.Now, result.FetchedOrders, result.InsertedOrders, result.UpdatedOrders,
            result.UnmatchedOrders, errors, notice);
        await _runs.UpdateAsync(run);
        await work.CompleteAsync();
    }

    private static decimal? TotalMoney(TikTokCreatorOrderSnapshot snapshot, string field)
    {
        decimal sum = 0;
        foreach (var sku in snapshot.Skus.Values)
        {
            if (!TikTokCreatorOrderSnapshot.TryMoney(sku, field, out var amount)) return null;
            sum += amount;
        }
        return sum;
    }

    private sealed record NormalizedSnapshot(NormalizedAffiliateConversion Conversion, bool NeedsReview, string Fingerprint, string ReviewNote);
}
