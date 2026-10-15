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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

public class TikTokDeveloperProxy : ITransientDependency
{
    private readonly IHttpClientFactory _clients;
    private readonly TikTokCreatorOAuthClient _client;
    private readonly TikTokDeveloperService _developers;
    private readonly IConfiguration _config;
    public TikTokDeveloperProxy(IHttpClientFactory clients, TikTokCreatorOAuthClient client, TikTokDeveloperService developers, IConfiguration config)
    { _clients = clients; _client = client; _developers = developers; _config = config; }

    public async Task<TikTokDeveloperResponse> ForwardAsync(Guid owner, string path, IQueryCollection input, string? body, CancellationToken ct)
    {
        var scope = path switch
        {
            TikTokDeveloperAccess.ProfilePath => "creator.affiliate.info",
            TikTokDeveloperAccess.ProductsPath or TikTokDeveloperAccess.OrdersPath => "creator.affiliate_collaboration.read",
            TikTokDeveloperAccess.LinksPath => "creator.affiliate.share_link.read",
            _ => throw new TikTokDeveloperException(404, "API chưa được mở trên CatBack.")
        };
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (input.Count > 32) throw new TikTokDeveloperException(400, "Query quá lớn.");
        foreach (var pair in input)
        {
            if (pair.Key is "app_key" or "sign" or "timestamp") continue; // server-owned signature parameters
            if (pair.Key.Equals("access_token", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("refresh_token", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("api_key", StringComparison.OrdinalIgnoreCase))
                throw new TikTokDeveloperException(400, "Không gửi token hoặc secret TikTok. Chỉ dùng header API key CatBack.");
            if (pair.Key.Length is < 1 or > 64 || !pair.Key.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') ||
                pair.Value.ToString().Length > 8192 || pair.Value.ToString().Any(char.IsControl) ||
                (pair.Value.Count > 1 && pair.Key != "product_ids"))
                throw new TikTokDeveloperException(400, "Query không hợp lệ hoặc lặp tham số.");
            query.Add(pair.Key, pair.Value.ToString());
        }
        if (body is not null)
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                if (json.RootElement.ValueKind != JsonValueKind.Object || ContainsCredentials(json.RootElement))
                    throw new TikTokDeveloperException(400, "Body phải là JSON object nghiệp vụ, không chứa token hoặc secret.");
            }
            catch (JsonException) { throw new TikTokDeveloperException(400, "Body chưa phải JSON hợp lệ."); }
        }
        var tokens = await _developers.GetTokensAsync(owner, ct);
        RequireScope(tokens, scope);
        var response = await SendAsync(path, query, body, tokens, ct);
        // Read endpoints can retry once after an explicit expired-token response. Never replay link creation.
        if (response.Code == 105002 && path != TikTokDeveloperAccess.LinksPath)
        {
            tokens = await _developers.GetTokensAsync(owner, ct, true, tokens.TokenVersion);
            RequireScope(tokens, scope);
            response = await SendAsync(path, query, body, tokens, ct);
        }
        if (response.Code == 105001) await _developers.MarkReauthAsync(owner, tokens.TokenVersion, ct);
        return response;
    }

    private static void RequireScope(TikTokCreatorTokens tokens, string scope)
    {
        if (!tokens.GrantedScopes.Contains(scope, StringComparer.Ordinal))
            throw new TikTokDeveloperException(403, "Creator của bạn chưa cấp quyền " + scope + ".");
    }
    private static bool ContainsCredentials(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in element.EnumerateObject())
            {
                if (!names.Add(field.Name) || field.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase) ||
                    field.Name.Equals("refresh_token", StringComparison.OrdinalIgnoreCase) ||
                    field.Name.Equals("app_secret", StringComparison.OrdinalIgnoreCase) || ContainsCredentials(field.Value)) return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) if (ContainsCredentials(child)) return true;
        return false;
    }

    private async Task<TikTokDeveloperResponse> SendAsync(string path, SortedDictionary<string, string> input, string? body,
        TikTokCreatorTokens tokens, CancellationToken ct)
    {
        var secret = (_config["TikTokShop:Creator:AppSecret"] ?? _config["TikTokShop:AppSecret"])?.Trim()
            ?? throw new TikTokDeveloperException(503, "Máy chủ chưa cấu hình TikTok.");
        var query = new SortedDictionary<string, string>(input, StringComparer.Ordinal)
        {
            ["app_key"] = _client.AppKey,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
        var signing = secret + path + string.Concat(query.Select(x => x.Key + x.Value)) + (body ?? "") + secret;
        query["sign"] = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signing))).ToLowerInvariant();
        var url = QueryHelpers.AddQueryString("https://open-api.tiktokglobalshop.com" + path, query.ToDictionary(x => x.Key, x => (string?)x.Value));
        using var request = new HttpRequestMessage(path == TikTokDeveloperAccess.ProfilePath ? HttpMethod.Get : HttpMethod.Post, url);
        request.Headers.Add("x-tts-access-token", tokens.AccessToken);
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body ?? ""));
        request.Content.Headers.ContentType = new("application/json");
        try
        {
            using var response = await _clients.CreateClient(TikTokCreatorOAuthClient.HttpClientName).SendAsync(request, ct);
            var jsonText = await response.Content.ReadAsStringAsync(ct);
            using var json = JsonDocument.Parse(jsonText);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("code", out var code) ||
                code.ValueKind != JsonValueKind.Number || !code.TryGetInt32(out var number) || ContainsCredentials(root) || jsonText.Contains(tokens.AccessToken, StringComparison.Ordinal) ||
                jsonText.Contains(tokens.RefreshToken, StringComparison.Ordinal) || jsonText.Contains(secret, StringComparison.Ordinal))
                throw new TikTokDeveloperException(502, "TikTok trả về phản hồi không hợp lệ.");
            // Preserve the entire business JSON envelope, including opaque IDs, amounts, errors and pagination.
            var retryAfter = response.Headers.RetryAfter?.ToString();
            return new((int)response.StatusCode, jsonText, number, retryAfter);
        }
        catch (HttpRequestException) { throw new TikTokDeveloperException(502, "Không kết nối được TikTok. Không tự gửi lại thao tác tạo link."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TikTokDeveloperException(504, "TikTok chưa phản hồi trong thời gian chờ. Kiểm tra trước khi tạo link lại."); }
        catch (JsonException) { throw new TikTokDeveloperException(502, "TikTok chưa trả JSON hợp lệ."); }
    }
}

public sealed record TikTokDeveloperResponse(int Status, string Json, int Code, string? RetryAfter);
