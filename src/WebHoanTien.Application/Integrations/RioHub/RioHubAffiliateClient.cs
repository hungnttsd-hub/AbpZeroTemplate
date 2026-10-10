using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations.RioHub;

// Singleton: remembers the reachable host for subsequent requests in this process.
public sealed class RioHubAffiliateClient : IRioHubAffiliateClient
{
    public const string HttpClientName = "RioHubAffiliate";
    private const string PathPrefix = "/partner/tiktok/affiliate/";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly IHttpClientFactory _factory;
    private readonly RioHubOptions _options;
    private readonly ILogger<RioHubAffiliateClient> _logger;
    private readonly string[] _baseUrls;
    private int _preferredHost;

    public RioHubAffiliateClient(IHttpClientFactory factory, IOptions<RioHubOptions> options,
        ILogger<RioHubAffiliateClient> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
        _baseUrls = new[] { _options.BaseUrl }.Concat(_options.FallbackBaseUrls)
            .Select(x => x.TrimEnd('/')).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<string> CreateAffiliateLinkAsync(string productUrlOrId, string subId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productUrlOrId);
        var product = productUrlOrId.Trim();
        var isId = System.Text.RegularExpressions.Regex.IsMatch(product, @"\A[0-9]+\z");
        var response = await CreateLinkAsync(new RioHubCreateLinkInput
        {
            CreatorUsername = _options.CreatorUsername,
            ProductId = isId ? product : null,
            ProductUrl = isId ? null : product,
            SubId = subId
        }, cancellationToken: cancellationToken);
        if (!response.TryGetProperty("affiliate_link", out var value) || value.ValueKind != JsonValueKind.String ||
            !TikTokAffiliateUrl.TryNormalize(value.GetString(), out var link, out _) ||
            !response.TryGetProperty("sub_id", out var returnedSub) || returnedSub.ValueKind != JsonValueKind.String ||
            returnedSub.GetString() != subId ||
            !response.TryGetProperty("creator_username", out var creator) || creator.ValueKind != JsonValueKind.String ||
            !string.Equals(creator.GetString(), _options.CreatorUsername, StringComparison.OrdinalIgnoreCase))
            throw new RioHubApiException("RioHub trả link hoặc mã theo dõi không hợp lệ.");
        return value.GetString()!;
    }

    public Task<JsonElement> CreateLinkAsync(RioHubCreateLinkInput input, bool includeProduct = false,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, includeProduct ? "product-links" : "links", input, cancellationToken);

    public Task<JsonElement> CreateGeneralLinksAsync(RioHubGeneralLinksInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "general-links", input, cancellationToken);

    public Task<JsonElement> GetLinksAsync(RioHubLinksQuery input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, "links", input, cancellationToken);

    public Task<JsonElement> GetOrdersAsync(RioHubOrdersQuery input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, "orders", input, cancellationToken);

    public Task<JsonElement> GetProductsAsync(RioHubProductsQuery input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, "products", input, cancellationToken);

    private async Task<JsonElement> SendAsync(HttpMethod method, string endpoint, object input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new RioHubApiException("RioHub chưa bật hoặc chưa có API key trong cấu hình bí mật/biến môi trường.");

        // Encode each query value independently; preserve empty positional sub filters.
        var json = JsonSerializer.Serialize(input, input.GetType(), JsonOptions);
        var query = method == HttpMethod.Get
            ? "?" + string.Join("&", JsonSerializer.SerializeToElement(input, input.GetType(), JsonOptions)
                .EnumerateObject().Select(p => Uri.EscapeDataString(p.Name) + "=" +
                    Uri.EscapeDataString(p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.ToString())))
            : string.Empty;
        using var client = _factory.CreateClient(HttpClientName);
        var start = Volatile.Read(ref _preferredHost);
        var rateRetries = 0;
        for (var attempt = 0; attempt < _baseUrls.Length; attempt++)
        {
            var hostIndex = (start + attempt) % _baseUrls.Length;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(method, _baseUrls[hostIndex] + PathPrefix + endpoint + query);
                request.Headers.Add("X-Riohub-Api-Key", _options.ApiKey);
                if (method == HttpMethod.Post) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response;
                try
                {
                    // Receive headers separately: body/JSON failures must not trigger host failover.
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is null &&
                    ex.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError)
                {
                    _logger.LogWarning("RioHub connection failed on host {Host}; category {Category}.",
                        new Uri(_baseUrls[hostIndex]).Host, ex.HttpRequestError);
                    break;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("RioHub connection timed out on host {Host}.", new Uri(_baseUrls[hostIndex]).Host);
                    break;
                }

                using (response)
                {
                    // Even an HTTP error proves connectivity. Never switch host because of a status code.
                    Volatile.Write(ref _preferredHost, hostIndex);
                    var retryAfter = GetRetryAfter(response, rateRetries);
                    if (response.StatusCode == HttpStatusCode.TooManyRequests && rateRetries < _options.MaxRateLimitRetries &&
                        retryAfter <= TimeSpan.FromSeconds(_options.MaxRetryAfterSeconds))
                    {
                        rateRetries++;
                        response.Dispose();
                        await Task.Delay(retryAfter, cancellationToken);
                        continue;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        // The spec gives a business code but does not fix an error envelope.
                        // Detect only this known literal; never relay/log arbitrary provider text.
                        var productNotPromotable = response.StatusCode == HttpStatusCode.UnprocessableEntity &&
                            await IsProductNotPromotableAsync(response, cancellationToken);
                        var code = productNotPromotable ? "product_not_promotable" : null;
                        _logger.LogWarning("RioHub {Endpoint} returned HTTP {Status}; code {Code}.", endpoint, (int)response.StatusCode, code);
                        var message = productNotPromotable
                            ? "Sản phẩm không có hoa hồng, chưa được shop duyệt hoặc bị TikTok hạn chế (product_not_promotable). Hãy chọn sản phẩm khác."
                            : ErrorMessage(response.StatusCode);
                        throw new RioHubApiException(message, response.StatusCode,
                            response.StatusCode == HttpStatusCode.TooManyRequests ? retryAfter : null, code);
                    }
                    try
                    {
                        using var bodyTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        bodyTimeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                        // Bound the buffered response. No provider response body is included in logs/errors.
                        await using var stream = await response.Content.ReadAsStreamAsync(bodyTimeout.Token);
                        using var buffer = new MemoryStream();
                        var chunk = new byte[16 * 1024];
                        int count;
                        while ((count = await stream.ReadAsync(chunk.AsMemory(), bodyTimeout.Token)) > 0)
                        {
                            if (buffer.Length + count > 4 * 1024 * 1024)
                                throw new IOException("RioHub response exceeds the size limit.");
                            buffer.Write(chunk, 0, count);
                        }
                        using var document = JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
                        if (document.RootElement.ValueKind != JsonValueKind.Object)
                            throw new JsonException();
                        return document.RootElement.Clone();
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new RioHubApiException("RioHub đã trả HTTP nhưng đọc nội dung bị timeout; không tự gửi lại request.");
                    }
                    catch (Exception ex) when (ex is JsonException or HttpRequestException or System.IO.IOException)
                    {
                        throw new RioHubApiException("Phản hồi RioHub không đọc được hoặc không phải JSON object hợp lệ; không tự gửi lại request.");
                    }
                }
            }
        }
        throw new RioHubApiException("Không kết nối được các tên miền RioHub. Với request tạo link bị timeout, kiểm tra danh sách link theo sub_id trước khi gửi lại.");
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response, int retry)
    {
        var header = response.Headers.RetryAfter;
        var delay = header?.Delta ?? (header?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(Math.Pow(2, retry + 1));
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    private async Task<bool> IsProductNotPromotableAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[2048];
            int count;
            while ((count = await stream.ReadAsync(chunk.AsMemory(), timeout.Token)) > 0)
            {
                if (buffer.Length + count > 32 * 1024) return false;
                buffer.Write(chunk, 0, count);
            }
            using var document = JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
            return ContainsBusinessCode(document.RootElement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
        catch (Exception ex) when (ex is JsonException or IOException or HttpRequestException) { return false; }
    }

    private static bool ContainsBusinessCode(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() == "product_not_promotable",
        JsonValueKind.Object => value.EnumerateObject().Any(p => ContainsBusinessCode(p.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(ContainsBusinessCode),
        _ => false
    };

    private static string ErrorMessage(HttpStatusCode status) => (int)status switch
    {
        401 => "RioHub: API key sai hoặc thiếu.",
        403 => "RioHub: Creator không thuộc tài khoản của API key.",
        404 => "RioHub: không tìm thấy tài nguyên hoặc Creator chưa kết nối.",
        422 => "RioHub: dữ liệu không hợp lệ hoặc sản phẩm không đủ điều kiện affiliate.",
        429 => "RioHub: vượt giới hạn request; chờ theo Retry-After.",
        502 => "RioHub: lỗi xử lý phía nhà cung cấp hoặc không tạo được deep link.",
        _ => "RioHub trả lỗi HTTP " + ((int)status).ToString(CultureInfo.InvariantCulture) + "."
    };
}
