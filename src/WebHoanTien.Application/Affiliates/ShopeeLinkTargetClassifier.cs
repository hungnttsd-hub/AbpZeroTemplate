using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public class ShopeeLinkTargetClassifier : ITransientDependency
{
    private static readonly Regex ShopUsernamePattern = new(
        @"^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ProductSlugPattern = new(
        @"-i\.\d+\.\d+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> ReservedRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "account", "affiliate", "api", "bundle-deal", "bundle-deals", "buyer", "campaign", "cart",
        "checkout", "collection", "collections", "daily_discover", "flash_sale", "help", "index",
        "live", "login", "mall", "m", "notifications", "official-shop", "opaanlp", "orders",
        "product", "search", "seller", "settings", "shop", "top-products", "user", "verify", "video",
        "voucher", "web", "webchat"
    };

    public AffiliateLinkTargetType Classify(string normalizedUrl)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            (!uri.IdnHost.Equals("shopee.vn", StringComparison.OrdinalIgnoreCase) &&
             !uri.IdnHost.Equals("www.shopee.vn", StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return AffiliateLinkTargetType.Unknown;
        }

        var escapedSegments = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segments = new string[escapedSegments.Length];
        for (var index = 0; index < escapedSegments.Length; index++)
        {
            string segment;
            try
            {
                segment = Uri.UnescapeDataString(escapedSegments[index]);
            }
            catch (UriFormatException)
            {
                return AffiliateLinkTargetType.Unknown;
            }

            if (segment.Length == 0 || segment is "." or ".." || segment.Contains('/') || segment.Contains('\\') ||
                HasControlCharacter(segment))
            {
                return AffiliateLinkTargetType.Unknown;
            }

            segments[index] = segment;
        }

        if ((segments.Length == 1 && ProductSlugPattern.IsMatch(segments[0])) ||
            (segments.Length == 3 &&
             (segments[0].Equals("product", StringComparison.OrdinalIgnoreCase) ||
              segments[0].Equals("opaanlp", StringComparison.OrdinalIgnoreCase)) &&
             IsDigitsOnly(segments[1]) && IsDigitsOnly(segments[2])))
        {
            return AffiliateLinkTargetType.Product;
        }

        if (segments.Length == 2 && segments[0].Equals("shop", StringComparison.OrdinalIgnoreCase) &&
            IsDigitsOnly(segments[1]))
        {
            return AffiliateLinkTargetType.Shop;
        }

        if (segments.Length == 1 && !ReservedRoutes.Contains(segments[0]) &&
            ShopUsernamePattern.IsMatch(segments[0]))
        {
            return AffiliateLinkTargetType.Shop;
        }

        return AffiliateLinkTargetType.Unknown;
    }

    private static bool IsDigitsOnly(string value)
    {
        if (value.Length == 0) return false;
        foreach (var character in value)
        {
            if (character is < '0' or > '9') return false;
        }

        return true;
    }

    private static bool HasControlCharacter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character)) return true;
        }

        return false;
    }
}
