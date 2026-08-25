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

namespace WebHoanTien.Operations;

public sealed record AffiliateConversionUpsertResult(bool Inserted, bool Matched);

public class AffiliateConversionUpserter : ITransientDependency
{
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly AffiliateCommissionRuleManager _ruleManager;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public AffiliateConversionUpserter(IRepository<AffiliateTracking, Guid> trackings,
        IRepository<AffiliateConversion, Guid> conversions, IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items, AffiliateCommissionRuleManager ruleManager,
        AffiliateCommissionCalculator calculator, IGuidGenerator guidGenerator, IClock clock)
    {
        _trackings = trackings;
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _ruleManager = ruleManager;
        _calculator = calculator;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<AffiliateConversionUpsertResult> UpsertAsync(AffiliatePlatform platform,
        NormalizedAffiliateConversion source)
    {
        var conversion = (await _conversions.GetListAsync(x => x.Platform == platform &&
            x.ExternalConversionId == source.ExternalConversionId)).FirstOrDefault();
        var inserted = conversion is null;
        conversion ??= new AffiliateConversion(_guidGenerator.Create(), platform, source.ExternalConversionId, source.PurchaseTime);

        var matched = conversion.TrackingId.HasValue;
        if (!matched && !string.IsNullOrWhiteSpace(source.AttributionValue))
        {
            var tracking = (await _trackings.GetListAsync(x => x.Platform == platform &&
                x.TrackingToken == source.AttributionValue)).FirstOrDefault();
            if (tracking is not null)
            {
                conversion.MapTo(tracking.Id, tracking.UserId, source.AttributionValue);
                matched = true;
            }
        }

        var rule = await _ruleManager.GetForPurchaseAsync(platform, source.PurchaseTime);
        conversion.SetClickTime(source.ClickTime);
        conversion.ApplyCommission(source.GrossCommission, source.NetCommission, source.CommissionSource, rule.UserShareRate);
        conversion.ChangeStatus(source.Status, _clock.Now);
        if (inserted) await _conversions.InsertAsync(conversion, autoSave: true);
        else await _conversions.UpdateAsync(conversion, autoSave: true);

        foreach (var sourceOrder in source.Orders)
        {
            var order = (await _orders.GetListAsync(x => x.ConversionId == conversion.Id &&
                x.ExternalOrderId == sourceOrder.ExternalOrderId)).FirstOrDefault();
            var isNewOrder = order is null;
            order ??= new AffiliateOrder(_guidGenerator.Create(), conversion.Id, sourceOrder.ExternalOrderId);
            var orderUserCommission = _calculator.CalculateUserCommission(sourceOrder.NetCommission, rule.UserShareRate);
            order.Update(sourceOrder.Status, sourceOrder.ShopType, sourceOrder.PurchaseAmount, sourceOrder.NetCommission,
                orderUserCommission);
            if (isNewOrder) await _orders.InsertAsync(order, autoSave: true);
            else await _orders.UpdateAsync(order, autoSave: true);

            var allocation = _calculator.Allocate(sourceOrder.NetCommission, rule.UserShareRate,
                    sourceOrder.Items.Select(item => new CommissionAllocationInput(
                        item.ExternalItemId + ":" + (item.ModelId ?? string.Empty), item.ItemTotalCommission)))
                .ToDictionary(item => item.Key, StringComparer.Ordinal);
            foreach (var sourceItem in sourceOrder.Items)
            {
                var modelId = sourceItem.ModelId?.Trim() ?? string.Empty;
                var item = (await _items.GetListAsync(x => x.OrderId == order.Id &&
                    x.ExternalItemId == sourceItem.ExternalItemId && x.ModelId == modelId)).FirstOrDefault();
                var isNewItem = item is null;
                item ??= new AffiliateOrderItem(_guidGenerator.Create(), order.Id, sourceItem.ExternalItemId, modelId);
                var allocated = allocation[sourceItem.ExternalItemId + ":" + modelId];
                item.Update(sourceItem.ProductName, sourceItem.PurchaseAmount, sourceItem.Quantity,
                    sourceItem.ItemTotalCommission, allocated.NetCommission, allocated.UserCommission,
                    sourceItem.RefundAmount, sourceItem.IsFraud, sourceItem.ProviderStatus);
                if (isNewItem) await _items.InsertAsync(item);
                else await _items.UpdateAsync(item);
            }
        }

        return new AffiliateConversionUpsertResult(inserted, matched);
    }
}
