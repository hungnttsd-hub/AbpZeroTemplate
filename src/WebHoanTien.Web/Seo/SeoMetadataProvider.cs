using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace WebHoanTien.Web.Seo;

public sealed class SeoMetadataProvider(IOptions<SeoOptions> options, IWebHostEnvironment environment)
{
    public SeoOptions Options => options.Value;
    public Uri Origin => new(Options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    public string Absolute(string path) => new Uri(Origin, path).AbsoluteUri;
    public bool ProductionIndexing => environment.IsProduction() && Options.EnableIndexing;
    public bool IsPrimaryHost(HttpRequest request) => request.Host.Host.Equals(Origin.Host, StringComparison.OrdinalIgnoreCase);
    public bool CanIndex(HttpRequest request) => ProductionIndexing && IsPrimaryHost(request)
        && (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        && SeoPageCatalog.Find(request.Path.Value) is not null && HasOnlyTrackingQuery(request);

    public static bool HasOnlyTrackingQuery(HttpRequest request) => request.Query.Keys.All(key =>
        key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("fbclid", StringComparison.OrdinalIgnoreCase) || key.Equals("gclid", StringComparison.OrdinalIgnoreCase)
        || key.Equals("msclkid", StringComparison.OrdinalIgnoreCase));

    public SeoMetadata Create(HttpContext context, string? pageTitle = null)
    {
        var page = SeoPageCatalog.Find(context.Request.Path.Value);
        var publicContent = ProductionIndexing && IsPrimaryHost(context.Request) && page is not null && HasOnlyTrackingQuery(context.Request)
            && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method));
        var title = page?.Path == "/" ? Options.DefaultTitle : WithBrand(page?.Title ?? pageTitle ?? "CatBack");
        var description = page is not null && page.Path != "/" ? page.Description : Options.DefaultDescription;
        var robots = CanIndex(context.Request) && context.Response.StatusCode < 400 ? "index, follow"
            : environment.IsProduction() && context.Response.StatusCode < 400
              && context.Request.Path.StartsWithSegments("/Account") ? "noindex, follow" : "noindex, nofollow";
        var canonical = publicContent && context.Response.StatusCode < 400 ? Absolute(page!.Path) : null;
        return new(title, description, canonical, robots, title, description, Absolute(Options.DefaultOgImage),
            StructuredData: canonical is not null ? BuildStructuredData(page!) : null);
    }

    private string WithBrand(string title)
    {
        foreach (var suffix in new[] { " | " + Options.SiteName, " — " + Options.SiteName, " - " + Options.SiteName })
            if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) title = title[..^suffix.Length];
        return title.Equals(Options.SiteName, StringComparison.OrdinalIgnoreCase) ? title : title + " | " + Options.SiteName;
    }

    private string BuildStructuredData(SeoPage page)
    {
        if (page.Path == "/")
            return JsonSerializer.Serialize(new Dictionary<string, object> {
                ["@context"] = "https://schema.org", ["@type"] = "Organization",
                ["name"] = Options.SiteName, ["url"] = Absolute("/"), ["logo"] = Absolute(Options.Logo)
            });
        var crumbs = new List<object> { new { @type = "ListItem", position = 1, name = "Trang chủ", item = Absolute("/") } };
        if (page.Path.StartsWith("/huong-dan/video/", StringComparison.Ordinal))
            crumbs.Add(new { @type = "ListItem", position = 2, name = "Hướng dẫn", item = Absolute("/huong-dan") });
        crumbs.Add(new { @type = "ListItem", position = crumbs.Count + 1, name = page.Title, item = Absolute(page.Path) });
        // Dictionaries retain the JSON-LD '@type' key (C# escaped identifiers do not).
        return JsonSerializer.Serialize(new Dictionary<string, object> {
            ["@context"] = "https://schema.org", ["@type"] = "BreadcrumbList",
            ["itemListElement"] = crumbs.Select((crumb, index) => {
                var value = JsonSerializer.SerializeToElement(crumb);
                return new Dictionary<string, object> { ["@type"] = "ListItem", ["position"] = index + 1,
                    ["name"] = value.GetProperty("name").GetString()!, ["item"] = value.GetProperty("item").GetString()! };
            }).ToArray()
        });
    }

    public string Sitemap(bool includePages)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + new XElement(ns + "urlset",
            includePages ? SeoPageCatalog.Pages.Select(page => new XElement(ns + "url", new XElement(ns + "loc", Absolute(page.Path)))) : null);
    }
}
