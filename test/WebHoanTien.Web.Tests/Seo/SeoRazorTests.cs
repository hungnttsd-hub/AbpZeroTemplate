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
        document.DocumentNode.SelectNodes("//link[@rel='canonical']").Count.ShouldBe(1);
        document.DocumentNode.SelectSingleNode("//link[@rel='canonical']").GetAttributeValue("href", "").ShouldBe("https://catback.id.vn/");
        document.DocumentNode.SelectSingleNode("//title").InnerText.ShouldNotBeNullOrWhiteSpace();
        document.DocumentNode.SelectSingleNode("//meta[@name='description']").GetAttributeValue("content", "").ShouldNotBeNullOrWhiteSpace();
        document.DocumentNode.SelectNodes("//h1").Count.ShouldBe(1);
        document.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']").ShouldNotBeNull();
        document.DocumentNode.SelectSingleNode("//meta[@property='og:image']").ShouldNotBeNull();
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
