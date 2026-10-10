using System;
using System.Text.RegularExpressions;

namespace WebHoanTien.Affiliates;

public static class TikTokAffiliateUrl
{
    public static bool TryNormalize(string? input, out string normalizedUrl, out string? productId)
    {
        normalizedUrl = string.Empty;
        productId = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > WebHoanTienConsts.UrlMaxLength ||
            !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || uri.UserInfo.Length > 0 ||
            !(uri.IdnHost.Equals("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
              uri.IdnHost.EndsWith(".tiktok.com", StringComparison.OrdinalIgnoreCase)) || uri.AbsolutePath == "/")
            return false;

        // Do not fetch short URLs from the application server; RioHub resolves them.
        // Preserve query parameters needed by TikTok links.
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant(), Port = -1 };
        normalizedUrl = builder.Uri.AbsoluteUri;
        var match = Regex.Match(uri.AbsolutePath, @"\A/(?:view/product|(?:[a-z]{2}/)?pdp)/([0-9]{1,30})(?:/|$)", RegexOptions.CultureInvariant);
        if (match.Success) productId = match.Groups[1].Value;
        return normalizedUrl.Length <= WebHoanTienConsts.UrlMaxLength;
    }
}
