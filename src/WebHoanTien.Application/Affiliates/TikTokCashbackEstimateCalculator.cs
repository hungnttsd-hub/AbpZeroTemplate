using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Affiliates;

// Projects user-facing estimates from the original commission, without changing the settlement ledger.
public class TikTokCashbackEstimateCalculator : ITransientDependency
{
    private readonly AffiliateCommissionCalculator _calculator;
    public TikTokCashbackEstimateCalculator(AffiliateCommissionCalculator calculator) => _calculator = calculator;

    public decimal ProductCashback(decimal commission, decimal rate)
    {
        var net = TikTokCashbackTaxPolicy.AfterTax(commission);
        return Math.Min(_calculator.CalculateUserCommission(net, rate), decimal.Floor(net));
    }

    public Dictionary<Guid, decimal> Calculate(IEnumerable<AffiliateOrder> orders,
        IEnumerable<AffiliateOrderItem> items, IEnumerable<AffiliateOrderItemAttribution> attributions)
    {
        var itemsByOrder = items.ToLookup(x => x.OrderId);
        var rowsByItem = attributions.ToLookup(x => x.OrderItemId);
        var result = new Dictionary<Guid, decimal>();
        foreach (var order in orders)
        {
            var orderItems = itemsByOrder[order.Id].ToList();
            var rows = orderItems.SelectMany(item => rowsByItem[item.Id]).ToList();
            if (order.Platform != AffiliatePlatform.TikTok || order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
            {
                foreach (var row in rows) result[row.Id] = row.UserCommissionSnapshot;
                continue;
            }
            foreach (var row in rows) result[row.Id] = 0m;
            if (order.Status is AffiliateOrderStatus.Cancelled or AffiliateOrderStatus.Refunded or AffiliateOrderStatus.Rejected)
                continue;

            var withholdings = ReadWithholdings(order);
            var netById = new Dictionary<Guid, decimal>();
            foreach (var item in orderItems)
            foreach (var row in rowsByItem[item.Id])
            {
                withholdings.TryGetValue(item.ExternalItemId, out var withheld);
                // AllocatedNetCommission remains the provider amount; never reserve tax from a prior estimate.
                netById[row.Id] = TikTokCashbackTaxPolicy.AfterTax(row.AllocatedNetCommission, withheld);
            }
            foreach (var group in rows.Where(x => x.Status == AffiliateAttributionStatus.Matched &&
                         x.UserId.HasValue && x.TrackingId.HasValue).GroupBy(x => x.UserId!.Value))
            {
                var userNet = group.Sum(x => netById[x.Id]);
                var target = Math.Min(decimal.Round(group.Sum(x => netById[x.Id] *
                        Math.Clamp(x.UserShareRate, 0m, 100m) / 100m), 0, MidpointRounding.AwayFromZero),
                    decimal.Floor(userNet));
                foreach (var allocation in _calculator.AllocateAmount(target, group.Select(x =>
                             new AmountAllocationInput(x.Id.ToString("N"), netById[x.Id] * Math.Clamp(x.UserShareRate, 0m, 100m) / 100m)), 0))
                    result[Guid.ParseExact(allocation.Key, "N")] = allocation.Amount;
            }
        }
        return result;
    }

    private static Dictionary<string, decimal?> ReadWithholdings(AffiliateOrder order)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.Ordinal);
        var raw = order.GetProperty<string>("TikTokOrderSnapshot");
        if (string.IsNullOrWhiteSpace(raw)) return result;
        try
        {
            using var json = JsonDocument.Parse(raw);
            if (json.RootElement.ValueKind != JsonValueKind.Object ||
                !json.RootElement.TryGetProperty("skus", out var skus) || skus.ValueKind != JsonValueKind.Array) return result;
            foreach (var sku in skus.EnumerateArray())
            {
                if (sku.ValueKind != JsonValueKind.Object) continue;
                var id = TikTokCreatorOrderSnapshot.String(sku, "id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                // Unknown/invalid tax receives a provisional reserve; actual settlement still validates the source.
                var withheld = TikTokCreatorOrderSnapshot.TryWithholding(sku, out var value) ? value : null;
                if (!result.TryAdd(id, withheld)) result[id] = null;
            }
        }
        catch (JsonException) { /* Legacy orders without a readable snapshot use the default reserve. */ }
        return result;
    }
}
