using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

[ExposeServices(typeof(IAffiliateUrlNormalizer), typeof(ShopeeUrlNormalizer))]
public class ShopeeUrlNormalizer : IAffiliateUrlNormalizer, ITransientDependency
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "shopee.vn", "www.shopee.vn", "s.shopee.vn", "shope.ee", "vn.shp.ee"
    };

    public bool TryNormalize(string input, out string normalizedUrl, out string? itemId)
    {
        normalizedUrl = string.Empty;
        itemId = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > WebHoanTienConsts.UrlMaxLength ||
            !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !AllowedHosts.Contains(uri.IdnHost) || !string.IsNullOrEmpty(uri.UserInfo) ||
            (!uri.IsDefaultPort && uri.Port != 443))
        {
            return false;
        }

        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant(), Port = -1 };
        if (!IsShortHost(uri.IdnHost))
        {
            if (TryExtractProductIds(uri.AbsolutePath, out var shopId, out var productItemId))
            {
                builder.Path = $"/product/{shopId}/{productItemId}";
                itemId = productItemId;
            }

            builder.Query = string.Empty;
        }

        normalizedUrl = builder.Uri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    public static bool IsShortHost(string host) => host.Equals("s.shopee.vn", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("shope.ee", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("vn.shp.ee", StringComparison.OrdinalIgnoreCase);

    private static bool TryExtractProductIds(string path, out string shopId, out string itemId)
    {
        shopId = string.Empty;
        itemId = string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(path, @"-i\.(\d+)\.(\d+)(?:/|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            match = System.Text.RegularExpressions.Regex.Match(path, @"/(?:product|opaanlp)/(\d+)/(\d+)(?:/|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        shopId = match.Groups[1].Value;
        itemId = match.Groups[2].Value;
        return true;
    }
}

public sealed record CommissionAllocationInput(string Key, decimal Weight);
public sealed record CommissionAllocation(string Key, decimal NetCommission, decimal UserCommission);
public sealed record AmountAllocationInput(string Key, decimal Weight);
public sealed record AmountAllocation(string Key, decimal Amount);
