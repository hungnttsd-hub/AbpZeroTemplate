using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebHoanTien.Web.Seo;

public sealed class SeoResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SeoMetadataProvider seo)
    {
        var request = context.Request;
        var path = request.Path.Value ?? "/";
        context.Response.OnStarting(() => {
            var html = context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
            if (!seo.CanIndex(request) || context.Response.StatusCode >= 400)
                context.Response.Headers["X-Robots-Tag"] = seo.Create(context).Robots;
            if (html) context.Response.Headers.CacheControl = "private, no-store";
            return Task.CompletedTask;
        });

        var safeMethod = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
        var page = SeoPageCatalog.Find(path);
        // Only known public content routes are normalized. Preserve query bytes and all POST/callback flows.
        if (safeMethod && page is not null && seo.ProductionIndexing && seo.Options.EnableCanonicalRedirects
            && (seo.IsPrimaryHost(request) || request.Host.Host.Equals("www." + seo.Origin.Host, StringComparison.OrdinalIgnoreCase))
            && (!request.IsHttps || !seo.IsPrimaryHost(request) || path != page.Path))
        {
            context.Response.Redirect(seo.Absolute(page.Path) + request.QueryString, permanent: true);
            return;
        }

        // Serve discovery before authentication/consent middleware, without user-specific data or database work.
        if (safeMethod && (path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) || path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase)))
        {
            var sitemap = path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase);
            context.Response.ContentType = sitemap ? "application/xml; charset=utf-8" : "text/plain; charset=utf-8";
            var enabled = seo.ProductionIndexing && seo.IsPrimaryHost(request);
            context.Response.Headers.CacheControl = "no-cache";
            if (!HttpMethods.IsHead(request.Method))
                await context.Response.WriteAsync(sitemap ? seo.Sitemap(enabled)
                    : "User-agent: *\nAllow: /\n" + (enabled ? "\nSitemap: " + seo.Absolute("/sitemap.xml") + "\n" : ""));
            return;
        }
        await next(context);
    }
}
