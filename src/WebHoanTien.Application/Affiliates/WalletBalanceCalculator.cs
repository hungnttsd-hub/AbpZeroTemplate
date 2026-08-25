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
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<WithdrawalRequest, Guid> _withdrawals;

    public WalletBalanceCalculator(IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrder, Guid> orders, IRepository<WithdrawalRequest, Guid> withdrawals)
    {
        _conversions = conversions;
        _orders = orders;
        _withdrawals = withdrawals;
    }

    public async Task<WalletBalanceSnapshot> GetAsync(Guid userId)
    {
        var conversionIds = (await _conversions.GetListAsync(x => x.UserId == userId)).Select(x => x.Id).ToList();
        var orders = conversionIds.Count == 0
            ? new List<AffiliateOrder>()
            : await _orders.GetListAsync(x => conversionIds.Contains(x.ConversionId));
        var withdrawals = await _withdrawals.GetListAsync(x => x.UserId == userId);

        return new WalletBalanceSnapshot(
            orders.Where(x => x.Status == AffiliateOrderStatus.Settled).Sum(x => x.PayableUserCommission),
            orders.Where(x => x.Status is AffiliateOrderStatus.Unpaid or AffiliateOrderStatus.Pending
                    or AffiliateOrderStatus.Completed)
                .Sum(x => x.UserCommissionSnapshot),
            withdrawals.Where(x => x.Status == WithdrawalRequestStatus.Pending).Sum(x => x.Amount),
            withdrawals.Where(x => x.Status == WithdrawalRequestStatus.Paid).Sum(x => x.Amount));
    }
}
