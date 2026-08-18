using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;

namespace WebHoanTien.Operations;

public class AffiliateSyncJob : IAsyncBackgroundJob<AffiliateSyncJobArgs>, ITransientDependency
{
    private readonly IAffiliateProviderRegistry _providers;
    private readonly IRepository<AffiliateSyncState, Guid> _states;
    private readonly IRepository<AffiliateSyncRun, Guid> _runs;
    private readonly IRepository<AffiliateRawPayload, Guid> _payloads;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly AffiliateCommissionRuleManager _ruleManager;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly Volo.Abp.Guids.IGuidGenerator _guidGenerator;
    private readonly Volo.Abp.Timing.IClock _clock;
    private readonly ILogger<AffiliateSyncJob> _logger;

    public AffiliateSyncJob(IAffiliateProviderRegistry providers, IRepository<AffiliateSyncState, Guid> states,
        IRepository<AffiliateSyncRun, Guid> runs, IRepository<AffiliateRawPayload, Guid> payloads,
        IRepository<AffiliateTracking, Guid> trackings, IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateOrderItem, Guid> items,
        AffiliateCommissionRuleManager ruleManager, AffiliateCommissionCalculator calculator,
        Volo.Abp.Guids.IGuidGenerator guidGenerator, Volo.Abp.Timing.IClock clock, ILogger<AffiliateSyncJob> logger)
    {
        _providers = providers; _states = states; _runs = runs; _payloads = payloads; _trackings = trackings;
        _conversions = conversions; _orders = orders; _items = items; _ruleManager = ruleManager; _calculator = calculator;
        _guidGenerator = guidGenerator; _clock = clock; _logger = logger;
    }

    [UnitOfWork]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    public async Task ExecuteAsync(AffiliateSyncJobArgs args)
    {
        var now = _clock.Now;
        var state = (await _states.GetListAsync(x => x.Platform == args.Platform && x.SyncKind == AffiliateSyncKind.Conversion)).FirstOrDefault();
        if (state is null)
        {
            state = new AffiliateSyncState(_guidGenerator.Create(), args.Platform, AffiliateSyncKind.Conversion);
            await _states.InsertAsync(state, autoSave: true);
        }

        var from = args.From ?? state.Watermark?.AddMinutes(-15) ?? state.InitialStartDate;
        if (!from.HasValue) throw new BusinessException(WebHoanTienDomainErrorCodes.SyncStartDateRequired);
        var to = args.To ?? now;
        if (to - from.Value > TimeSpan.FromDays(93)) from = to.AddMonths(-3);

        var run = new AffiliateSyncRun(_guidGenerator.Create(), args.Platform, args.Kind, from.Value, to, now);
        await _runs.InsertAsync(run, autoSave: true);
        var fetched = 0; var inserted = 0; var updated = 0; var unmatched = 0; var errors = 0;

        try
        {
            var provider = _providers.Get(args.Platform);
            string? scrollId = null;
            do
            {
                var query = new AffiliateConversionQuery(from.Value, to, scrollId, 500);
                var page = args.Kind == AffiliateSyncKind.Reconciliation
                    ? await provider.GetValidatedConversionsAsync(query)
                    : await provider.GetConversionsAsync(query);
                fetched += page.Items.Count;
                await _payloads.InsertAsync(new AffiliateRawPayload(_guidGenerator.Create(), run.Id, null,
                    args.Kind.ToString(), page.SanitizedPayload, now.AddDays(WebHoanTienConsts.RetentionDays)));

                foreach (var source in page.Items)
                {
                    try
                    {
                        var result = await UpsertAsync(args.Platform, source);
                        if (result.Inserted) inserted++; else updated++;
                        if (!result.Matched) unmatched++;
                    }
                    catch (Exception exception)
                    {
                        errors++;
                        _logger.LogError(exception, "Không thể upsert conversion {ConversionId}", source.ExternalConversionId);
                    }
                }
                scrollId = string.IsNullOrWhiteSpace(page.ScrollId) ? null : page.ScrollId;
            } while (scrollId is not null);

            state.Succeeded(to, _clock.Now);
            run.Complete(_clock.Now, fetched, inserted, updated, unmatched, errors, errors == 0 ? null : $"{errors} conversion lỗi");
        }
        catch (Exception exception)
        {
            state.Failed(exception.Message);
            run.Complete(_clock.Now, fetched, inserted, updated, unmatched, errors + 1, exception.Message);
            await _states.UpdateAsync(state, autoSave: true);
            await _runs.UpdateAsync(run, autoSave: true);
            throw;
        }

        await _states.UpdateAsync(state, autoSave: true);
        await _runs.UpdateAsync(run, autoSave: true);
    }

    private async Task<(bool Inserted, bool Matched)> UpsertAsync(AffiliatePlatform platform, NormalizedAffiliateConversion source)
    {
        var conversion = (await _conversions.GetListAsync(x => x.Platform == platform && x.ExternalConversionId == source.ExternalConversionId)).FirstOrDefault();
        var inserted = conversion is null;
        conversion ??= new AffiliateConversion(_guidGenerator.Create(), platform, source.ExternalConversionId, source.PurchaseTime);

        var matched = conversion.TrackingId.HasValue;
        if (!matched && !string.IsNullOrWhiteSpace(source.AttributionValue))
        {
            var tracking = (await _trackings.GetListAsync(x => x.Platform == platform && x.TrackingToken == source.AttributionValue)).FirstOrDefault();
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
            var order = (await _orders.GetListAsync(x => x.ConversionId == conversion.Id && x.ExternalOrderId == sourceOrder.ExternalOrderId)).FirstOrDefault();
            var newOrder = order is null;
            order ??= new AffiliateOrder(_guidGenerator.Create(), conversion.Id, sourceOrder.ExternalOrderId);
            var orderUser = _calculator.CalculateUserCommission(sourceOrder.NetCommission, rule.UserShareRate);
            order.Update(sourceOrder.Status, sourceOrder.ShopType, sourceOrder.PurchaseAmount, sourceOrder.NetCommission, orderUser);
            if (newOrder) await _orders.InsertAsync(order, autoSave: true); else await _orders.UpdateAsync(order, autoSave: true);

            var allocation = _calculator.Allocate(sourceOrder.NetCommission, rule.UserShareRate,
                sourceOrder.Items.Select(x => new CommissionAllocationInput(x.ExternalItemId + ":" + (x.ModelId ?? string.Empty), x.ItemTotalCommission)))
                .ToDictionary(x => x.Key, StringComparer.Ordinal);
            foreach (var sourceItem in sourceOrder.Items)
            {
                var modelId = sourceItem.ModelId?.Trim() ?? string.Empty;
                var item = (await _items.GetListAsync(x => x.OrderId == order.Id && x.ExternalItemId == sourceItem.ExternalItemId && x.ModelId == modelId)).FirstOrDefault();
                var newItem = item is null;
                item ??= new AffiliateOrderItem(_guidGenerator.Create(), order.Id, sourceItem.ExternalItemId, modelId);
                var allocated = allocation[sourceItem.ExternalItemId + ":" + modelId];
                item.Update(sourceItem.ProductName, sourceItem.PurchaseAmount, sourceItem.Quantity, sourceItem.ItemTotalCommission,
                    allocated.NetCommission, allocated.UserCommission, sourceItem.RefundAmount, sourceItem.IsFraud, sourceItem.ProviderStatus);
                if (newItem) await _items.InsertAsync(item); else await _items.UpdateAsync(item);
            }
        }
        return (inserted, matched);
    }
}
