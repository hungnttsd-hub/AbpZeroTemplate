using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace WebHoanTien.Integrations.Shopee;

public interface IShopeeAmsPermissionChecker
{
    Task<ShopeeAmsPermissionCheckResult> CheckPermissionAsync(CancellationToken cancellationToken = default);
}

public sealed class ShopeeAmsPermissionCheckResult
{
    public bool IsConfigured { get; init; }
    public bool HasPermission { get; init; }
    public DateTime CheckedAtUtc { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? Error { get; init; }
    public string? Message { get; init; }
    public string? RequestId { get; init; }
    public int ReturnedRecords { get; init; }
}

public sealed class ShopeeAmsPermissionChecker : IShopeeAmsPermissionChecker
{
    private const string ApiPath = "/api/v2/ams/get_conversion_report";
    private readonly HttpClient _httpClient;
    private readonly ShopeeOpenPlatformOptions _options;

    public ShopeeAmsPermissionChecker(HttpClient httpClient, IOptions<ShopeeOpenPlatformOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ShopeeAmsPermissionCheckResult> CheckPermissionAsync(
        CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = DateTime.UtcNow;
        var missingSettings = GetMissingSettings();
        if (missingSettings.Count > 0)
        {
            return Failure(
                checkedAtUtc,
                "configuration_missing",
                "Thiếu cấu hình: " + string.Join(", ", missingSettings));
        }

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(checkedAtUtc, "configuration_invalid", "Shopee OpenPlatform BaseUrl phải là URL HTTPS hợp lệ.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var days = Math.Clamp(_options.PermissionCheckDays, 1, 89);
        var startTime = timestamp - days * 86400L;
        var sign = ShopeeRequestSigner.GetSignShopee(
            _options.PartnerId,
            ApiPath,
            timestamp,
            _options.AccessToken,
            _options.ShopId,
            _options.PartnerKey);

        var requestUri = BuildRequestUri(baseUri, timestamp, startTime, sign);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                return Failure(
                    checkedAtUtc,
                    "invalid_response",
                    "Shopee không trả về JSON hợp lệ.",
                    (int)response.StatusCode);
            }

            using (document)
            {
                var root = document.RootElement;
                var error = ReadString(root, "error");
                var message = ReadString(root, "message");
                var requestId = ReadString(root, "request_id");
                var hasResponseObject = root.TryGetProperty("response", out var responsePayload) &&
                    responsePayload.ValueKind == JsonValueKind.Object;
                var returnedRecords = ReadReturnedRecords(responsePayload, hasResponseObject);
                var hasPermission = response.IsSuccessStatusCode &&
                    string.IsNullOrWhiteSpace(error) &&
                    hasResponseObject;

                return new ShopeeAmsPermissionCheckResult
                {
                    IsConfigured = true,
                    HasPermission = hasPermission,
                    CheckedAtUtc = checkedAtUtc,
                    HttpStatusCode = (int)response.StatusCode,
                    Error = hasPermission ? null : error ?? $"http_{(int)response.StatusCode}",
                    Message = message,
                    RequestId = requestId,
                    ReturnedRecords = returnedRecords
                };
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(checkedAtUtc, "request_timeout", "Hết thời gian chờ Shopee Open Platform.");
        }
        catch (HttpRequestException)
        {
            return Failure(checkedAtUtc, "request_failed", "Không thể kết nối tới Shopee Open Platform.");
        }
    }

    private List<string> GetMissingSettings()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.BaseUrl)) missing.Add("BaseUrl");
        if (string.IsNullOrWhiteSpace(_options.PartnerId)) missing.Add("PartnerId");
        if (string.IsNullOrWhiteSpace(_options.PartnerKey)) missing.Add("PartnerKey");
        if (string.IsNullOrWhiteSpace(_options.ShopId)) missing.Add("ShopId");
        if (string.IsNullOrWhiteSpace(_options.AccessToken)) missing.Add("AccessToken");
        return missing;
    }

    private Uri BuildRequestUri(Uri baseUri, long timestamp, long startTime, string sign)
    {
        var values = new Dictionary<string, string>
        {
            ["partner_id"] = _options.PartnerId,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture),
            ["access_token"] = _options.AccessToken,
            ["shop_id"] = _options.ShopId,
            ["sign"] = sign,
            ["page_no"] = "1",
            ["page_size"] = "1",
            ["place_order_time_start"] = startTime.ToString(CultureInfo.InvariantCulture),
            ["place_order_time_end"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        var queryParts = new List<string>(values.Count);
        foreach (var (key, value) in values)
        {
            queryParts.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value));
        }

        var root = baseUri.AbsoluteUri.TrimEnd('/');
        return new Uri(root + ApiPath + "?" + string.Join("&", queryParts));
    }

    private static int ReadReturnedRecords(JsonElement response, bool hasResponseObject)
    {
        if (!hasResponseObject ||
            !response.TryGetProperty("list", out var list) ||
            list.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return list.GetArrayLength();
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }

    private static ShopeeAmsPermissionCheckResult Failure(
        DateTime checkedAtUtc,
        string error,
        string message,
        int? httpStatusCode = null) => new()
    {
        IsConfigured = error is not "configuration_missing" and not "configuration_invalid",
        HasPermission = false,
        CheckedAtUtc = checkedAtUtc,
        HttpStatusCode = httpStatusCode,
        Error = error,
        Message = message
    };
}
