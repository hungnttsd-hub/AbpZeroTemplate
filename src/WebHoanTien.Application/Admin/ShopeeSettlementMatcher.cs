using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

// Shared by report imports, duplicate settlement imports, page reads and approvals.
// Matching only changes links/status/summary; it never settles orders or credits wallets.
public class ShopeeSettlementMatcher : ITransientDependency
{
    private readonly IRepository<ShopeeSettlementRecord, Guid> _records;
    private readonly IRepository<ShopeeSettlementBill, Guid> _bills;
    private readonly IRepository<ShopeeSettlementBatch, Guid> _batches;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;

    public ShopeeSettlementMatcher(IRepository<ShopeeSettlementRecord, Guid> records,
        IRepository<ShopeeSettlementBill, Guid> bills, IRepository<ShopeeSettlementBatch, Guid> batches,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrderItem, Guid> items, IRepository<AffiliateOrderItemAttribution, Guid> attributions)
    {
        _records = records; _bills = bills; _batches = batches; _orders = orders;
        _conversions = conversions; _items = items; _attributions = attributions;
    }

    public async Task<int> RefreshUnmatchedAsync(Guid? batchId = null)
    {
        var rows = await _records.GetListAsync(row => (!batchId.HasValue || row.BatchId == batchId.Value) &&
            (row.Status == ShopeeSettlementRecordStatus.Unmatched || row.Status == ShopeeSettlementRecordStatus.Invalid ||
             row.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment));
        await RefreshAsync(rows);
        return rows.Count;
    }

    public async Task RefreshForOrdersAsync(IReadOnlyCollection<string> externalOrderIds)
    {
        foreach (var chunk in externalOrderIds.Distinct(StringComparer.Ordinal).Chunk(500))
        {
            var ids = chunk.ToList();
            await RefreshAsync(await _records.GetListAsync(row => ids.Contains(row.ExternalOrderId) &&
                row.Status != ShopeeSettlementRecordStatus.Approved && row.Status != ShopeeSettlementRecordStatus.AlreadySettled));
        }
    }

    public async Task RefreshAsync(IEnumerable<ShopeeSettlementRecord> input)
    {
        var rows = input.Where(row => row.Status != ShopeeSettlementRecordStatus.Approved &&
            row.Status != ShopeeSettlementRecordStatus.AlreadySettled).DistinctBy(row => row.Id).ToList();
        if (rows.Count == 0) return;
        var previous = rows.ToDictionary(row => row.Id,
            row => (row.Status, row.AffiliateOrderId, row.AffiliateConversionId, row.UserId, row.Issue));
        var billIds = rows.Select(row => row.BillId).Distinct().ToList();
        var bills = (await _bills.GetListAsync(bill => billIds.Contains(bill.Id))).ToDictionary(bill => bill.Id);
        var orders = new List<AffiliateOrder>();
        foreach (var chunk in rows.Select(row => row.ExternalOrderId).Distinct(StringComparer.Ordinal).Chunk(500))
        {
            var ids = chunk.ToList();
            orders.AddRange(await _orders.GetListAsync(order => order.Platform == AffiliatePlatform.Shopee && ids.Contains(order.ExternalOrderId)));
        }
        var conversions = new List<AffiliateConversion>();
        foreach (var chunk in orders.Select(order => order.ConversionId).Distinct().Chunk(500))
        {
            var ids = chunk.ToList();
            conversions.AddRange(await _conversions.GetListAsync(conversion => conversion.Platform == AffiliatePlatform.Shopee && ids.Contains(conversion.Id)));
        }
        var conversionIds = conversions.Select(conversion => conversion.Id).ToHashSet();
        var byExternalId = orders.Where(order => conversionIds.Contains(order.ConversionId))
            .GroupBy(order => order.ExternalOrderId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var orderIds = orders.Select(order => order.Id).ToList();
        var items = orderIds.Count == 0 ? new List<AffiliateOrderItem>() : await _items.GetListAsync(item => orderIds.Contains(item.OrderId));
        var itemIds = items.Select(item => item.Id).ToList();
        var attributions = itemIds.Count == 0 ? new List<AffiliateOrderItemAttribution>() :
            await _attributions.GetListAsync(attribution => itemIds.Contains(attribution.OrderItemId));
        var orderByItem = items.ToDictionary(item => item.Id, item => item.OrderId);
        var byOrder = attributions.GroupBy(attribution => orderByItem[attribution.OrderItemId])
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var row in rows)
        {
            if (!byExternalId.TryGetValue(row.ExternalOrderId, out var matches) || matches.Count == 0)
            {
                row.SetUnmatched("Không tìm thấy đơn hàng tương ứng trong CatsBack.", clearMatch: true);
                continue;
            }
            if (matches.Count != 1)
            {
                row.SetUnmatched("Tìm thấy nhiều đơn hàng có cùng ID; cần kiểm tra dữ liệu nguồn.", clearMatch: true);
                continue;
            }
            var order = matches[0];
            var links = byOrder.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>());
            var users = links.Where(link => link.Status == AffiliateAttributionStatus.Matched && link.UserId.HasValue)
                .Select(link => link.UserId!.Value).Distinct().ToList();
            Guid? userId = users.Count == 1 ? users[0] : null;
            row.SetPendingApproval(order.Id, order.ConversionId, userId);
            if (order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
                row.SetAlreadySettled(order.Id, order.ConversionId, userId);
            else if (users.Count == 0)
                row.SetUnmatched("Đơn hàng có trong CatsBack nhưng chưa được ghép với người dùng.");
            else
            {
                var tolerance = Math.Max(1m, Math.Abs(order.NetCommission) * 0.0001m);
                if (bills[row.BillId].HasAuthoritativeEligibleCommission && Math.Abs(row.EligibleCommission - order.NetCommission) > tolerance)
                    row.SetInvalid(order.Id, order.ConversionId, userId, "Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
                else if (!bills[row.BillId].HasAuthoritativeEligibleCommission && row.ActualPaidCommission > order.NetCommission + tolerance)
                    row.SetInvalid(order.Id, order.ConversionId, userId, "Tiền thực trả trong file lớn hơn hoa hồng đơn hàng trong CatsBack.");
                else
                {
                    var unmatched = links.Count(link => link.Status != AffiliateAttributionStatus.Matched || !link.UserId.HasValue);
                    row.SetPendingApproval(order.Id, order.ConversionId, userId, unmatched > 0
                        ? $"Có {unmatched} affiliate link chưa ghép; phần này sẽ không được cộng cho người dùng." : null);
                }
            }
        }
        var changed = rows.Where(row => previous[row.Id] !=
            (row.Status, row.AffiliateOrderId, row.AffiliateConversionId, row.UserId, row.Issue)).ToList();
        if (changed.Count == 0) return;
        await _records.UpdateManyAsync(changed, autoSave: true);
        foreach (var batchId in changed.Select(row => row.BatchId).Distinct()) await RefreshBatchAsync(batchId);
    }

    public async Task RefreshBatchAsync(Guid batchId)
    {
        var batch = await _batches.GetAsync(batchId);
        var rows = await _records.GetListAsync(row => row.BatchId == batchId);
        var bills = (await _bills.GetListAsync(bill => bill.BatchId == batchId)).ToDictionary(bill => bill.Id);
        batch.UpdateSummary(bills.Count, rows.Count,
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.PendingApproval),
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.Approved),
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.Unmatched),
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.AlreadySettled),
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.Invalid),
            rows.Count(row => row.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment),
            rows.Sum(row => row.EligibleCommission), rows.Sum(row => ShopeeSettlementAmounts.For(row, bills[row.BillId]).Net),
            rows.Where(row => row.Status == ShopeeSettlementRecordStatus.PendingApproval).Sum(row => ShopeeSettlementAmounts.For(row, bills[row.BillId]).Net),
            rows.Where(row => row.Status == ShopeeSettlementRecordStatus.Approved).Sum(row => row.ActualPaidCommission));
        await _batches.UpdateAsync(batch, autoSave: true);
    }
}
