using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace WebHoanTien.Affiliates;

public class AffiliateUserShareRateResolver : ITransientDependency
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly AffiliateCommissionRuleManager _commissionRuleManager;

    public AffiliateUserShareRateResolver(IRepository<AffiliateConversion, Guid> conversions,
        AffiliateCommissionRuleManager commissionRuleManager)
    {
        _conversions = conversions;
        _commissionRuleManager = commissionRuleManager;
    }

    public async Task<decimal> GetForNextOrderAsync(Guid userId, AffiliatePlatform platform, DateTime purchaseTime)
    {
        var configuredRate = (await _commissionRuleManager.GetForPurchaseAsync(platform, purchaseTime)).UserShareRate;
        var qualifyingOrders = await GetQualifyingConversionsAsync(userId, platform);
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

    private async Task<List<OrderSequenceValue>> GetQualifyingConversionsAsync(Guid userId, AffiliatePlatform platform)
    {
        var conversions = await _conversions.GetListAsync(x => x.UserId == userId && x.Platform == platform &&
            x.Status != AffiliateConversionStatus.Cancelled && x.Status != AffiliateConversionStatus.Refunded &&
            x.Status != AffiliateConversionStatus.Rejected);
        return conversions.Select(x => new OrderSequenceValue(x.Id, x.PurchaseTime, x.ExternalConversionId)).ToList();
    }

    private static int Compare(OrderSequenceValue left, OrderSequenceValue right)
    {
        var timeComparison = left.PurchaseTime.CompareTo(right.PurchaseTime);
        if (timeComparison != 0) return timeComparison;

        var externalIdComparison = string.Compare(left.ExternalConversionId, right.ExternalConversionId,
            StringComparison.Ordinal);
        return externalIdComparison != 0 ? externalIdComparison : left.Id.CompareTo(right.Id);
    }

    private sealed record OrderSequenceValue(Guid Id, DateTime PurchaseTime, string ExternalConversionId);
}
