using System;
using Shouldly;
using WebHoanTien.Integrations.Shopee;
using Xunit;

namespace WebHoanTien.Tests.Integrations;

public class ShopeeRequestSignerTests
{
    [Fact]
    public void GetSignShopee_Should_Generate_OpenPlatform_Shop_Signature()
    {
        var signature = ShopeeRequestSigner.GetSignShopee(
            "1",
            "/api/v2/ams/get_conversion_report",
            1610000000,
            "c09222e3fc40ffb25fc947f738b1abf1",
            "600000",
            "test-partner-key");

        signature.ShouldBe("914e79e2fa7fba8e869a400229d42c6226fb49b145cd3c3fff11116f4149d2d0");
    }

    [Fact]
    public void GetSignShopee_Should_Reject_Path_Without_Leading_Slash()
    {
        Should.Throw<ArgumentException>(() => ShopeeRequestSigner.GetSignShopee(
            "1",
            "api/v2/ams/get_conversion_report",
            1610000000,
            "access-token",
            "600000",
            "partner-key"));
    }

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
