using System;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.Shopee;

public class ShopeeAffiliateLinkBuilder
{
    private const string AffiliateRedirectEndpoint = "https://s.shopee.vn/an_redir";

    public string Build(string originUrl, string trackingToken, string affiliateId)
    {
        var normalizedAffiliateId = AffiliateIdRules.Normalize(affiliateId);

        var origin = new Uri(originUrl, UriKind.Absolute);
        var canonicalOrigin = new UriBuilder(origin)
        {
            Fragment = string.Empty,
            Query = string.Empty
        }.Uri.AbsoluteUri.TrimEnd('/');

        return $"{AffiliateRedirectEndpoint}?origin_link={Uri.EscapeDataString(canonicalOrigin)}" +
               $"&affiliate_id={Uri.EscapeDataString(normalizedAffiliateId)}" +
               $"&sub_id={Uri.EscapeDataString(trackingToken)}";
    }
}
