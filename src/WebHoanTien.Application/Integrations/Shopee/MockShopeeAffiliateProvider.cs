using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.Shopee;

public class MockShopeeAffiliateProvider : IAffiliateProvider
{
    public AffiliatePlatform Platform => AffiliatePlatform.Shopee;
    public Task<AffiliateShortLinkResult> GenerateShortLinkAsync(string originUrl, string trackingToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AffiliateShortLinkResult("https://s.shopee.vn/" + trackingToken[..Math.Min(12, trackingToken.Length)]));
    public Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult<AffiliateProductOffer?>(new AffiliateProductOffer(itemId, "mock-shop", "Sản phẩm Shopee mẫu", null, 7000m));
    public Task<AffiliateConversionPage> GetConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default) => Empty();
    public Task<AffiliateConversionPage> GetValidatedConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default) => Empty();
    private static Task<AffiliateConversionPage> Empty() => Task.FromResult(new AffiliateConversionPage(Array.Empty<NormalizedAffiliateConversion>(), null, "{}"));
}
