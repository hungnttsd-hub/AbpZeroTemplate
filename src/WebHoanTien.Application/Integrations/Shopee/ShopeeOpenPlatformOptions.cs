namespace WebHoanTien.Integrations.Shopee;

public sealed class ShopeeOpenPlatformOptions
{
    public const string SectionName = "Shopee:OpenPlatform";

    public string BaseUrl { get; set; } = "https://partner.shopeemobile.com";
    public string PartnerId { get; set; } = string.Empty;
    public string PartnerKey { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public int PermissionCheckDays { get; set; } = 7;
    public int TimeoutSeconds { get; set; } = 15;
}
