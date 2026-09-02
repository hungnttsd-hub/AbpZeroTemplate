using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations;

public interface IAffiliateProvider
{
    AffiliatePlatform Platform { get; }
    Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default);
}

public sealed record AffiliateProductOffer(
    string ItemId,
    string? ShopId,
    string? Name,
    string? ImageUrl,
    decimal? EstimatedCommission);

public sealed record NormalizedAffiliateConversion(
    string ExternalConversionId,
    string? AttributionValue,
    DateTime PurchaseTime,
    DateTime? ClickTime,
    AffiliateConversionStatus Status,
    decimal GrossCommission,
    decimal NetCommission,
    CommissionSource CommissionSource,
    IReadOnlyList<NormalizedAffiliateOrder> Orders);

public sealed record NormalizedAffiliateOrder(
    string ExternalOrderId,
    AffiliateOrderStatus Status,
    string? ShopType,
    decimal PurchaseAmount,
    decimal NetCommission,
    IReadOnlyList<NormalizedAffiliateOrderItem> Items);

public sealed record NormalizedAffiliateOrderItem(
    string ExternalItemId,
    string? ModelId,
    string? ProductName,
    decimal PurchaseAmount,
    int Quantity,
    decimal ItemTotalCommission,
    decimal RefundAmount,
    bool IsFraud,
    string? ProviderStatus,
    IReadOnlyList<NormalizedAffiliateOrderItemAttribution> Attributions);

public sealed record NormalizedAffiliateOrderItemAttribution(
    string AttributionValue,
    decimal PurchaseAmount,
    int Quantity,
    decimal ItemTotalCommission,
    decimal RefundAmount,
    bool IsFraud,
    string? ProviderStatus);

public interface IAffiliateProviderRegistry
{
    IAffiliateProvider Get(AffiliatePlatform platform);
}
