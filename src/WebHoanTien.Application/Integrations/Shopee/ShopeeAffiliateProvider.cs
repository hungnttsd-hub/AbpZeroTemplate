using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Settings;
using WebHoanTien.Affiliates;
using WebHoanTien.Settings;

namespace WebHoanTien.Integrations.Shopee;

public class ShopeeAffiliateProvider : IAffiliateProvider
{
    private const string ConversionFields = "purchaseTime clickTime conversionId shopeeCommissionCapped sellerCommission totalCommission netCommission utmContent orders { orderId orderStatus shopType items { itemId modelId itemName actualAmount qty itemTotalCommission refundAmount fraudStatus displayItemStatus } }";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShopeeAffiliateOptions _options;
    private readonly ILogger<ShopeeAffiliateProvider> _logger;
    private readonly ISettingProvider _settingProvider;
    private int _consecutiveFailures;
    private DateTime _circuitOpenUntil;

    public AffiliatePlatform Platform => AffiliatePlatform.Shopee;

    public ShopeeAffiliateProvider(IHttpClientFactory httpClientFactory, IOptions<ShopeeAffiliateOptions> options,
        ILogger<ShopeeAffiliateProvider> logger, ISettingProvider settingProvider)
    { _httpClientFactory = httpClientFactory; _options = options.Value; _logger = logger; _settingProvider = settingProvider; }

    public async Task<AffiliateShortLinkResult> GenerateShortLinkAsync(string originUrl, string trackingToken, CancellationToken cancellationToken = default)
    {
        var escapedUrl = JsonSerializer.Serialize(originUrl);
        var escapedToken = JsonSerializer.Serialize(trackingToken);
        var query = $"mutation{{generateShortLink(input:{{originUrl:{escapedUrl},subIds:[{escapedToken}]}}){{shortLink}}}}";
        using var document = await ExecuteAsync(query, cancellationToken);
        var url = document.RootElement.GetProperty("data").GetProperty("generateShortLink").GetProperty("shortLink").GetString();
        if (string.IsNullOrWhiteSpace(url)) throw ProviderError("Shopee không trả về shortLink.");
        return new AffiliateShortLinkResult(url);
    }

    public async Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(itemId, NumberStyles.None, CultureInfo.InvariantCulture, out var numericItemId)) return null;
        var query = $"query{{productOfferV2(itemId:{numericItemId},page:1,limit:1){{nodes{{itemId shopId productName imageUrl commission}}}}}}";
        using var document = await ExecuteAsync(query, cancellationToken);
        var nodes = document.RootElement.GetProperty("data").GetProperty("productOfferV2").GetProperty("nodes");
        if (nodes.GetArrayLength() == 0) return null;
        var node = nodes[0];
        return new AffiliateProductOffer(ReadString(node, "itemId") ?? itemId, ReadString(node, "shopId"),
            ReadString(node, "productName"), ReadString(node, "imageUrl"), ReadDecimal(node, "commission"));
    }

    public Task<AffiliateConversionPage> GetConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default)
    {
        var args = new List<string>
        {
            "purchaseTimeStart:" + new DateTimeOffset(query.From).ToUnixTimeSeconds(),
            "purchaseTimeEnd:" + new DateTimeOffset(query.To).ToUnixTimeSeconds(),
            "limit:" + Math.Clamp(query.Limit, 1, 500)
        };
        if (!string.IsNullOrWhiteSpace(query.ScrollId)) args.Add("scrollId:" + JsonSerializer.Serialize(query.ScrollId));
        return FetchConversionsAsync("conversionReport", string.Join(',', args), query, false, cancellationToken);
    }

    public Task<AffiliateConversionPage> GetValidatedConversionsAsync(AffiliateConversionQuery query, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "limit:" + Math.Clamp(query.Limit, 1, 500) };
        if (!string.IsNullOrWhiteSpace(query.ScrollId)) args.Add("scrollId:" + JsonSerializer.Serialize(query.ScrollId));
        if (!string.IsNullOrWhiteSpace(query.ValidationId) && long.TryParse(query.ValidationId, out var id)) args.Add("validationId:" + id);
        return FetchConversionsAsync("validatedReport", string.Join(',', args), query, true, cancellationToken);
    }

    private async Task<AffiliateConversionPage> FetchConversionsAsync(string operation, string arguments, AffiliateConversionQuery requested,
        bool validated, CancellationToken cancellationToken)
    {
        var graphQl = $"query{{{operation}({arguments}){{nodes{{{ConversionFields}}}pageInfo{{limit hasNextPage scrollId}}}}}}";
        using var document = await ExecuteAsync(graphQl, cancellationToken);
        var connection = document.RootElement.GetProperty("data").GetProperty(operation);
        var items = new List<NormalizedAffiliateConversion>();
        var allowTotalCommissionFallback = _options.AllowTotalCommissionFallback ||
            await _settingProvider.GetAsync<bool>(WebHoanTienSettings.AllowTotalCommissionFallback);
        foreach (var node in connection.GetProperty("nodes").EnumerateArray())
        {
            var parsed = ParseConversion(node, validated, allowTotalCommissionFallback);
            if (!validated || parsed.PurchaseTime >= requested.From && parsed.PurchaseTime <= requested.To) items.Add(parsed);
        }
        var pageInfo = connection.GetProperty("pageInfo");
        var hasNext = pageInfo.TryGetProperty("hasNextPage", out var next) && next.GetBoolean();
        var scroll = hasNext ? ReadString(pageInfo, "scrollId") : null;
        return new AffiliateConversionPage(items, scroll, Sanitize(document.RootElement.GetRawText()));
    }

    private NormalizedAffiliateConversion ParseConversion(JsonElement node, bool validated, bool allowTotalCommissionFallback)
    {
        var total = ReadDecimal(node, "totalCommission") ?? 0m;
        var net = ReadDecimal(node, "netCommission");
        var source = CommissionSource.NetCommission;
        if (!net.HasValue)
        {
            if (!allowTotalCommissionFallback) throw ProviderError("netCommission không có và fallback chưa được cho phép.");
            net = total;
            source = CommissionSource.TotalCommissionFallback;
        }

        var orderNodes = node.GetProperty("orders").EnumerateArray().ToList();
        var orderWeights = orderNodes.Select(x => x.GetProperty("items").EnumerateArray().Sum(i => ReadDecimal(i, "itemTotalCommission") ?? 0m)).ToList();
        var totalWeight = orderWeights.Sum();
        decimal allocated = 0m;
        var orders = new List<NormalizedAffiliateOrder>();
        for (var index = 0; index < orderNodes.Count; index++)
        {
            var order = orderNodes[index];
            var items = order.GetProperty("items").EnumerateArray().Select(ParseItem).ToList();
            var last = index == orderNodes.Count - 1;
            var orderNet = last ? net.Value - allocated : decimal.Round(net.Value * (totalWeight == 0 ? 1m / Math.Max(1, orderNodes.Count) : orderWeights[index] / totalWeight), 4, MidpointRounding.AwayFromZero);
            allocated += orderNet;
            orders.Add(new NormalizedAffiliateOrder(ReadString(order, "orderId") ?? string.Empty,
                MapOrderStatus(ReadString(order, "orderStatus")), ReadString(order, "shopType"),
                items.Sum(x => x.PurchaseAmount), orderNet, items));
        }

        var status = validated ? AffiliateConversionStatus.Approved : DeriveConversionStatus(orders);
        if (orders.Count > 0 && orders.All(x => x.Status == AffiliateOrderStatus.Cancelled)) status = AffiliateConversionStatus.Cancelled;
        return new NormalizedAffiliateConversion(ReadString(node, "conversionId") ?? throw ProviderError("Thiếu conversionId."),
            ReadString(node, "utmContent"), FromUnix(node, "purchaseTime"), TryFromUnix(node, "clickTime"), status,
            total, net.Value, source, orders);
    }

    private static NormalizedAffiliateOrderItem ParseItem(JsonElement item) => new(
        ReadString(item, "itemId") ?? string.Empty, ReadString(item, "modelId"), ReadString(item, "itemName"),
        ReadDecimal(item, "actualAmount") ?? 0m, ReadInt(item, "qty"), ReadDecimal(item, "itemTotalCommission") ?? 0m,
        ReadDecimal(item, "refundAmount") ?? 0m, string.Equals(ReadString(item, "fraudStatus"), "FRAUD", StringComparison.OrdinalIgnoreCase),
        ReadString(item, "displayItemStatus"));

    private async Task<JsonDocument> ExecuteAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.Secret))
            throw new BusinessException(WebHoanTienDomainErrorCodes.ProviderNotConfigured);
        if (_circuitOpenUntil > DateTime.UtcNow) throw ProviderError("Shopee circuit đang tạm mở.");

        var payload = JsonSerializer.Serialize(new { query });
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var signature = ShopeeRequestSigner.Sign(_options.AppId, timestamp, payload, _options.Secret);
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
                { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
                request.Headers.TryAddWithoutValidation("Authorization", ShopeeRequestSigner.CreateAuthorization(_options.AppId, timestamp, signature));
                using var response = await _httpClientFactory.CreateClient("ShopeeAffiliate").SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                    throw new HttpRequestException($"Shopee HTTP {(int)response.StatusCode}");
                response.EnsureSuccessStatusCode();
                var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                {
                    var message = errors[0].TryGetProperty("message", out var errorMessage) ? errorMessage.GetString() : "GraphQL error";
                    document.Dispose();
                    throw ProviderError(message ?? "GraphQL error");
                }
                _consecutiveFailures = 0;
                return document;
            }
            catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException) && attempt < 3)
            {
                last = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1)), cancellationToken);
            }
        }

        if (Interlocked.Increment(ref _consecutiveFailures) >= 5)
        {
            _circuitOpenUntil = DateTime.UtcNow.AddMinutes(1);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }
        _logger.LogError(last, "Shopee request thất bại sau retry.");
        throw ProviderError(last?.Message ?? "Shopee request failed");
    }

    private static AffiliateOrderStatus MapOrderStatus(string? value) => value?.ToUpperInvariant() switch
    { "UNPAID" => AffiliateOrderStatus.Unpaid, "PENDING" => AffiliateOrderStatus.Pending, "COMPLETED" => AffiliateOrderStatus.Completed, "CANCELLED" => AffiliateOrderStatus.Cancelled, _ => AffiliateOrderStatus.Pending };
    private static AffiliateConversionStatus DeriveConversionStatus(IEnumerable<NormalizedAffiliateOrder> orders)
    { var values = orders.Select(x => x.Status).ToList(); return values.Count > 0 && values.All(x => x == AffiliateOrderStatus.Completed) ? AffiliateConversionStatus.Approved : values.Any(x => x == AffiliateOrderStatus.Pending) ? AffiliateConversionStatus.Pending : AffiliateConversionStatus.Estimated; }
    private static string? ReadString(JsonElement element, string name) => !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null ? null : value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText().Trim('"');
    private static decimal? ReadDecimal(JsonElement element, string name) => decimal.TryParse(ReadString(element, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static int ReadInt(JsonElement element, string name) => int.TryParse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    private static DateTime FromUnix(JsonElement element, string name) => DateTimeOffset.FromUnixTimeSeconds(long.Parse(ReadString(element, name)!, CultureInfo.InvariantCulture)).UtcDateTime;
    private static DateTime? TryFromUnix(JsonElement element, string name) => long.TryParse(ReadString(element, name), out var value) && value > 0 ? DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime : null;
    private static string Sanitize(string json) => Regex.Replace(json, "\\\"(email|phone|buyerId|username)\\\"\\s*:\\s*\\\"[^\\\"]*\\\"", "\"$1\":\"***\"", RegexOptions.IgnoreCase);
    private static BusinessException ProviderError(string message) => new(WebHoanTienDomainErrorCodes.ProviderRequestFailed, message);
}
