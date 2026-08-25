using System;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace WebHoanTien.Integrations.Shopee;

public class ShopeeAffiliateLinkBuilder
{
    private const string AffiliateRedirectEndpoint = "https://s.shopee.vn/an_redir";
    private readonly ShopeeAffiliateOptions _options;

    public ShopeeAffiliateLinkBuilder(IOptions<ShopeeAffiliateOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string originUrl, string trackingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AffiliateId))
        {
            throw new UserFriendlyException(
                "Chưa cấu hình Shopee Affiliate ID. Liên hệ quản trị viên để thiết lập SHOPEE_AFFILIATE_ID trước khi tạo link.",
                code: WebHoanTienDomainErrorCodes.ProviderNotConfigured);
        }

        var origin = new Uri(originUrl, UriKind.Absolute);
        var canonicalOrigin = new UriBuilder(origin)
        {
            Fragment = string.Empty,
            Query = string.Empty
        }.Uri.AbsoluteUri.TrimEnd('/');

        return $"{AffiliateRedirectEndpoint}?origin_link={Uri.EscapeDataString(canonicalOrigin)}" +
               $"&affiliate_id={Uri.EscapeDataString(_options.AffiliateId.Trim())}" +
               $"&sub_id={Uri.EscapeDataString(trackingToken)}";
    }
}
