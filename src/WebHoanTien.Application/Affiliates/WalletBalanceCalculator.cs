using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace WebHoanTien.Affiliates;

public sealed record WalletBalanceSnapshot(decimal ConfirmedAmount, decimal PendingAmount,
    decimal ReservedAmount, decimal PaidAmount)
{
    public decimal RawBalance => ConfirmedAmount - ReservedAmount - PaidAmount;
    public decimal AvailableBalance => Math.Max(0m, RawBalance);
}

public class WalletBalanceCalculator : ITransientDependency
{
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IRepository<WithdrawalRequest, Guid> _withdrawals;

    public WalletBalanceCalculator(IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items,
        IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        IRepository<WithdrawalRequest, Guid> withdrawals)
    {
        _orders = orders;
        _items = items;
        _attributions = attributions;
        _withdrawals = withdrawals;
    }

    public async Task<WalletBalanceSnapshot> GetAsync(Guid userId)
        => (await GetManyAsync(new[] { userId }))[userId];

    public async Task<Dictionary<Guid, WalletBalanceSnapshot>> GetManyAsync(IReadOnlyCollection<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, WalletBalanceSnapshot>();
        var attributions = await _attributions.GetListAsync(x => x.UserId.HasValue && ids.Contains(x.UserId.Value) &&
            x.Status != AffiliateAttributionStatus.Unmatched);
        var itemIds = attributions.Select(x => x.OrderItemId).Distinct().ToList();
        var items = itemIds.Count == 0 ? new List<AffiliateOrderItem>() :
            await _items.GetListAsync(x => itemIds.Contains(x.Id));
        var orderIds = items.Select(x => x.OrderId).Distinct().ToList();
        var orders = orderIds.Count == 0 ? new List<AffiliateOrder>() :
            await _orders.GetListAsync(x => orderIds.Contains(x.Id));
        var orderByItem = items.ToDictionary(x => x.Id, x => x.OrderId);
        var orderById = orders.ToDictionary(x => x.Id);
        var withdrawals = await _withdrawals.GetListAsync(x => ids.Contains(x.UserId));
        var attributionsByUser = attributions.ToLookup(x => x.UserId!.Value);
        var withdrawalsByUser = withdrawals.ToLookup(x => x.UserId);

        return ids.ToDictionary(userId => userId, userId => new WalletBalanceSnapshot(
            attributionsByUser[userId].Where(x => orderByItem.TryGetValue(x.OrderItemId, out var orderId) &&
                    orderById.TryGetValue(orderId, out var order) && order.Status == AffiliateOrderStatus.Settled)
                .Sum(x => x.SettledUserCommission ?? 0m),
            attributionsByUser[userId].Where(x => orderByItem.TryGetValue(x.OrderItemId, out var orderId) &&
                    orderById.TryGetValue(orderId, out var order) &&
                    order.Status is AffiliateOrderStatus.Unpaid or AffiliateOrderStatus.Pending
                        or AffiliateOrderStatus.Completed)
                .Sum(x => x.UserCommissionSnapshot),
            withdrawalsByUser[userId].Where(x => x.Status == WithdrawalRequestStatus.Pending).Sum(x => x.Amount),
            withdrawalsByUser[userId].Where(x => x.Status == WithdrawalRequestStatus.Paid).Sum(x => x.Amount)));
    }
}
