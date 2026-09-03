using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebHoanTien.Integrations.Shopee;

public sealed record ShopeeShopMetadata(string? ShopId, string DisplayName, string? ImageUrl);

public class ShopeeShopMetadataProvider
{
    private const string Endpoint = "https://shopee.vn/api/v4/shop/get_shop_detail";
    private const string DefaultDisplayName = "Cửa hàng Shopee";
    private const string ImageBaseUrl = "https://down-vn.img.susercontent.com/file/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShopeeShopMetadataProvider> _logger;

    public ShopeeShopMetadataProvider(IHttpClientFactory httpClientFactory,
        ILogger<ShopeeShopMetadataProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ShopeeShopMetadata> GetAsync(string normalizedUrl,
        CancellationToken cancellationToken = default)
    {
        var locator = ReadLocator(normalizedUrl);
        var fallback = CreateFallback(locator.ShopId);
        if (locator.QueryName is null || locator.QueryValue is null) return fallback;

        try
        {
            var requestUri = $"{Endpoint}?{locator.QueryName}={Uri.EscapeDataString(locator.QueryValue)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Referrer = new Uri(normalizedUrl);
            using var response = await _httpClientFactory.CreateClient("ShopeeShopMetadata")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Shopee shop metadata returned HTTP {StatusCode} for {ShopUrl}.",
                    (int)response.StatusCode, normalizedUrl);
                return fallback;
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            return ReadMetadata(document.RootElement, fallback);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Không lấy được metadata cho Shop Shopee {ShopUrl}.", normalizedUrl);
            return fallback;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            _logger.LogWarning(exception, "Không lấy được metadata cho Shop Shopee {ShopUrl}.", normalizedUrl);
            return fallback;
        }
    }

    internal static ShopeeShopMetadata ReadMetadata(JsonElement root, ShopeeShopMetadata fallback)
    {
        if (!root.TryGetProperty("error", out var error) || !IsSuccess(error) ||
            !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return fallback;
        }

        var shopId = ReadString(data, "shopid") ?? fallback.ShopId;
        var name = ReadString(data, "name");
        var displayName = !string.IsNullOrWhiteSpace(name)
            ? Truncate(name.Trim(), 500)
            : CreateFallback(shopId).DisplayName;
        string? imageUrl = null;
        if (data.TryGetProperty("account", out var account) && account.ValueKind == JsonValueKind.Object)
        {
            var portrait = ReadString(account, "portrait");
            if (!string.IsNullOrWhiteSpace(portrait) && IsSafeImageKey(portrait))
                imageUrl = ImageBaseUrl + portrait;
        }

        return new ShopeeShopMetadata(shopId, displayName, imageUrl);
    }

    private static ShopLocator ReadLocator(string normalizedUrl)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)) return default;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2 && segments[0].Equals("shop", StringComparison.OrdinalIgnoreCase) &&
            ulong.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return new ShopLocator("shopid", segments[1], segments[1]);
        }

        return segments.Length == 1
            ? new ShopLocator("username", Uri.UnescapeDataString(segments[0]), null)
            : default;
    }

    private static ShopeeShopMetadata CreateFallback(string? shopId) => new(
        shopId,
        string.IsNullOrWhiteSpace(shopId) ? DefaultDisplayName : $"Shop #{shopId}",
        null);

    private static string? ReadString(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;
        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ValueKind == JsonValueKind.Number ? property.GetRawText() : null;
    }

    private static bool IsSuccess(JsonElement error) => error.ValueKind switch
    {
        JsonValueKind.Number => error.TryGetInt32(out var value) && value == 0,
        JsonValueKind.String => string.IsNullOrWhiteSpace(error.GetString()) || error.GetString() == "0",
        JsonValueKind.Null => true,
        _ => false
    };

    private static bool IsSafeImageKey(string value)
    {
        if (value.Length is < 1 or > 200) return false;
        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_') return false;
        }

        return true;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private readonly record struct ShopLocator(string? QueryName, string? QueryValue, string? ShopId);
}
