namespace WebHoanTien.Web.Seo;

public sealed record SeoMetadata(string Title, string Description, string? CanonicalUrl,
    string Robots, string OgTitle, string OgDescription, string OgImage,
    string OgType = "website", string? StructuredData = null);

public sealed record SeoPage(string Path, string Title, string Description);
