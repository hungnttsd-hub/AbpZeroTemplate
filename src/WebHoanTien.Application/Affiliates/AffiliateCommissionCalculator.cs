using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public class AffiliateCommissionCalculator : ITransientDependency
{
    public decimal CalculateUserCommission(decimal netCommission, decimal userShareRate) =>
        decimal.Round(netCommission * userShareRate / 100m, 0, MidpointRounding.AwayFromZero);

    public IReadOnlyList<CommissionAllocation> Allocate(decimal netCommission, decimal userShareRate, IEnumerable<CommissionAllocationInput> inputs)
    {
        var rows = inputs.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        if (rows.Count == 0) return Array.Empty<CommissionAllocation>();

        var totalWeight = rows.Sum(x => Math.Max(0m, x.Weight));
        var targetUser = CalculateUserCommission(netCommission, userShareRate);
        decimal allocatedNet = 0m;
        decimal allocatedUser = 0m;
        var result = new List<CommissionAllocation>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var last = index == rows.Count - 1;
            var ratio = totalWeight == 0m ? 1m / rows.Count : Math.Max(0m, rows[index].Weight) / totalWeight;
            var itemNet = last ? netCommission - allocatedNet : decimal.Round(netCommission * ratio, 0, MidpointRounding.AwayFromZero);
            var itemUser = last ? targetUser - allocatedUser : decimal.Round(targetUser * ratio, 0, MidpointRounding.AwayFromZero);
            allocatedNet += itemNet;
            allocatedUser += itemUser;
            result.Add(new CommissionAllocation(rows[index].Key, itemNet, itemUser));
        }

        return result;
    }

    public IReadOnlyList<AmountAllocation> AllocateAmount(decimal amount,
        IEnumerable<AmountAllocationInput> inputs, int decimals)
    {
        var rows = inputs.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        if (rows.Count == 0) return Array.Empty<AmountAllocation>();

        var target = Math.Max(0m, amount);
        var totalWeight = rows.Sum(x => Math.Max(0m, x.Weight));
        decimal allocated = 0m;
        var result = new List<AmountAllocation>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var remaining = target - allocated;
            var last = index == rows.Count - 1;
            var ratio = totalWeight == 0m ? 1m / rows.Count : Math.Max(0m, rows[index].Weight) / totalWeight;
            var value = last ? remaining : decimal.Round(target * ratio, decimals, MidpointRounding.AwayFromZero);
            value = Math.Max(0m, Math.Min(remaining, value));
            allocated += value;
            result.Add(new AmountAllocation(rows[index].Key, value));
        }

        return result;
    }
}
