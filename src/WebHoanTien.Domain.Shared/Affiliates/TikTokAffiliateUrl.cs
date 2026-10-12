using System;
using System.Text.RegularExpressions;

namespace WebHoanTien.Affiliates;

public static class TikTokAffiliateUrl
{
    public static bool TryNormalize(string? input, out string normalizedUrl, out string? productId) =>
        TryNormalizeCore(input, WebHoanTienConsts.UrlMaxLength, out normalizedUrl, out productId);

    public static bool TryNormalizeProductInput(string? input, out string normalizedUrl, out string? productId)
    {
        if (!TryNormalizeCore(input, WebHoanTienConsts.AffiliateInputUrlMaxLength, out normalizedUrl, out productId))
            return false;
        // Creator Generate General Sharing Links only needs the product ID. Drop the copy/share
        // query here, including og_info, encode_params and device metadata. API sharing links
        // keep using TryNormalize above so their encoded affiliate parameters remain intact.
        if (productId is not null) normalizedUrl = CanonicalProductUrl(productId);
        return normalizedUrl.Length <= WebHoanTienConsts.UrlMaxLength;
    }

    public static string CanonicalProductUrl(string productId) => "https://shop.tiktok.com/vn/pdp/" + productId;

    private static bool TryNormalizeCore(string? input, int maxLength, out string normalizedUrl, out string? productId)
    {
        normalizedUrl = string.Empty;
        productId = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > maxLength ||
            !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || uri.UserInfo.Length > 0 ||
            !(uri.IdnHost.Equals("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
              uri.IdnHost.EndsWith(".tiktok.com", StringComparison.OrdinalIgnoreCase)) || uri.AbsolutePath == "/")
            return false;

        // URL validation only. The dedicated resolver handles approved short-link hosts.
        // Preserve query parameters needed by TikTok links.
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant(), Port = -1 };
        normalizedUrl = builder.Uri.AbsoluteUri;
        var match = Regex.Match(uri.AbsolutePath, @"\A/(?:view/product|(?:[a-z]{2}/)?pdp)/([0-9]{1,30})(?:/|$)", RegexOptions.CultureInvariant);
        if (match.Success) productId = match.Groups[1].Value;
        return normalizedUrl.Length <= maxLength;
    }
}
