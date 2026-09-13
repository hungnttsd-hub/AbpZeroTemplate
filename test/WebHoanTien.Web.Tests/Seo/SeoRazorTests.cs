using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Shouldly;
using Xunit;

namespace WebHoanTien.Seo;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class SeoRazorTests : WebHoanTienWebTestBase
{
    [Fact]
    public async Task Home_Should_Render_Seo_In_Initial_Html()
    {
        var html = await GetResponseAsStringAsync("/");
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var canonicalNodes = document.DocumentNode.SelectNodes("//link[@rel='canonical']");
        (canonicalNodes?.Count ?? 0).ShouldBeLessThanOrEqualTo(1);
        if (canonicalNodes is { Count: 1 })
        {
            canonicalNodes[0].GetAttributeValue("href", "").ShouldBe("https://catback.id.vn/");
        }
        document.DocumentNode.SelectSingleNode("//title").InnerText.ShouldNotBeNullOrWhiteSpace();
        document.DocumentNode.SelectSingleNode("//meta[@name='description']").GetAttributeValue("content", "").ShouldNotBeNullOrWhiteSpace();
        document.DocumentNode.SelectNodes("//h1").Count.ShouldBe(1);
        document.DocumentNode.SelectSingleNode("//meta[@property='og:image']").ShouldNotBeNull();
        var structuredData = document.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']");
        if (structuredData is not null) structuredData.InnerText.ShouldContain("schema.org");
    }

    [Theory]
    [InlineData("/does-not-exist-seo")]
    [InlineData("/huong-dan/video/does-not-exist-seo")]
    public async Task Missing_Pages_Should_Return_404(string path)
    {
        var response = await GetResponseAsync(path, HttpStatusCode.NotFound);
        response.Headers.Location.ShouldBeNull();
    }
}
