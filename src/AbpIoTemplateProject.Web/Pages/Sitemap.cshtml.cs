using System.Security;
using System.Text;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages;

public class SitemapModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    public SitemapModel(IStorefrontAppService storefrontAppService) { _storefrontAppService = storefrontAppService; }

    public async Task<IActionResult> OnGetAsync()
    {
        var root = $"{Request.Scheme}://{Request.Host}";
        var products = await _storefrontAppService.GetProductsAsync(new ProductListInput { MaxResultCount = StoreConsts.MaxPageSize });
        var articles = await _storefrontAppService.GetArticlesAsync(new PagedAndSortedResultRequestDto { MaxResultCount = StoreConsts.MaxPageSize });
        var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var path in new[] { "/", "/products", "/articles", "/stores", "/contact", "/track-order" })
        {
            AppendUrl(xml, root + path);
        }
        foreach (var product in products.Items) { AppendUrl(xml, $"{root}/products/{product.Slug}"); }
        foreach (var article in articles.Items) { AppendUrl(xml, $"{root}/articles/{article.Slug}"); }
        xml.Append("</urlset>");
        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void AppendUrl(StringBuilder xml, string url)
    {
        xml.Append("<url><loc>").Append(SecurityElement.Escape(url)).Append("</loc></url>");
    }
}
