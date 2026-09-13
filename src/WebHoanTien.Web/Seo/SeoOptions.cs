namespace WebHoanTien.Web.Seo;

public sealed class SeoOptions
{
    public string SiteName { get; set; } = "CatBack";
    public string BaseUrl { get; set; } = "";
    public string DefaultTitle { get; set; } = "CatBack - Hoàn tiền Shopee khi mua hàng online";
    public string DefaultDescription { get; set; } = "Tạo link mua hàng Shopee và nhận lại một phần hoa hồng affiliate. Theo dõi đơn hàng, tiền hoàn và rút tiền trực tiếp trên CatBack.";
    public string DefaultOgImage { get; set; } = "/catback/hero-approved-reference.png";
    public string Logo { get; set; } = "/catback-mascot-reference.png";
    public string? GoogleSiteVerification { get; set; }
    public bool EnableIndexing { get; set; } = true;
    public bool EnableCanonicalRedirects { get; set; } = true;
}
