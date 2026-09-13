using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;

namespace WebHoanTien.Web.Seo;

[HtmlTargetElement("link", Attributes = "href")]
[HtmlTargetElement("script", Attributes = "src")]
[HtmlTargetElement("img", Attributes = "src")]
public sealed class OptimizedAssetTagHelper(IWebHostEnvironment environment, IFileVersionProvider versions) : TagHelper
{
    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;
    // Run after MVC and hash the optimized bytes, not the original file.
    public override int Order => 10000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!environment.IsProduction()) return;
        var attribute = output.TagName == "link" ? "href" : "src";
        var value = output.Attributes[attribute]?.Value?.ToString();
        if (value is null || !(value.StartsWith("~/", StringComparison.Ordinal) || value.StartsWith('/')) || value.StartsWith("//")) return;
        var path = value.TrimStart('~', '/').Split('?')[0];
        string? candidate = path switch {
            "catback/hero-approved-reference.png" => "optimized/hero.webp",
            "catback-mascot-reference.png" => "optimized/mascot.webp",
            _ when !path.Contains('/') && (path.EndsWith(".css") || path.EndsWith(".js")) && path != "service-worker.js" => "optimized/" + path,
            _ => null
        };
        if (candidate is not null && environment.WebRootFileProvider.GetFileInfo(candidate).Exists)
            output.Attributes.SetAttribute(attribute, versions.AddFileVersionToPath(ViewContext.HttpContext.Request.PathBase, "/" + candidate));
    }
}
