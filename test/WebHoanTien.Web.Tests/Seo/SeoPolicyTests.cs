using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WebHoanTien.Web.Seo;
using Xunit;

namespace WebHoanTien.Seo;

public class SeoPolicyTests
{
    private static TestServer Server(string environment = "Production") => new(new WebHostBuilder()
        .UseEnvironment(environment)
        .ConfigureServices(services => {
            services.Configure<SeoOptions>(options => options.BaseUrl = "https://catback.id.vn");
            services.AddSingleton<SeoMetadataProvider>();
        })
        .Configure(app => {
            app.UseMiddleware<SeoResponseMiddleware>();
            app.Run(async context => {
                if (context.Request.Path == "/missing") { context.Response.StatusCode = 404; return; }
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html>fixture</html>");
            });
        }));

    [Fact]
    public async Task Discovery_Should_Return_Public_Urls_Only()
    {
        using var server = Server();
        using var client = server.CreateClient();
        client.BaseAddress = new Uri("https://catback.id.vn");
        var robots = await client.GetAsync("/robots.txt");
        robots.StatusCode.ShouldBe(HttpStatusCode.OK);
        robots.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        (await robots.Content.ReadAsStringAsync()).ShouldContain("Sitemap: https://catback.id.vn/sitemap.xml");
        var response = await client.GetAsync("/sitemap.xml");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/xml");
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = xml.Descendants(ns + "loc").Select(x => x.Value).ToArray();
        urls.Length.ShouldBe(7);
        urls.ShouldContain("https://catback.id.vn/");
        urls.ShouldContain("https://catback.id.vn/Legal/Terms");
        foreach (var url in urls) {
            url.ShouldNotContain('?');
            new Uri(url).AbsolutePath.ShouldNotStartWith("/Account");
            new Uri(url).AbsolutePath.ShouldNotStartWith("/Admin");
            new Uri(url).AbsolutePath.ShouldNotStartWith("/Wallet");
            new Uri(url).AbsolutePath.ShouldNotStartWith("/Orders");
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public async Task NonProduction_Should_Noindex_And_Empty_Sitemap(string environment)
    {
        using var server = Server(environment);
        using var client = server.CreateClient();
        client.BaseAddress = new Uri("https://catback.id.vn");
        var home = await client.GetAsync("/");
        home.Headers.GetValues("X-Robots-Tag").Single().ShouldBe("noindex, nofollow");
        (await client.GetStringAsync("/sitemap.xml")).ShouldNotContain("<loc>");
    }

    [Fact]
    public async Task Alias_Normalization_Should_Preserve_Query_In_One_Redirect()
    {
        using var server = Server();
        using var client = server.CreateClient();
        var response = await client.GetAsync("http://www.catback.id.vn/Index/?handler=More&skip=5&utm_source=test");
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.AbsoluteUri.ShouldBe("https://catback.id.vn/?handler=More&skip=5&utm_source=test");
        var post = await client.PostAsync("http://www.catback.id.vn/Index/?handler=Prepare", new System.Net.Http.StringContent(""));
        post.StatusCode.ShouldBe(HttpStatusCode.OK);
        var callback = await client.GetAsync("http://www.catback.id.vn/signin-google?code=secret");
        callback.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/Orders")]
    [InlineData("/Wallet/Withdraw")]
    [InlineData("/Account/Profile")]
    [InlineData("/Admin/Affiliates")]
    [InlineData("/?handler=More&skip=5")]
    public async Task Private_And_Functional_Responses_Should_Not_Be_Indexed_Or_Cached(string path)
    {
        using var server = Server();
        using var client = server.CreateClient();
        var response = await client.GetAsync("https://catback.id.vn" + path);
        response.Headers.GetValues("X-Robots-Tag").Single().ShouldStartWith("noindex");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        response.Headers.CacheControl.Private.ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_Url_Should_Retain_404_Without_Redirect()
    {
        using var server = Server();
        using var client = server.CreateClient();
        var response = await client.GetAsync("https://catback.id.vn/missing");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.Location.ShouldBeNull();
        response.Headers.GetValues("X-Robots-Tag").Single().ShouldStartWith("noindex");
    }

    [Fact]
    public void Metadata_Should_Clean_Tracking_Without_Leaking_Functional_Data()
    {
        using var server = Server();
        var seo = server.Services.GetRequiredService<SeoMetadataProvider>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("catback.id.vn");
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString("?utm_source=test&fbclid=secret");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "private@example.test") }, "test"));
        var metadata = seo.Create(context);
        metadata.CanonicalUrl.ShouldBe("https://catback.id.vn/");
        metadata.Title.ShouldNotBeNullOrWhiteSpace();
        metadata.Description.ShouldNotBeNullOrWhiteSpace();
        metadata.StructuredData!.ShouldNotContain("private@example.test");
        JsonDocument.Parse(metadata.StructuredData!).RootElement.GetProperty("@type").GetString().ShouldBe("Organization");
        context.Request.QueryString = new QueryString("?handler=More&token=secret");
        seo.Create(context).CanonicalUrl.ShouldBeNull();
        seo.Create(context).StructuredData.ShouldBeNull();
        context.Request.QueryString = QueryString.Empty;
        context.Request.Path = "/huong-dan/video/cai-dat";
        var crumbs = JsonDocument.Parse(seo.Create(context).StructuredData!).RootElement.GetProperty("itemListElement");
        crumbs.GetArrayLength().ShouldBe(3);
        crumbs[1].GetProperty("@type").GetString().ShouldBe("ListItem");
        context.Request.Host = new HostString("staging.example.test");
        seo.Create(context).Robots.ShouldBe("noindex, nofollow");
    }
}
