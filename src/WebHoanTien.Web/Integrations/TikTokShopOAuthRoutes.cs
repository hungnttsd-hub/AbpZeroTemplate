using System;

namespace WebHoanTien.Web.Integrations;

public static class TikTokShopOAuthRoutes
{
    public const string Callback = "/api/tiktok-shop/oauth/callback";
    public const string Result = "/tiktok-shop/connection-result";

    public static bool IsPublicEndpoint(string? path)
    {
        var normalized = path?.TrimEnd('/');
        return string.Equals(normalized, Callback, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Result, StringComparison.OrdinalIgnoreCase);
    }
}
