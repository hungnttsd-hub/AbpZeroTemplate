using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.Tests.Affiliates;

public class ShopeeShortLinkResolverTests
{
    [Fact]
    public async Task Should_Read_Product_Url_From_Shopee_Short_Link_Page()
    {
        const string html = """
            <script>
            var CONFIG={httpUrl:"https:\/\/shopee.vn\/product\/454758975\/27744650586?d_id=b6ec2\u0026uls_trackid=abc",deepLinkUrl:"shopeevn:\/\/reactPath"};
            </script>
            """;
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        };
        response.Content.Headers.ContentType = new("text/html");

        var method = typeof(SafeAffiliateUrlResolver).GetMethod(
            "TryReadResolvedUrlAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();

        var task = (Task<string?>)method.Invoke(null, new object[] { response, CancellationToken.None })!;
        var result = await task;

        result.ShouldBe("https://shopee.vn/product/454758975/27744650586?d_id=b6ec2&uls_trackid=abc");
    }

    [Fact]
    public async Task Should_Read_Shop_Url_From_Shopee_Short_Link_Page()
    {
        const string html = """
            <script>
            var CONFIG={httpUrl:"https:\/\/shopee.vn\/catsback.official?utm_source=share",deepLinkUrl:"shopeevn:\/\/shop"};
            </script>
            """;
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        };
        response.Content.Headers.ContentType = new("text/html");

        var method = typeof(SafeAffiliateUrlResolver).GetMethod(
            "TryReadResolvedUrlAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();

        var task = (Task<string?>)method.Invoke(null, new object[] { response, CancellationToken.None })!;
        var result = await task;

        result.ShouldBe("https://shopee.vn/catsback.official?utm_source=share");
    }
}
