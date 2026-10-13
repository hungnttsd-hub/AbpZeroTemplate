using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

public class TikTokCreatorOAuthClient : ITransientDependency
{
    public const string HttpClientName = "TikTokCreatorOAuth";
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TikTokCreatorOAuthClient> _logger;
    public string AppKey => (_configuration["TikTokShop:Creator:AppKey"] ?? _configuration["TikTokShop:AppKey"])?.Trim() ?? "";
    private string AppSecret => (_configuration["TikTokShop:Creator:AppSecret"] ?? _configuration["TikTokShop:AppSecret"])?.Trim() ?? "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppKey) && !string.IsNullOrWhiteSpace(AppSecret);

    public TikTokCreatorOAuthClient(IHttpClientFactory clients, IConfiguration configuration,
        ILogger<TikTokCreatorOAuthClient> logger)
    { _clients = clients; _configuration = configuration; _logger = logger; }

    public string AuthorizationUrl(string state)
    {
        EnsureConfigured();
        return QueryHelpers.AddQueryString("https://shop.tiktok.com/alliance/creator/auth",
            new Dictionary<string, string?> { ["app_key"] = AppKey, ["state"] = state });
    }

    public Task<TikTokCreatorTokens> Exchange(string code, CancellationToken ct = default) => GetTokens("get", "auth_code", code, "authorized_code", ct);
    public Task<TikTokCreatorTokens> Refresh(string token, CancellationToken ct = default) => GetTokens("refresh", "refresh_token", token, "refresh_token", ct);

    private async Task<TikTokCreatorTokens> GetTokens(string operation, string credentialKey, string credential, string grant, CancellationToken ct)
    {
        EnsureConfigured();
        var url = QueryHelpers.AddQueryString($"https://auth.tiktok-shops.com/api/v2/token/{operation}",
            new Dictionary<string, string?> { ["app_key"] = AppKey, ["app_secret"] = AppSecret,
                [credentialKey] = credential, ["grant_type"] = grant });
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var data = await Send(request, "token_" + operation, ct);
        if (!data.TryGetProperty("user_type", out var type) || type.ValueKind != JsonValueKind.Number || !type.TryGetInt32(out var userType) || userType != 1)
            throw new UserFriendlyException("TikTok trả về mã truy cập không thuộc nhà sáng tạo. Không thể dùng mã truy cập của nhà bán hàng cho kết nối này.");
        var scopes = data.TryGetProperty("granted_scopes", out var granted) && granted.ValueKind == JsonValueKind.Array
            ? granted.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
            : Array.Empty<string>();
        var tokens = new TikTokCreatorTokens(RequiredString(data, "access_token"), RequiredString(data, "refresh_token"),
            RequiredString(data, "open_id"), RequiredTimestamp(data, "access_token_expire_in"),
            RequiredTimestamp(data, "refresh_token_expire_in"), scopes);
        if (tokens.AccessExpiresAt <= DateTimeOffset.UtcNow || tokens.RefreshExpiresAt <= DateTimeOffset.UtcNow)
            throw new UserFriendlyException("TikTok trả về token đã hết hạn. Hãy bắt đầu lại kết nối.");
        return tokens;
    }

    public async Task<TikTokCreatorDto> GetProfile(TikTokCreatorTokens tokens)
    {
        EnsureConfigured();
        if (!tokens.GrantedScopes.Contains("creator.affiliate.info", StringComparer.Ordinal))
            throw new UserFriendlyException("Mã truy cập hiện tại thiếu quyền đọc hồ sơ nhà sáng tạo.");
        const string path = "/affiliate_creator/202508/profiles";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var input = AppSecret + path + "app_key" + AppKey + "timestamp" + timestamp + AppSecret;
        var sign = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret), Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        var url = QueryHelpers.AddQueryString("https://open-api.tiktokglobalshop.com" + path,
            new Dictionary<string, string?> { ["app_key"] = AppKey, ["timestamp"] = timestamp, ["sign"] = sign });
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-tts-access-token", tokens.AccessToken);
        request.Content = new ByteArrayContent(Array.Empty<byte>());
        request.Content.Headers.ContentType = new("application/json");
        var data = await Send(request, "creator_profile");
        var region = RequiredString(data, "selection_region");
        if (!string.Equals(region, "VN", StringComparison.OrdinalIgnoreCase))
            throw new UserFriendlyException("Thị trường đã chọn của nhà sáng tạo không phải Việt Nam, chưa phù hợp với thị trường của CatBack.");
        var username = RequiredString(data, "username");
        return new(RequiredString(data, "creator_user_open_id"), username, username, region, "CONNECTED");
    }


    public async Task<string> GetLink(TikTokCreatorTokens tokens, string productId, string? campaignId = null) =>
        (await GenerateSharingLink(tokens, productId, campaignId)).SharingLink;

    public async Task<TikTokSharingLink> GenerateSharingLink(
        TikTokCreatorTokens tokens,
        string productId,
        string? campaignId = null,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(productId))
            throw new UserFriendlyException("Product ID không được để trống.");

        if (!tokens.GrantedScopes.Contains(
            "creator.affiliate.share_link.read",
            StringComparer.Ordinal))
        {
            throw new UserFriendlyException(
                "Mã truy cập thiếu quyền tạo liên kết tiếp thị.");
        }

        const string path =
            "/affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch";

        // 1. Tạo JSON Body
        var payload = new Dictionary<string, object>
        {
            ["material"] = new
            {
                ids = new[] { productId },
                type = "PRODUCT"
            }
        };

        if (!string.IsNullOrWhiteSpace(campaignId))
            payload["campaign_id"] = campaignId;

        var body = JsonSerializer.Serialize(payload);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        // 2. Timestamp
        var timestamp = DateTimeOffset.UtcNow
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);

        // 3. Tạo chữ ký HMAC SHA256
        var input =
            AppSecret +
            path +
            "app_key" + AppKey +
            "timestamp" + timestamp +
            body +
            AppSecret;

        var sign = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(AppSecret),
                Encoding.UTF8.GetBytes(input)
            )
        ).ToLowerInvariant();

        // 4. Tạo URL
        var url = QueryHelpers.AddQueryString(
            "https://open-api.tiktokglobalshop.com" + path,
            new Dictionary<string, string?>
            {
                ["app_key"] = AppKey,
                ["timestamp"] = timestamp,
                ["sign"] = sign
            });

        // 5. Gửi POST request
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add(
            "x-tts-access-token",
            tokens.AccessToken);

        request.Content = new ByteArrayContent(bodyBytes);
        request.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/json");

        var data = await Send(request, "creator_generate_general_link", ct);

        // 6. Đọc link TikTok trả về
        if (data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("sharing_links", out var links) &&
            links.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in links.EnumerateArray())
            {
                if (item.TryGetProperty("material_id", out var id) &&
                    id.GetString() == productId &&
                    item.TryGetProperty("sharing_link", out var link))
                {
                    var affiliateLink = link.GetString();

                    if (!string.IsNullOrWhiteSpace(affiliateLink))
                        return new TikTokSharingLink(affiliateLink,
                            OptionalString(item, "deep_link"), OptionalString(item, "one_link"), tokens.OpenId);
                }
            }
        }

        // 7. Kiểm tra sản phẩm tạo link thất bại
        if (data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("failed_materials", out var failed) &&
            failed.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in failed.EnumerateArray())
            {
                if (item.TryGetProperty("material_id", out var id) &&
                    id.GetString() == productId &&
                    item.TryGetProperty("fail_reason", out var reason))
                {
                    throw new UserFriendlyException(
                        $"Không thể tạo link: {reason.GetString()}");
                }
            }
        }

        throw new UserFriendlyException(
            "TikTok không trả về liên kết tiếp thị cho sản phẩm.");
    }


    public async Task<IReadOnlyList<TikTokCreatorProduct>> GetProducts(TikTokCreatorTokens tokens,
        IReadOnlyCollection<string> productIds, CancellationToken ct = default)
    {
        EnsureConfigured();
        if (!tokens.GrantedScopes.Contains("creator.affiliate_collaboration.read", StringComparer.Ordinal))
            throw new UserFriendlyException("Creator token thiếu quyền đọc thông tin sản phẩm affiliate.");
        var ids = productIds.Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length is < 1 or > 50 || ids.Any(x => x.Length is < 1 or > 30 || !x.All(char.IsAsciiDigit)))
            throw new UserFriendlyException("Cần từ 1 đến 50 Product ID TikTok hợp lệ.");
        const string path = "/affiliate_creator/202509/open_collaborations/products";
        // The official Reference specifies POST, comma-separated product_ids in query, and {} body.
        const string body = "{}";
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["app_key"] = AppKey, ["product_ids"] = string.Join(",", ids),
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
        var input = AppSecret + path + string.Concat(query.Select(x => x.Key + x.Value)) + body + AppSecret;
        var parameters = query.ToDictionary(x => x.Key, x => (string?)x.Value);
        parameters["sign"] = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret),
            Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            QueryHelpers.AddQueryString("https://open-api.tiktokglobalshop.com" + path, parameters));
        request.Headers.Add("x-tts-access-token", tokens.AccessToken);
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        request.Content.Headers.ContentType = new("application/json");
        var data = await Send(request, "creator_product_info", ct);
        if (!data.TryGetProperty("products", out var products) || products.ValueKind == JsonValueKind.Null)
            return Array.Empty<TikTokCreatorProduct>();
        if (products.ValueKind != JsonValueKind.Array)
            throw new UserFriendlyException("TikTok trả về thông tin sản phẩm không hợp lệ.");
        var result = new List<TikTokCreatorProduct>();
        foreach (var product in products.EnumerateArray())
        {
            var id = OptionalString(product, "id");
            var region = OptionalString(product, "sale_region");
            if (id is null || !ids.Contains(id, StringComparer.Ordinal) || region is not (null or "VN")) continue;
            var price = ProductPrice(product);
            var commission = ProductCommission(product);
            // commission.amount is independently authoritative for the product preview. TikTok can
            // return a nonzero amount with empty sales_price fields. Only treat a zero without any
            // promotional price as unavailable, as specified in the Reference.
            if (price.Minimum is null && commission.Maximum == 0m) commission = (null, null);
            string? shopName = product.TryGetProperty("shop", out var shop) && shop.ValueKind == JsonValueKind.Object
                ? OptionalString(shop, "name") : null;
            result.Add(new(id, tokens.OpenId, OptionalString(product, "title"), shopName,
                OptionalString(product, "main_image_url"), price.Minimum, price.Maximum,
                commission.Minimum, commission.Maximum));
        }
        return result;
    }

    private static (decimal? Minimum, decimal? Maximum) ProductPrice(JsonElement product)
    {
        if (!product.TryGetProperty("sales_price", out var price) || price.ValueKind != JsonValueKind.Object
            || OptionalString(price, "currency") != "VND") return (null, null);
        var minText = OptionalString(price, "minimum_amount");
        var maxText = OptionalString(price, "maximum_amount");
        decimal? min = null, max = null;
        if (minText is not null) { if (!TryProductMoney(minText, out var value)) return (null, null); min = value; }
        if (maxText is not null) { if (!TryProductMoney(maxText, out var value)) return (null, null); max = value; }
        min ??= max; max ??= min;
        return min <= max ? (min, max) : (null, null);
    }

    private static (decimal? Minimum, decimal? Maximum) ProductCommission(JsonElement product)
    {
        if (!product.TryGetProperty("commission", out var commission) || commission.ValueKind != JsonValueKind.Object
            || OptionalString(commission, "currency") != "VND") return (null, null);
        var text = OptionalString(commission, "amount");
        if (text is null) return (null, null);
        var amounts = text.Split(" - ", StringSplitOptions.TrimEntries);
        if (amounts.Length is < 1 or > 2 || !TryProductMoney(amounts[0], out var min)) return (null, null);
        var max = min;
        if (amounts.Length == 2 && !TryProductMoney(amounts[1], out max)) return (null, null);
        return min <= max ? (min, max) : (null, null);
    }

    private static bool TryProductMoney(string text, out decimal amount)
    {
        amount = 0;
        return text.Length is > 0 and <= 24 && text.All(c => char.IsAsciiDigit(c) || c == '.')
            && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
            && amount >= 0 && amount <= 1_000_000_000_000m;
    }

    public async Task<TikTokCreatorOrderPage> SearchOrders(TikTokCreatorTokens tokens,
        long fromUnix, long toUnix, string? pageToken, CancellationToken ct = default)
    {
        EnsureConfigured();
        if (!tokens.GrantedScopes.Contains("creator.affiliate_collaboration.read", StringComparer.Ordinal))
            throw new UserFriendlyException("Creator token thiếu quyền đọc đơn hàng affiliate.");
        const string path = "/affiliate_creator/202410/orders/search";
        var body = JsonSerializer.Serialize(new { create_time_ge = fromUnix, create_time_lt = toUnix });
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["app_key"] = AppKey, ["page_size"] = "100",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrEmpty(pageToken)) query["page_token"] = pageToken;
        var input = AppSecret + path + string.Concat(query.Select(x => x.Key + x.Value)) + body + AppSecret;
        var sign = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret),
            Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        var parameters = query.ToDictionary(x => x.Key, x => (string?)x.Value);
        parameters["sign"] = sign;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            QueryHelpers.AddQueryString("https://open-api.tiktokglobalshop.com" + path, parameters));
        request.Headers.Add("x-tts-access-token", tokens.AccessToken);
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        request.Content.Headers.ContentType = new("application/json");
        var data = await Send(request, "creator_search_orders", ct);
        if (!data.TryGetProperty("orders", out var orders) || orders.ValueKind != JsonValueKind.Array)
        {
            if (data.TryGetProperty("total_count", out var count) && count.ValueKind == JsonValueKind.Number
                && count.TryGetInt64(out var total) && total == 0 && string.IsNullOrEmpty(OptionalString(data, "next_page_token")))
                return new(Array.Empty<JsonElement>(), null, tokens.OpenId);
            throw new UserFriendlyException("TikTok trả về danh sách đơn hàng không hợp lệ; chưa cập nhật đơn hoặc ví.");
        }
        return new(orders.EnumerateArray().Select(x => x.Clone()).ToArray(),
            OptionalString(data, "next_page_token"), tokens.OpenId);
    }

    private async Task<JsonElement> Send(HttpRequestMessage request, string stage, CancellationToken ct = default)
    {
        var httpStatus = 0;
        TimeSpan? retryAfter = null;
        try
        {
            using var response = await _clients.CreateClient(HttpClientName).SendAsync(request, ct);
            httpStatus = (int)response.StatusCode;
            retryAfter = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date is { } retryAt ? retryAt - DateTimeOffset.UtcNow : null);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;
            var requestId = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("request_id", out var rid) && rid.ValueKind == JsonValueKind.String
                ? new string((rid.GetString() ?? "").Where(char.IsAsciiLetterOrDigit).Take(100).ToArray()) : "";
            var code = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("code", out var c)
                && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var number) ? number : -1;
            // Never log request URI, credentials, raw response or platform message.
            _logger.LogInformation("TikTok Creator stage {Stage}: HTTP {Status}, code {Code}, request_id {RequestId}",
                stage, (int)response.StatusCode, code, requestId);
            if (stage is "token_get" or "token_refresh")
                _logger.LogInformation(new EventId(41001, "TikTokTokenResponseDiagnostic"),
                    "TikTok token diagnostic {Stage}: HTTP {Status}, code {Code}, request_id {RequestId}, summary {TokenSummary}",
                    stage, (int)response.StatusCode, code, requestId, TikTokTokenDiagnostics.Describe(root));
            if (!response.IsSuccessStatusCode || code != 0)
                throw new TikTokCreatorApiException(code, (int)response.StatusCode, stage,
                    $"TikTok chưa hoàn tất bước {stage} (HTTP {(int)response.StatusCode}, mã {code}, request_id {requestId}). " +
                    ErrorHint(code), retryAfter);
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                throw new UserFriendlyException("TikTok trả về phản hồi không có dữ liệu hợp lệ.");
            return data.Clone();
        }
        catch (HttpRequestException) { throw new TikTokCreatorApiException(-1, 0, stage, "Không thể kết nối máy chủ TikTok. Hãy thử lại sau."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TikTokCreatorApiException(-1, 0, stage, "Yêu cầu TikTok quá thời gian chờ. Hãy thử lại sau; trạng thái kết nối được giữ nguyên."); }
        catch (JsonException) { throw new TikTokCreatorApiException(-1, httpStatus, stage, "Máy chủ TikTok chưa trả về JSON hợp lệ. Hãy thử lại sau.", retryAfter); }
    }

    private static string ErrorHint(int code) => code switch
    {
        105001 => "Token không hợp lệ hoặc quyền đã bị thu hồi. Admin cần kiểm tra và authorize lại Creator.",
        105002 => "Access token đã hết hạn; cần làm mới token.",
        105005 => "Kiểm tra scope của app và granted_scopes của Creator; callback thành công không chứng minh đã có đủ quyền.",
        101000 => "Sai danh tính token hoặc API-token pairing. Cần kiểm tra cấu hình Creator API.",
        _ => "Hãy thử lại sau hoặc gửi mã lỗi và request_id cho TikTok. Không gửi token hay mã ủy quyền."
    };

    private void EnsureConfigured()
    {
        if (!IsConfigured) throw new UserFriendlyException("Chưa cấu hình TikTokShop.AppKey và TikTokShop.AppSecret trên máy chủ CatBack.");
    }
    private static string RequiredString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()! : throw new UserFriendlyException($"Phản hồi TikTok thiếu trường {name} hợp lệ.");
    private static string? OptionalString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static DateTimeOffset RequiredTimestamp(JsonElement data, string name)
    {
        if (!data.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var timestamp))
            throw new UserFriendlyException($"Phản hồi TikTok thiếu thời hạn {name}.");
        try { return DateTimeOffset.FromUnixTimeSeconds(timestamp); }
        catch (ArgumentOutOfRangeException) { throw new UserFriendlyException("Thời hạn token TikTok không hợp lệ."); }
    }
}

public sealed record TikTokCreatorTokens(string AccessToken, string RefreshToken, string OpenId,
    DateTimeOffset AccessExpiresAt, DateTimeOffset RefreshExpiresAt, string[] GrantedScopes)
{
    public long TokenVersion { get; init; }
    public override string ToString() => "TikTokCreatorTokens [redacted]";
}

public sealed class TikTokCreatorApiException : UserFriendlyException
{
    public int TikTokCode { get; }
    public int HttpStatus { get; }
    public bool RequiresReauthorization { get; }
    public bool IsTransient => HttpStatus == 0 || HttpStatus == 429 || HttpStatus >= 500;
    public int RetryAfterSeconds { get; }
    public TikTokCreatorApiException(int code, int status, string stage, string message, TimeSpan? retryAfter = null) : base(message)
    {
        TikTokCode = code; HttpStatus = status;
        RequiresReauthorization = stage == "token_refresh" && code == 105001 && status < 500 && status != 429;
        RetryAfterSeconds = (int)Math.Clamp(Math.Ceiling(retryAfter?.TotalSeconds ?? 10), 1, 60);
    }
}
