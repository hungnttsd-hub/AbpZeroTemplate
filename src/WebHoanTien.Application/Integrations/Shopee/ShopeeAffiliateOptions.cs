namespace WebHoanTien.Integrations.Shopee;

public sealed class ShopeeAffiliateOptions
{
    public const string SectionName = "Shopee";
    public string Endpoint { get; set; } = "https://open-api.affiliate.shopee.vn/graphql";
    public string AppId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    public bool AllowTotalCommissionFallback { get; set; }
}
