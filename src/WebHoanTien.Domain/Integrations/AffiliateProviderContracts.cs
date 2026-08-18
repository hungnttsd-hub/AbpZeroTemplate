using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations;

public interface IAffiliateProvider
{
    AffiliatePlatform Platform { get; }
    Task<AffiliateShortLinkResult> GenerateShortLinkAsync(string originUrl, string trackingToken, CancellationToken cancellationToken = default);
    Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default);
    Task<AffiliateConversionPage> GetConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default);
    Task<AffiliateConversionPage> GetValidatedConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default);
}

public sealed record AffiliateShortLinkResult(string Url);

public sealed record AffiliateProductOffer(
    string ItemId,
    string? ShopId,
    string? Name,
    string? ImageUrl,
    decimal? EstimatedCommission);

public sealed record AffiliateConversionQuery(
    DateTime From,
    DateTime To,
    string? ScrollId = null,
    int Limit = 500,
    string? ValidationId = null);

public sealed record AffiliateConversionPage(
    IReadOnlyList<NormalizedAffiliateConversion> Items,
    string? ScrollId,
    string SanitizedPayload);

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
    string? ProviderStatus);

public interface IAffiliateProviderRegistry
{
    IAffiliateProvider Get(AffiliatePlatform platform);
}
