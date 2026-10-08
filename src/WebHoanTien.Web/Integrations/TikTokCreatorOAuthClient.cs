using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public string AppKey => _configuration["TikTokShop:AppKey"]?.Trim() ?? "";
    private string AppSecret => _configuration["TikTokShop:AppSecret"]?.Trim() ?? "";
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

    public Task<TikTokCreatorTokens> Exchange(string code) => GetTokens("get", "auth_code", code, "authorized_code");
    public Task<TikTokCreatorTokens> Refresh(string token) => GetTokens("refresh", "refresh_token", token, "refresh_token");

    private async Task<TikTokCreatorTokens> GetTokens(string operation, string credentialKey, string credential, string grant)
    {
        EnsureConfigured();
        var url = QueryHelpers.AddQueryString($"https://auth.tiktok-shops.com/api/v2/token/{operation}",
            new Dictionary<string, string?> { ["app_key"] = AppKey, ["app_secret"] = AppSecret,
                [credentialKey] = credential, ["grant_type"] = grant });
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var data = await Send(request, "token_" + operation);
        if (!data.TryGetProperty("user_type", out var type) || !type.TryGetInt32(out var userType) || userType != 1)
            throw new UserFriendlyException("TikTok trả về token không phải Creator. Không thể dùng Seller token cho kết nối này.");
        var scopes = data.TryGetProperty("granted_scopes", out var granted) && granted.ValueKind == JsonValueKind.Array
            ? granted.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
            : Array.Empty<string>();
        if (!scopes.Contains("creator.affiliate.info", StringComparer.Ordinal))
            throw new UserFriendlyException("Creator chưa cấp quyền creator.affiliate.info. Hãy cấp lại quyền hồ sơ đang được app yêu cầu.");
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
            throw new UserFriendlyException("Token hiện tại thiếu quyền đọc hồ sơ Creator.");
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
            throw new UserFriendlyException("Creator này không có selection region VN, chưa phù hợp thị trường Vietnam của CatBack.");
        var username = RequiredString(data, "username");
        return new(RequiredString(data, "creator_user_open_id"), username, username, region, "CONNECTED");
    }

    private async Task<JsonElement> Send(HttpRequestMessage request, string stage)
    {
        try
        {
            using var response = await _clients.CreateClient(HttpClientName).SendAsync(request);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var requestId = root.TryGetProperty("request_id", out var rid) && rid.ValueKind == JsonValueKind.String
                ? new string((rid.GetString() ?? "").Where(char.IsAsciiLetterOrDigit).Take(100).ToArray()) : "";
            var code = root.TryGetProperty("code", out var c) && c.TryGetInt32(out var number) ? number : -1;
            // Never log request URI, credentials, raw response or platform message.
            _logger.LogInformation("TikTok Creator stage {Stage}: HTTP {Status}, code {Code}, request_id {RequestId}",
                stage, (int)response.StatusCode, code, requestId);
            if (!response.IsSuccessStatusCode || code != 0)
                throw new UserFriendlyException($"TikTok chưa hoàn tất bước {stage} (HTTP {(int)response.StatusCode}, mã {code}, request_id {requestId}). " +
                    "Nếu đang beta, hãy gửi mã và request_id này cho TikTok để kiểm tra quyền app/Creator. Không gửi token hoặc mã ủy quyền.");
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                throw new UserFriendlyException("TikTok trả về phản hồi không có dữ liệu hợp lệ.");
            return data.Clone();
        }
        catch (HttpRequestException) { throw new UserFriendlyException("Không thể kết nối máy chủ TikTok. Hãy thử lại sau."); }
        catch (TaskCanceledException) { throw new UserFriendlyException("Yêu cầu TikTok quá thời gian chờ. Hãy kết nối lại."); }
        catch (JsonException) { throw new UserFriendlyException("Máy chủ TikTok chưa trả về JSON hợp lệ. Hãy thử lại sau."); }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured) throw new UserFriendlyException("Chưa cấu hình TikTokShop.AppKey và TikTokShop.AppSecret trên máy chủ CatBack.");
    }
    private static string RequiredString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()! : throw new UserFriendlyException($"Phản hồi TikTok thiếu trường {name} hợp lệ.");
    private static DateTimeOffset RequiredTimestamp(JsonElement data, string name)
    {
        if (!data.TryGetProperty(name, out var value) || !value.TryGetInt64(out var timestamp))
            throw new UserFriendlyException($"Phản hồi TikTok thiếu thời hạn {name}.");
        try { return DateTimeOffset.FromUnixTimeSeconds(timestamp); }
        catch (ArgumentOutOfRangeException) { throw new UserFriendlyException("Thời hạn token TikTok không hợp lệ."); }
    }
}

public sealed record TikTokCreatorTokens(string AccessToken, string RefreshToken, string OpenId,
    DateTimeOffset AccessExpiresAt, DateTimeOffset RefreshExpiresAt, string[] GrantedScopes);
