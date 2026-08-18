using Shouldly;
using WebHoanTien.Integrations.Shopee;
using Xunit;

namespace WebHoanTien.Tests.Integrations;

public class ShopeeRequestSignerTests
{
    [Fact]
    public void Signature_Should_Use_Exact_Payload_Bytes()
    {
        const string payload = "{\"query\":\"query{productOfferV2(itemId:1,page:1,limit:1){nodes{itemId}}}\"}";
        var first = ShopeeRequestSigner.Sign("123456", 1577836800, payload, "secret");
        var second = ShopeeRequestSigner.Sign("123456", 1577836800, payload + " ", "secret");
        first.ShouldBe("6cba9b41175c3024ff4255e2ada90309e86c532757b072a3bdefc08f76f29c95");
        second.ShouldNotBe(first);
        ShopeeRequestSigner.CreateAuthorization("123456", 1577836800, first)
            .ShouldBe($"SHA256 Credential=123456, Signature={first}, Timestamp=1577836800");
    }
}
