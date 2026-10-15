using System;

namespace WebHoanTien.TikTokAffiliate;

public static class TikTokDeveloperAccess
{
    public const string UserProperty = "CanUseTikTokDeveloperApi";
    public const string Policy = "CatBack.TikTokDeveloper.Access";
    public const string ApiKeyHeader = "X-CatBack-Api-Key";
    public const string Portal = "/Developer/TikTok";
    public const string StatePrefix = "cbdev_";
    public const string StateCookie = "CatBack.TikTokDeveloper.State";
    public const string ProfilePath = "/affiliate_creator/202508/profiles";
    public const string ProductsPath = "/affiliate_creator/202509/open_collaborations/products";
    public const string LinksPath = "/affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch";
    public const string OrdersPath = "/affiliate_creator/202410/orders/search";
    public const string TokenPath = "/api/developer/tiktok/token";

    public static bool IsApiPath(string? path)
    {
        path = path?.TrimEnd('/');
        return string.Equals(path, ProfilePath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, ProductsPath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, LinksPath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, OrdersPath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, TokenPath, StringComparison.OrdinalIgnoreCase);
    }
}
