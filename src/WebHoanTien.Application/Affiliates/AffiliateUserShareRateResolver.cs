using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using WebHoanTien.Notifications;

namespace WebHoanTien.Affiliates;

public class AffiliateUserShareRateResolver : ITransientDependency
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly AffiliateCommissionRuleManager _commissionRuleManager;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notificationManager;

    public AffiliateUserShareRateResolver(IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateOrderItem, Guid> items,
        IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        AffiliateCommissionRuleManager commissionRuleManager,
        AffiliateCommissionCalculator calculator,
        CustomerNotificationManager notificationManager)
    {
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _attributions = attributions;
        _commissionRuleManager = commissionRuleManager;
        _calculator = calculator;
        _notificationManager = notificationManager;
    }

    public async Task<decimal> GetForNextOrderAsync(Guid userId, AffiliatePlatform platform, DateTime purchaseTime)
    {
        var configuredRate = (await _commissionRuleManager.GetForPurchaseAsync(platform, purchaseTime)).UserShareRate;
        var qualifyingOrders = await GetQualifyingOrdersAsync(userId, platform);
        return AffiliateUserShareRatePolicy.Resolve(qualifyingOrders.Count + 1, configuredRate);
    }

    public async Task<decimal> GetForOrderAsync(Guid userId, AffiliatePlatform platform, DateTime purchaseTime,
        Guid conversionId, string externalConversionId, decimal configuredRate)
    {
        var qualifyingOrders = await GetQualifyingConversionsAsync(userId, platform);
        var current = new OrderSequenceValue(conversionId, purchaseTime, externalConversionId);
        var previousOrderCount = qualifyingOrders.Count(x => Compare(x, current) < 0);
        return AffiliateUserShareRatePolicy.Resolve(previousOrderCount + 1, configuredRate);
    }

    public async Task<decimal> GetForAttributedOrderAsync(Guid userId, AffiliatePlatform platform,
        DateTime purchaseTime, Guid orderId, string externalOrderId, decimal configuredRate)
    {
        var qualifyingOrders = await GetQualifyingOrdersAsync(userId, platform);
        var current = new OrderSequenceValue(orderId, purchaseTime, externalOrderId);
        var previousOrderCount = qualifyingOrders.Count(x => Compare(x, current) < 0);
        return AffiliateUserShareRatePolicy.Resolve(previousOrderCount + 1, configuredRate);
    }

    public async Task RecalculateUnsettledOrdersAsync(IEnumerable<Guid> userIds, AffiliatePlatform platform)
    {
        var affectedUserIds = userIds.Distinct().ToList();
        if (affectedUserIds.Count == 0) return;

        var ownedAttributions = await _attributions.GetListAsync(x => x.UserId.HasValue &&
            affectedUserIds.Contains(x.UserId.Value) && x.Status != AffiliateAttributionStatus.Unmatched);
        var ownedItemIds = ownedAttributions.Select(x => x.OrderItemId).Distinct().ToList();
        if (ownedItemIds.Count == 0) return;
        var ownedItems = await _items.GetListAsync(x => ownedItemIds.Contains(x.Id));
        var orderIdByItemId = ownedItems.ToDictionary(x => x.Id, x => x.OrderId);
        var ownedOrderIds = ownedItems.Select(x => x.OrderId).Distinct().ToList();
        var ownedOrders = await _orders.GetListAsync(x => ownedOrderIds.Contains(x.Id) && x.Platform == platform);
        var conversionIds = ownedOrders.Select(x => x.ConversionId).Distinct().ToList();
        var conversions = (await _conversions.GetListAsync(x => conversionIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        var configuredRateByPurchaseTime = new Dictionary<DateTime, decimal>();
        var changedAttributions = new Dictionary<Guid, AffiliateOrderItemAttribution>();
        var changedOrderIds = new HashSet<Guid>();
        var changedRecipients = new HashSet<(Guid UserId, Guid OrderId)>();

        foreach (var userId in affectedUserIds)
        {
            var userOrderIds = ownedAttributions.Where(x => x.UserId == userId &&
                    orderIdByItemId.ContainsKey(x.OrderItemId))
                .Select(x => orderIdByItemId[x.OrderItemId]).Distinct().ToHashSet();
            var sequence = ownedOrders.Where(x => userOrderIds.Contains(x.Id) && IsQualifying(x.Status) &&
                    conversions.ContainsKey(x.ConversionId))
                .OrderBy(x => conversions[x.ConversionId].PurchaseTime)
                .ThenBy(x => x.ExternalOrderId, StringComparer.Ordinal)
                .ThenBy(x => x.Id)
                .ToList();

            for (var index = 0; index < sequence.Count; index++)
            {
                var order = sequence[index];
                if (order.Status == AffiliateOrderStatus.Settled) continue;
                var userRows = ownedAttributions.Where(x => x.UserId == userId &&
                        x.Status == AffiliateAttributionStatus.Matched && x.TrackingId.HasValue &&
                        orderIdByItemId.GetValueOrDefault(x.OrderItemId) == order.Id)
                    .OrderBy(x => x.OrderItemId).ThenBy(x => x.AttributionValue, StringComparer.Ordinal)
                    .ThenBy(x => x.Id).ToList();
                if (userRows.Count == 0) continue;

                var purchaseTime = conversions[order.ConversionId].PurchaseTime;
                if (!configuredRateByPurchaseTime.TryGetValue(purchaseTime, out var configuredRate))
                {
                    configuredRate = (await _commissionRuleManager.GetForPurchaseAsync(platform, purchaseTime))
                        .UserShareRate;
                    configuredRateByPurchaseTime[purchaseTime] = configuredRate;
                }
                var rate = AffiliateUserShareRatePolicy.Resolve(index + 1, configuredRate);
                var target = _calculator.CalculateUserCommission(
                    userRows.Sum(x => x.AllocatedNetCommission), rate);
                var keys = userRows.ToDictionary(x => x.Id,
                    x => $"{x.OrderItemId:N}:{x.AttributionValue}:{x.Id:N}");
                var allocations = _calculator.AllocateAmount(target,
                        userRows.Select(x => new AmountAllocationInput(keys[x.Id], x.AllocatedNetCommission)), 0)
                    .ToDictionary(x => x.Key, x => x.Amount, StringComparer.Ordinal);
                foreach (var attribution in userRows)
                {
                    attribution.Match(attribution.TrackingId!.Value, userId, rate, allocations[keys[attribution.Id]]);
                    changedAttributions[attribution.Id] = attribution;
                }
                changedOrderIds.Add(order.Id);
                changedRecipients.Add((userId, order.Id));
            }
        }

        if (changedOrderIds.Count == 0) return;
        await _attributions.UpdateManyAsync(changedAttributions.Values, autoSave: false);

        var allItems = await _items.GetListAsync(x => changedOrderIds.Contains(x.OrderId));
        var allItemIds = allItems.Select(x => x.Id).ToList();
        var allAttributions = allItemIds.Count == 0
            ? new List<AffiliateOrderItemAttribution>()
            : await _attributions.GetListAsync(x => allItemIds.Contains(x.OrderItemId));
        foreach (var item in allItems)
        {
            item.Update(item.ProductName, item.PurchaseAmount, item.Quantity, item.ItemTotalCommission,
                item.AllocatedNetCommission,
                allAttributions.Where(x => x.OrderItemId == item.Id).Sum(x => x.UserCommissionSnapshot),
                item.RefundAmount, item.IsFraud, item.ProviderStatus);
        }
        await _items.UpdateManyAsync(allItems, autoSave: false);

        var changedOrders = ownedOrders.Where(x => changedOrderIds.Contains(x.Id)).ToList();
        foreach (var order in changedOrders)
        {
            order.Update(order.Status, order.ShopType, order.PurchaseAmount, order.NetCommission,
                allItems.Where(x => x.OrderId == order.Id).Sum(x => x.UserCommissionSnapshot));
        }
        await _orders.UpdateManyAsync(changedOrders, autoSave: false);

        var changedOrderById = changedOrders.ToDictionary(x => x.Id);
        var orderIdByAllItemId = allItems.ToDictionary(x => x.Id, x => x.OrderId);
        foreach (var recipient in changedRecipients)
        {
            var expected = allAttributions.Where(x => x.UserId == recipient.UserId &&
                    orderIdByAllItemId.GetValueOrDefault(x.OrderItemId) == recipient.OrderId)
                .Sum(x => x.UserCommissionSnapshot);
            await _notificationManager.NotifyOrderStatusAsync(recipient.UserId,
                changedOrderById[recipient.OrderId], expected, null);
        }

        var changedConversionIds = changedOrders.Select(x => x.ConversionId).Distinct().ToList();
        var conversionOrders = await _orders.GetListAsync(x => changedConversionIds.Contains(x.ConversionId));
        var conversionOrderIds = conversionOrders.Select(x => x.Id).ToList();
        var conversionItems = await _items.GetListAsync(x => conversionOrderIds.Contains(x.OrderId));
        var conversionItemIds = conversionItems.Select(x => x.Id).ToList();
        var conversionAttributions = conversionItemIds.Count == 0
            ? new List<AffiliateOrderItemAttribution>()
            : await _attributions.GetListAsync(x => conversionItemIds.Contains(x.OrderItemId));
        var conversionIdByOrderId = conversionOrders.ToDictionary(x => x.Id, x => x.ConversionId);
        var conversionIdByItemId = conversionItems.ToDictionary(x => x.Id,
            x => conversionIdByOrderId[x.OrderId]);
        foreach (var conversionId in changedConversionIds)
        {
            var conversion = conversions[conversionId];
            var conversionRows = conversionAttributions.Where(x =>
                    conversionIdByItemId.GetValueOrDefault(x.OrderItemId) == conversionId &&
                    x.UserId.HasValue && x.Status != AffiliateAttributionStatus.Unmatched)
                .ToList();
            var distinctUsers = conversionRows.Select(x => x.UserId!.Value).Distinct().ToList();
            var distinctRates = conversionRows.Select(x => x.UserShareRate).Distinct().ToList();
            conversion.ApplyAttributedCommission(conversion.GrossCommission, conversion.NetCommission,
                conversion.CommissionSource,
                distinctUsers.Count == 1 && distinctRates.Count == 1 ? distinctRates[0] : null,
                conversionOrders.Where(x => x.ConversionId == conversionId).Sum(x => x.UserCommissionSnapshot));
        }
        await _conversions.UpdateManyAsync(changedConversionIds.Select(x => conversions[x]), autoSave: true);
    }

    private async Task<List<OrderSequenceValue>> GetQualifyingConversionsAsync(Guid userId, AffiliatePlatform platform)
    {
        var conversions = await _conversions.GetListAsync(x => x.UserId == userId && x.Platform == platform &&
            x.Status != AffiliateConversionStatus.Cancelled && x.Status != AffiliateConversionStatus.Refunded &&
            x.Status != AffiliateConversionStatus.Rejected);
        return conversions.Select(x => new OrderSequenceValue(x.Id, x.PurchaseTime, x.ExternalConversionId)).ToList();
    }

    private async Task<List<OrderSequenceValue>> GetQualifyingOrdersAsync(Guid userId, AffiliatePlatform platform)
    {
        var attributions = await _attributions.GetListAsync(x => x.UserId == userId &&
            x.Status != AffiliateAttributionStatus.Unmatched);
        var itemIds = attributions.Select(x => x.OrderItemId).Distinct().ToList();
        if (itemIds.Count == 0) return new List<OrderSequenceValue>();

        var items = await _items.GetListAsync(x => itemIds.Contains(x.Id));
        var orderIds = items.Select(x => x.OrderId).Distinct().ToList();
        var orders = await _orders.GetListAsync(x => orderIds.Contains(x.Id) && x.Platform == platform &&
            x.Status != AffiliateOrderStatus.Cancelled && x.Status != AffiliateOrderStatus.Refunded &&
            x.Status != AffiliateOrderStatus.Rejected);
        var conversionIds = orders.Select(x => x.ConversionId).Distinct().ToList();
        var conversions = (await _conversions.GetListAsync(x => conversionIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        return orders.Where(x => conversions.ContainsKey(x.ConversionId))
            .Select(x => new OrderSequenceValue(x.Id, conversions[x.ConversionId].PurchaseTime, x.ExternalOrderId))
            .ToList();
    }

    private static int Compare(OrderSequenceValue left, OrderSequenceValue right)
    {
        var timeComparison = left.PurchaseTime.CompareTo(right.PurchaseTime);
        if (timeComparison != 0) return timeComparison;

        var externalIdComparison = string.Compare(left.ExternalConversionId, right.ExternalConversionId,
            StringComparison.Ordinal);
        return externalIdComparison != 0 ? externalIdComparison : left.Id.CompareTo(right.Id);
    }

    private static bool IsQualifying(AffiliateOrderStatus status) => status is not AffiliateOrderStatus.Cancelled
        and not AffiliateOrderStatus.Refunded and not AffiliateOrderStatus.Rejected;

    private sealed record OrderSequenceValue(Guid Id, DateTime PurchaseTime, string ExternalConversionId);
}
