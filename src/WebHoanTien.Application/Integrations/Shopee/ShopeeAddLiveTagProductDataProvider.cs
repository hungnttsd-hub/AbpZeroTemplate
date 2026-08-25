using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;

namespace WebHoanTien.Integrations.Shopee;

public class ShopeeAddLiveTagProductDataProvider : IAffiliateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShopeeAffiliateOptions _options;

    public ShopeeAddLiveTagProductDataProvider(IHttpClientFactory httpClientFactory, IOptions<ShopeeAffiliateOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public AffiliatePlatform Platform => AffiliatePlatform.Shopee;

    public async Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !Uri.TryCreate(_options.ProductDataEndpoint, UriKind.Absolute, out var endpoint))
        {
            return null;
        }

        var separator = string.IsNullOrWhiteSpace(endpoint.Query) ? "?" : "&";
        var requestUri = endpoint.AbsoluteUri + separator + "item_id=" + Uri.EscapeDataString(itemId);
        using var response = await _httpClientFactory.CreateClient("ShopeeProductData").GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (!root.TryGetProperty("status", out var status) || !string.Equals(status.GetString(), "success", StringComparison.OrdinalIgnoreCase) ||
            !root.TryGetProperty("productInfo", out var product) || product.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new AffiliateProductOffer(
            ReadString(product, "itemId") ?? itemId,
            ReadString(product, "shopId"),
            ReadString(product, "productName"),
            ReadString(product, "imageUrl"),
            ReadDecimal(product, "commission"));
    }

    private static string? ReadString(JsonElement value, string propertyName) =>
        !value.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null
            ? null
            : property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText().Trim('"');

    private static decimal? ReadDecimal(JsonElement value, string propertyName) =>
        decimal.TryParse(ReadString(value, propertyName), NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
}
