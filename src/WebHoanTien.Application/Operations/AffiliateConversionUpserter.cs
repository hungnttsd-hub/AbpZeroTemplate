using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;
using WebHoanTien.Notifications;

namespace WebHoanTien.Operations;

public sealed record AffiliateConversionUpsertResult(bool Inserted, bool Matched,
    int MatchedItemCount = 0, int UnmatchedItemCount = 0, int MultiTrackingOrderCount = 0,
    string? ConflictMessage = null);

public class AffiliateConversionUpserter : ITransientDependency
{
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly AffiliateCommissionRuleManager _ruleManager;
    private readonly AffiliateUserShareRateResolver _shareRateResolver;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notificationManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public AffiliateConversionUpserter(IRepository<AffiliateTracking, Guid> trackings,
        IRepository<AffiliateConversion, Guid> conversions, IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items,
        IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        AffiliateCommissionRuleManager ruleManager, AffiliateUserShareRateResolver shareRateResolver,
        AffiliateCommissionCalculator calculator, CustomerNotificationManager notificationManager,
        IGuidGenerator guidGenerator, IClock clock)
    {
        _trackings = trackings;
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _attributions = attributions;
        _ruleManager = ruleManager;
        _shareRateResolver = shareRateResolver;
        _calculator = calculator;
        _notificationManager = notificationManager;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<AffiliateConversionUpsertResult> UpsertAsync(AffiliatePlatform platform,
        NormalizedAffiliateConversion source)
    {
        var sourceOrderIds = source.Orders.Select(x => x.ExternalOrderId)
            .Distinct(StringComparer.Ordinal).ToList();
        var canonicalOrders = sourceOrderIds.Count == 0
            ? new List<AffiliateOrder>()
            : await _orders.GetListAsync(x => x.Platform == platform &&
                sourceOrderIds.Contains(x.ExternalOrderId));
        var canonicalConversionIds = canonicalOrders.Select(x => x.ConversionId).Distinct().ToList();
        AffiliateConversion? conversion = canonicalConversionIds.Count == 1
            ? await _conversions.FindAsync(canonicalConversionIds[0])
            : null;
        conversion ??= (await _conversions.GetListAsync(x => x.Platform == platform &&
            x.ExternalConversionId == source.ExternalConversionId)).FirstOrDefault();
        var inserted = conversion is null;
        conversion ??= new AffiliateConversion(_guidGenerator.Create(), platform, source.ExternalConversionId,
            source.PurchaseTime);
        if (inserted) await _conversions.InsertAsync(conversion, autoSave: true);
        var canonicalOrderByExternalId = canonicalOrders
            .GroupBy(x => x.ExternalOrderId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var sourceTokens = source.Orders.SelectMany(order => order.Items)
            .SelectMany(item => item.Attributions)
            .Select(value => value.AttributionValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var trackingByToken = sourceTokens.Count == 0
            ? new Dictionary<string, AffiliateTracking>(StringComparer.Ordinal)
            : (await _trackings.GetListAsync(x => x.Platform == platform && sourceTokens.Contains(x.TrackingToken)))
                .GroupBy(x => x.TrackingToken, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var rule = await _ruleManager.GetForPurchaseAsync(platform, source.PurchaseTime);
        var allRates = new Dictionary<Guid, decimal>();
        decimal aggregateUserCommission = 0m;
        var matchedItemCount = 0;
        var unmatchedItemCount = 0;
        var multiTrackingOrderCount = 0;
        var hasMatchedAttribution = false;
        var conflicts = new List<string>();
        var affectedUserIds = new HashSet<Guid>();

        foreach (var sourceOrder in source.Orders)
        {
            var order = canonicalOrderByExternalId.GetValueOrDefault(sourceOrder.ExternalOrderId);
            var isNewOrder = order is null;
            order ??= new AffiliateOrder(_guidGenerator.Create(), conversion.Id, sourceOrder.ExternalOrderId,
                platform);

            var existingItems = isNewOrder
                ? new List<AffiliateOrderItem>()
                : await _items.GetListAsync(x => x.OrderId == order.Id);
            var existingItemIds = existingItems.Select(x => x.Id).ToList();
            var existingAttributions = existingItemIds.Count == 0
                ? new List<AffiliateOrderItemAttribution>()
                : await _attributions.GetListAsync(x => existingItemIds.Contains(x.OrderItemId));
            var existingUserIds = existingAttributions.Where(x => x.UserId.HasValue)
                .Select(x => x.UserId!.Value).ToHashSet();
            affectedUserIds.UnionWith(existingUserIds);
            var previousStatus = isNewOrder ? (AffiliateOrderStatus?)null : order.Status;

            var sourceKeys = sourceOrder.Items.SelectMany(item => item.Attributions.Select(attribution =>
                    AttributionKey(item.ExternalItemId, item.ModelId, attribution.AttributionValue)))
                .ToHashSet(StringComparer.Ordinal);
            if (previousStatus == AffiliateOrderStatus.Settled)
            {
                var existingItemById = existingItems.ToDictionary(x => x.Id);
                var existingKeys = existingAttributions.Where(x => existingItemById.ContainsKey(x.OrderItemId))
                    .Select(x => AttributionKey(existingItemById[x.OrderItemId].ExternalItemId,
                        existingItemById[x.OrderItemId].ModelId, x.AttributionValue))
                    .ToHashSet(StringComparer.Ordinal);
                if (!existingKeys.SetEquals(sourceKeys))
                {
                    foreach (var attribution in existingAttributions) attribution.MarkConflict();
                    if (existingAttributions.Count > 0)
                        await _attributions.UpdateManyAsync(existingAttributions);
                    var existingTokens = DescribeTokens(existingAttributions.Select(x => x.AttributionValue));
                    var importedTokens = DescribeTokens(sourceOrder.Items.SelectMany(x => x.Attributions)
                        .Select(x => x.AttributionValue));
                    conflicts.Add($"Đơn {order.ExternalOrderId} đã thanh toán nhưng tập affiliate link đã thay đổi " +
                        $"(đã chốt: {existingTokens}; file mới: {importedTokens}); cần admin kiểm tra.");
                }

                var settledItemGroups = existingAttributions.GroupBy(x => x.OrderItemId).ToList();
                matchedItemCount += settledItemGroups.Count(group =>
                    group.All(x => x.Status == AffiliateAttributionStatus.Matched));
                unmatchedItemCount += settledItemGroups.Count(group =>
                    group.Any(x => x.Status != AffiliateAttributionStatus.Matched));
                hasMatchedAttribution |= existingAttributions.Any(x => x.UserId.HasValue);
                if (existingAttributions.Select(x => x.AttributionValue).Distinct(StringComparer.Ordinal).Count() > 1)
                    multiTrackingOrderCount++;
                foreach (var userGroup in existingAttributions.Where(x => x.UserId.HasValue)
                             .GroupBy(x => x.UserId!.Value))
                    allRates[userGroup.Key] = userGroup.First().UserShareRate;
                aggregateUserCommission += existingAttributions.Sum(x => x.UserCommissionSnapshot);
                continue;
            }

            order.Update(sourceOrder.Status, sourceOrder.ShopType, sourceOrder.PurchaseAmount,
                sourceOrder.NetCommission, 0m);
            if (isNewOrder)
            {
                await _orders.InsertAsync(order, autoSave: true);
                canonicalOrderByExternalId[order.ExternalOrderId] = order;
            }

            var flat = sourceOrder.Items.SelectMany(item => item.Attributions.Select(attribution => new
                {
                    Item = item,
                    Attribution = attribution,
                    Key = AttributionKey(item.ExternalItemId, item.ModelId, attribution.AttributionValue),
                    Tracking = trackingByToken.GetValueOrDefault(attribution.AttributionValue)
                })).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            var allocatedNet = _calculator.AllocateAmount(sourceOrder.NetCommission,
                    flat.Select(x => new AmountAllocationInput(x.Key, x.Attribution.ItemTotalCommission)), 4)
                .ToDictionary(x => x.Key, x => x.Amount, StringComparer.Ordinal);

            var rates = new Dictionary<Guid, decimal>();
            foreach (var userId in flat.Where(x => x.Tracking is not null).Select(x => x.Tracking!.UserId).Distinct())
            {
                var rate = await _shareRateResolver.GetForAttributedOrderAsync(userId, platform,
                    source.PurchaseTime, order.Id, order.ExternalOrderId, rule.UserShareRate);
                rates[userId] = rate;
                allRates[userId] = rate;
                affectedUserIds.Add(userId);
            }

            var userCommissionByKey = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var userRows in flat.Where(x => x.Tracking is not null).GroupBy(x => x.Tracking!.UserId))
            {
                var totalNet = userRows.Sum(x => allocatedNet[x.Key]);
                var targetUserCommission = _calculator.CalculateUserCommission(totalNet, rates[userRows.Key]);
                foreach (var allocation in _calculator.AllocateAmount(targetUserCommission,
                             userRows.Select(x => new AmountAllocationInput(x.Key, allocatedNet[x.Key])), 0))
                    userCommissionByKey[allocation.Key] = allocation.Amount;
            }

            var seenItemIds = new HashSet<Guid>();
            var seenAttributionIds = new HashSet<Guid>();
            foreach (var sourceItem in sourceOrder.Items)
            {
                var modelId = sourceItem.ModelId?.Trim() ?? string.Empty;
                var item = existingItems.FirstOrDefault(x => x.ExternalItemId == sourceItem.ExternalItemId &&
                    x.ModelId == modelId);
                var isNewItem = item is null;
                item ??= new AffiliateOrderItem(_guidGenerator.Create(), order.Id, sourceItem.ExternalItemId, modelId);
                var itemRows = flat.Where(x => x.Item.ExternalItemId == sourceItem.ExternalItemId &&
                    string.Equals(x.Item.ModelId?.Trim() ?? string.Empty, modelId, StringComparison.Ordinal)).ToList();
                var itemAllocatedNet = itemRows.Sum(x => allocatedNet[x.Key]);
                var itemUserCommission = itemRows.Sum(x => userCommissionByKey.GetValueOrDefault(x.Key));
                item.Update(sourceItem.ProductName, sourceItem.PurchaseAmount, sourceItem.Quantity,
                    sourceItem.ItemTotalCommission, itemAllocatedNet, itemUserCommission,
                    sourceItem.RefundAmount, sourceItem.IsFraud, sourceItem.ProviderStatus);
                if (isNewItem) await _items.InsertAsync(item, autoSave: true);
                else await _items.UpdateAsync(item);
                seenItemIds.Add(item.Id);

                foreach (var row in itemRows)
                {
                    var attribution = existingAttributions.FirstOrDefault(x => x.OrderItemId == item.Id &&
                        x.AttributionValue == row.Attribution.AttributionValue);
                    var isNewAttribution = attribution is null;
                    attribution ??= new AffiliateOrderItemAttribution(_guidGenerator.Create(), item.Id,
                        row.Attribution.AttributionValue);
                    attribution.UpdateSource(row.Attribution.PurchaseAmount, row.Attribution.Quantity,
                        row.Attribution.ItemTotalCommission, allocatedNet[row.Key], row.Attribution.RefundAmount,
                        row.Attribution.IsFraud, row.Attribution.ProviderStatus);
                    if (row.Tracking is null)
                    {
                        attribution.MarkUnmatched();
                    }
                    else
                    {
                        attribution.Match(row.Tracking.Id, row.Tracking.UserId, rates[row.Tracking.UserId],
                            userCommissionByKey.GetValueOrDefault(row.Key));
                        hasMatchedAttribution = true;
                    }

                    if (isNewAttribution) await _attributions.InsertAsync(attribution);
                    else await _attributions.UpdateAsync(attribution);
                    seenAttributionIds.Add(attribution.Id);
                }

                if (itemRows.Count > 0 && itemRows.All(x => x.Tracking is not null)) matchedItemCount++;
                else unmatchedItemCount++;
            }

            foreach (var stale in existingAttributions.Where(x => !seenAttributionIds.Contains(x.Id)))
                await _attributions.DeleteAsync(stale);
            foreach (var stale in existingItems.Where(x => !seenItemIds.Contains(x.Id)))
                await _items.DeleteAsync(stale);

            var orderUserCommission = userCommissionByKey.Values.Sum();
            aggregateUserCommission += orderUserCommission;
            order.Update(sourceOrder.Status, sourceOrder.ShopType, sourceOrder.PurchaseAmount,
                sourceOrder.NetCommission, orderUserCommission);
            await _orders.UpdateAsync(order);

            var distinctTokens = flat.Select(x => x.Attribution.AttributionValue)
                .Distinct(StringComparer.Ordinal).Count();
            if (distinctTokens > 1) multiTrackingOrderCount++;
            foreach (var userGroup in flat.Where(x => x.Tracking is not null).GroupBy(x => x.Tracking!.UserId))
            {
                var userId = userGroup.Key;
                if (isNewOrder || previousStatus != order.Status || !existingUserIds.Contains(userId))
                {
                    var expected = userGroup.Sum(x => userCommissionByKey.GetValueOrDefault(x.Key));
                    await _notificationManager.NotifyOrderStatusAsync(userId, order, expected, 0m);
                }
            }
        }

        var matchedTrackings = trackingByToken.Values.OrderBy(x => x.TrackingToken, StringComparer.Ordinal).ToList();
        var matchedUsers = matchedTrackings.Select(x => x.UserId).Distinct().ToList();
        if (conflicts.Count == 0 && matchedUsers.Count == 1 && matchedTrackings.Count > 0)
        {
            var primary = matchedTrackings[0];
            conversion.MapTo(primary.Id, primary.UserId, primary.TrackingToken);
        }
        else if (conflicts.Count == 0)
        {
            conversion.ClearMapping();
        }

        decimal? soleRate = null;
        if (allRates.Count == 1) soleRate = allRates.Values.Single();
        conversion.SetClickTime(source.ClickTime);
        conversion.ApplyAttributedCommission(source.GrossCommission, source.NetCommission, source.CommissionSource,
            soleRate, aggregateUserCommission);
        conversion.ChangeStatus(source.Status, _clock.Now);
        await _conversions.UpdateAsync(conversion, autoSave: true);
        await _shareRateResolver.RecalculateUnsettledOrdersAsync(affectedUserIds, platform);

        return new AffiliateConversionUpsertResult(inserted,
            hasMatchedAttribution, matchedItemCount,
            unmatchedItemCount, multiTrackingOrderCount,
            conflicts.Count == 0 ? null : string.Join(" ", conflicts));
    }

    private static string AttributionKey(string externalItemId, string? modelId, string attributionValue) =>
        externalItemId + "\u001f" + (modelId?.Trim() ?? string.Empty) + "\u001f" + attributionValue;

    private static string DescribeTokens(IEnumerable<string> values)
    {
        var tokens = values.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var shown = string.Join(", ", tokens.Take(5));
        return tokens.Count <= 5 ? shown : $"{shown}, … (+{tokens.Count - 5})";
    }
}
