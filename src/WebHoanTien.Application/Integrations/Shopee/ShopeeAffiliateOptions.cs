namespace WebHoanTien.Integrations.Shopee;

public sealed class ShopeeAffiliateOptions
{
    public const string SectionName = "Shopee";
    public string AffiliateId { get; set; } = string.Empty;
    public string ProductDataEndpoint { get; set; } = "https://data.addlivetag.com/product-data/product-data.php";
    public int ProductDataTimeoutSeconds { get; set; } = 10;
    public int ShopMetadataTimeoutSeconds { get; set; } = 4;
}
