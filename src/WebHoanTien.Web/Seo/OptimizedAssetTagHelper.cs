using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;

namespace WebHoanTien.Web.Seo;

[HtmlTargetElement("link", Attributes = "href")]
[HtmlTargetElement("script", Attributes = "src")]
[HtmlTargetElement("img", Attributes = "src")]
public sealed class OptimizedAssetTagHelper(IWebHostEnvironment environment) : TagHelper
{
    // Rewrite before MVC's versioning helper hashes the optimized bytes.
    public override int Order => -2000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!environment.IsProduction()) return;
        var attribute = output.TagName == "link" ? "href" : "src";
        var value = output.Attributes[attribute]?.Value?.ToString();
        if (value is null || !(value.StartsWith("~/", StringComparison.Ordinal) || value.StartsWith('/')) || value.StartsWith("//")) return;
        var queryIndex = value.IndexOf('?');
        var path = value.TrimStart('~', '/').Split('?')[0];
        var query = queryIndex >= 0 ? value[queryIndex..] : string.Empty;
        string? candidate = path switch {
            "catback/hero-approved-reference.png" => "optimized/hero.webp",
            "catback-mascot-reference.png" => "optimized/mascot.webp",
            _ when !path.Contains('/') && (path.EndsWith(".css") || path.EndsWith(".js")) && path != "service-worker.js" => "optimized/" + path,
            _ => null
        };
        if (candidate is not null && environment.WebRootFileProvider.GetFileInfo(candidate).Exists)
            output.Attributes.SetAttribute(attribute, "~/" + candidate + query);
    }
}
