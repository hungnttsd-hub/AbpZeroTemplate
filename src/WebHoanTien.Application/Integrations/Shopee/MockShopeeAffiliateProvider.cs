using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.Shopee;

public class MockShopeeAffiliateProvider : IAffiliateProvider
{
    public AffiliatePlatform Platform => AffiliatePlatform.Shopee;
    public Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult<AffiliateProductOffer?>(new AffiliateProductOffer(itemId, "mock-shop", "Sản phẩm Shopee mẫu", null, 7000m));
}
