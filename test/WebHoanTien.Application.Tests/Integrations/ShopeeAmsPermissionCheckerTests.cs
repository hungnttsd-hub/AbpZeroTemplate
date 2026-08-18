using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using WebHoanTien.Integrations.Shopee;
using Xunit;

namespace WebHoanTien.Tests.Integrations;

public class ShopeeAmsPermissionCheckerTests
{
    [Fact]
    public async Task CheckPermission_Should_Return_True_For_An_Empty_Successful_Report()
    {
        Uri? capturedUri = null;
        using var client = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedUri = request.RequestUri;
            return JsonResponse(HttpStatusCode.OK,
                "{\"error\":\"\",\"message\":\"\",\"request_id\":\"request-1\",\"response\":{\"list\":[],\"total_count\":0,\"has_more\":false}}");
        }));
        var options = CreateOptions();
        var checker = new ShopeeAmsPermissionChecker(client, Options.Create(options));

        var result = await checker.CheckPermissionAsync();

        result.IsConfigured.ShouldBeTrue();
        result.HasPermission.ShouldBeTrue();
        result.HttpStatusCode.ShouldBe(200);
        result.Error.ShouldBeNull();
        result.RequestId.ShouldBe("request-1");
        result.ReturnedRecords.ShouldBe(0);

        capturedUri.ShouldNotBeNull();
        capturedUri!.AbsolutePath.ShouldBe("/api/v2/ams/get_conversion_report");
        var query = ParseQuery(capturedUri.Query);
        query["partner_id"].ShouldBe(options.PartnerId);
        query["shop_id"].ShouldBe(options.ShopId);
        query["page_no"].ShouldBe("1");
        query["page_size"].ShouldBe("1");
        var timestamp = long.Parse(query["timestamp"]);
        query["sign"].ShouldBe(ShopeeRequestSigner.GetSignShopee(
            options.PartnerId,
            capturedUri.AbsolutePath,
            timestamp,
            options.AccessToken,
            options.ShopId,
            options.PartnerKey));
    }

    [Fact]
    public async Task CheckPermission_Should_Preserve_Shopee_Error_Details()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK,
                "{\"error\":\"error_permission\",\"message\":\"Access denied\",\"request_id\":\"request-2\"}")));
        var checker = new ShopeeAmsPermissionChecker(client, Options.Create(CreateOptions()));

        var result = await checker.CheckPermissionAsync();

        result.IsConfigured.ShouldBeTrue();
        result.HasPermission.ShouldBeFalse();
        result.Error.ShouldBe("error_permission");
        result.Message.ShouldBe("Access denied");
        result.RequestId.ShouldBe("request-2");
    }

    [Fact]
    public async Task CheckPermission_Should_Not_Send_When_Configuration_Is_Missing()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP request must not be sent.")));
        var checker = new ShopeeAmsPermissionChecker(
            client,
            Options.Create(new ShopeeOpenPlatformOptions()));

        var result = await checker.CheckPermissionAsync();

        result.IsConfigured.ShouldBeFalse();
        result.HasPermission.ShouldBeFalse();
        result.Error.ShouldBe("configuration_missing");
        result.Message.ShouldNotBeNull();
        result.Message!.ShouldContain("PartnerId");
        result.Message.ShouldContain("PartnerKey");
        result.Message.ShouldContain("ShopId");
        result.Message.ShouldContain("AccessToken");
    }

    private static ShopeeOpenPlatformOptions CreateOptions() => new()
    {
        BaseUrl = "https://partner.shopeemobile.com",
        PartnerId = "123456",
        PartnerKey = "test-partner-key",
        ShopId = "600000",
        AccessToken = "test-access-token",
        PermissionCheckDays = 7
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            result[Uri.UnescapeDataString(parts[0])] = parts.Length == 2
                ? Uri.UnescapeDataString(parts[1])
                : string.Empty;
        }

        return result;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(_handler(request));
    }
}
