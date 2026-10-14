using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.IdentityExtensions;

namespace WebHoanTien.Web.Integrations;

[DisableAuditing]
public class TikTokCreatorTokenService : ITikTokCreatorTokenService, ITransientDependency
{
    public const string RedirectUrl = "https://catback.id.vn/api/tiktok-shop/oauth/callback";
    private readonly ITikTokCreatorCredentialRepository _store;
    private readonly TikTokCreatorOAuthClient _client;
    private readonly IDataProtectionProvider _protection;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly RecoveryCertificates _certificates;
    private readonly ICurrentTenant _tenant;

    public TikTokCreatorTokenService(ITikTokCreatorCredentialRepository store, TikTokCreatorOAuthClient client,
        IDataProtectionProvider protection, IConfiguration configuration, IHostEnvironment environment, RecoveryCertificates certificates, ICurrentTenant tenant)
    { _store = store; _client = client; _protection = protection; _configuration = configuration; _environment = environment; _certificates = certificates; _tenant = tenant; }

    private int RefreshBeforeMinutes => _configuration.GetValue("TikTokShop:Creator:TokenRefreshBeforeExpiryMinutes", 5);
    private string[] RequiredScopes => _configuration.GetSection("TikTokShop:Creator:RequiredScopes").Get<string[]>() ?? Array.Empty<string>();
    private static readonly string[] KnownScopes = { "creator.affiliate.info", "creator.affiliate_collaboration.read", "creator.affiliate.link.write", "creator.affiliate.share_link.read" };

    public void ValidateConfiguration()
    {
        if (!_client.IsConfigured) throw new UserFriendlyException("Chưa cấu hình AppKey và AppSecret của TikTok Shop trên máy chủ.");
        var redirect = _configuration["TikTokShop:Creator:RedirectUrl"] ?? _configuration["TikTokShop:RedirectUrl"] ?? RedirectUrl;
        if (redirect != RedirectUrl) throw new UserFriendlyException("RedirectUrl phải đúng " + RedirectUrl);
        if (RefreshBeforeMinutes is < 1 or > 30) throw new UserFriendlyException("TokenRefreshBeforeExpiryMinutes phải nằm trong khoảng 1–30.");
        if (RequiredScopes.Any(x => !KnownScopes.Contains(x, StringComparer.Ordinal)))
            throw new UserFriendlyException("RequiredScopes chứa scope chưa được đối chiếu cho app CatBack.");
        if (!_environment.IsDevelopment() && _certificates.Current is null)
            throw new UserFriendlyException("Cần cấu hình chứng chỉ DataProtection dùng chung giữa các instance để bảo vệ khóa mã hóa token trong database.");
    }

    public async Task<string> StartAuthorizationAsync(Guid actorId, CancellationToken ct)
    {
        // This is a host-level CatBack credential, not a tenant administrator's connection.
        if (_tenant.Id.HasValue) throw new UserFriendlyException("Chỉ tài khoản hệ thống CatBack được authorize Creator chung.");
        ValidateConfiguration();
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        await _store.CreateStateAsync(new TikTokCreatorAuthState(Guid.NewGuid())
        {
            StateHash = HashState(state), AppKey = _client.AppKey, InitiatedByAdminId = actorId,
            CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(10)
        }, ct);
        return _client.AuthorizationUrl(state);
    }

    public async Task<TikTokCreatorStatusDto> CompleteAuthorizationAsync(string state, string? code, string? appKey, bool denied, CancellationToken ct)
    {
        ValidateConfiguration();
        if (state.Length != 64 || state.Any(x => !char.IsAsciiHexDigit(x)))
            throw new UserFriendlyException("State không hợp lệ. Hãy bắt đầu authorize lại từ trang quản trị.");
        if (appKey is not null && appKey != _client.AppKey)
            throw new UserFriendlyException("App key trong callback không khớp CatBack.");
        var session = await _store.ConsumeStateAsync(HashState(state), _client.AppKey, ct)
            ?? throw new UserFriendlyException("Phiên authorize đã hết hạn, đã dùng hoặc không hợp lệ. Hãy bắt đầu lại từ trang quản trị.");
        try
        {
            if (denied) throw new UserFriendlyException("TikTok chưa cấp quyền hoặc bạn đã từ chối authorize.");
            if (string.IsNullOrWhiteSpace(code) || code == "null" || code.Length > 2048)
                throw new UserFriendlyException("TikTok không trả mã cấp quyền hợp lệ.");
            var tokens = await _client.Exchange(code, ct);
            var now = DateTimeOffset.UtcNow;
            var credentials = new TikTokCreatorCredentials(Guid.NewGuid())
            {
                AppKey = session.AppKey, OpenId = tokens.OpenId, AuthorizedByAdminId = session.InitiatedByAdminId,
                CreatedAtUtc = now, UpdatedAtUtc = now
            };
            ApplyTokens(credentials, tokens);
            await _store.SaveAuthorizationAsync(session, credentials, ct);
            return Status(credentials);
        }
        catch
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await _store.FailStateAsync(session.Id, cleanup.Token); } catch { /* keep the original safe error */ }
            throw;
        }
    }

    public async Task<TikTokCreatorStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var credentials = await _store.GetPrimaryAsync(_client.AppKey, ct);
        return credentials is null ? new(false, "NotConnected", null, null, null, null, null,
            Array.Empty<string>(), Array.Empty<string>(), null) : Status(credentials);
    }

    private TikTokCreatorStatusDto Status(TikTokCreatorCredentials value)
    {
        var scopes = JsonSerializer.Deserialize<string[]>(value.GrantedScopesJson) ?? Array.Empty<string>();
        var missing = RequiredScopes.Except(scopes, StringComparer.Ordinal).ToArray();
        var status = value.Status;
        if (status is "Active" or "MissingScopes")
            status = value.RefreshTokenExpiresAtUtc <= DateTimeOffset.UtcNow ? "ReauthRequired" : missing.Length > 0 ? "MissingScopes" : "Active";
        return new(status == "Active", status, value.OpenId, value.UserType, value.AccessTokenExpiresAtUtc,
            value.RefreshTokenExpiresAtUtc, value.LastRefreshedAtUtc, scopes, missing, value.TokenVersion);
    }

    public async Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken = default) =>
        (await GetTokensAsync(false, null, cancellationToken)).AccessToken;

    public async Task<string> RefreshAccessTokenAsync(bool force = false, CancellationToken cancellationToken = default) =>
        (await GetTokensAsync(force, null, cancellationToken)).AccessToken;

    public async Task<TikTokCreatorTokens> GetTokensAsync(bool force = false, long? observedVersion = null, CancellationToken ct = default)
    {
        ValidateConfiguration();
        var watch = Stopwatch.StartNew();
        long? startingVersion = observedVersion;
        while (watch.Elapsed < TimeSpan.FromSeconds(75))
        {
            ct.ThrowIfCancellationRequested();
            var value = await _store.GetPrimaryAsync(_client.AppKey, ct)
                ?? throw new UserFriendlyException("Admin chưa authorize TikTok Creator. Vui lòng kết nối trong trang quản trị.");
            if (value.Status is not ("Active" or "MissingScopes") || value.UserType != 1)
                throw new UserFriendlyException("Kết nối Creator cần được admin authorize lại (" + value.Status + ").");
            startingVersion ??= value.TokenVersion;
            var now = DateTimeOffset.UtcNow;
            if (value.AccessTokenExpiresAtUtc > now.AddMinutes(RefreshBeforeMinutes)
                && (!force || value.TokenVersion != startingVersion)) return Decrypt(value);
            if (value.RefreshTokenExpiresAtUtc <= now)
            {
                await _store.MarkReauthAsync(value.Id, value.TokenVersion, null, ct);
                throw new UserFriendlyException("Refresh token đã hết hạn. Admin cần authorize Creator lại.");
            }
            var lease = Guid.NewGuid();
            if (!await _store.AcquireRefreshAsync(value.Id, value.TokenVersion, lease, ct))
            { await Task.Delay(Random.Shared.Next(400, 800), ct); continue; }
            try
            {
                var tokens = await _client.Refresh(Decrypt(value).RefreshToken, ct);
                if (!string.Equals(tokens.OpenId, value.OpenId, StringComparison.Ordinal))
                    throw new UserFriendlyException("OpenId thay đổi khi refresh. Token mới không được lưu.");
                ApplyTokens(value, tokens);
                if (!await _store.SaveRefreshAsync(value, value.TokenVersion, lease, ct)) continue;
                return tokens with { TokenVersion = value.TokenVersion + 1 };
            }
            catch (TikTokCreatorApiException ex) when (ex.RequiresReauthorization)
            {
                await _store.MarkReauthAsync(value.Id, value.TokenVersion, lease, ct);
                throw;
            }
            catch (TikTokCreatorApiException ex) when (ex.IsTransient)
            {
                // Share a bounded cooldown across instances instead of letting every waiter
                // immediately repeat a rate-limited or uncertain refresh-token rotation.
                using var backoff = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _store.DeferRefreshAsync(value.Id, value.TokenVersion, lease, ex.RetryAfterSeconds, backoff.Token);
                throw;
            }
            finally
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await _store.ReleaseRefreshAsync(value.Id, value.TokenVersion, lease, cleanup.Token); } catch { /* lease expires after crash/outage */ }
            }
        }
        throw new UserFriendlyException("Creator token đang được làm mới. Vui lòng thử lại sau.");
    }

    public Task<TikTokCreatorCredentials?> ReadPrimaryAsync(CancellationToken ct = default) => _store.GetPrimaryAsync(_client.AppKey, ct);

    // Disconnect CatBack's credential; this does not revoke the user's grant at TikTok.
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_tenant.Id.HasValue) throw new UserFriendlyException("Chỉ tài khoản hệ thống CatBack được quản lý Creator chung.");
        return _store.DisablePrimaryAsync(_client.AppKey, ct);
    }

    public async Task ReportInvalidTokenAsync(long observedVersion, CancellationToken ct = default)
    {
        var value = await _store.GetPrimaryAsync(_client.AppKey, ct);
        if (value is not null && value.TokenVersion == observedVersion)
            await _store.MarkReauthAsync(value.Id, observedVersion, null, ct);
    }

    private IDataProtector Protector(TikTokCreatorCredentials value) => _protection.CreateProtector("CatBack.TikTokCreator.DatabaseTokens.v1", value.AppKey, value.OpenId);
    private void ApplyTokens(TikTokCreatorCredentials value, TikTokCreatorTokens tokens)
    {
        value.AccessTokenEncrypted = Protector(value).Protect(tokens.AccessToken);
        value.RefreshTokenEncrypted = Protector(value).Protect(tokens.RefreshToken);
        value.AccessTokenExpiresAtUtc = tokens.AccessExpiresAt;
        value.RefreshTokenExpiresAtUtc = tokens.RefreshExpiresAt;
        value.GrantedScopesJson = JsonSerializer.Serialize(tokens.GrantedScopes);
        value.UserType = 1;
        value.Status = RequiredScopes.Except(tokens.GrantedScopes, StringComparer.Ordinal).Any() ? "MissingScopes" : "Active";
    }

    private TikTokCreatorTokens Decrypt(TikTokCreatorCredentials value)
    {
        try
        {
            return new(Protector(value).Unprotect(value.AccessTokenEncrypted), Protector(value).Unprotect(value.RefreshTokenEncrypted),
                value.OpenId, value.AccessTokenExpiresAtUtc, value.RefreshTokenExpiresAtUtc,
                JsonSerializer.Deserialize<string[]>(value.GrantedScopesJson) ?? Array.Empty<string>()) { TokenVersion = value.TokenVersion };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        { throw new UserFriendlyException("Không đọc được token Creator. Kiểm tra khóa/chứng chỉ DataProtection dùng chung; không xóa dữ liệu token."); }
    }

    private static string HashState(string state) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
}

public sealed record TikTokCreatorStatusDto(bool IsConnected, string Status, string? OpenId, int? UserType,
    DateTimeOffset? AccessTokenExpiresAtUtc, DateTimeOffset? RefreshTokenExpiresAtUtc, DateTimeOffset? LastRefreshedAtUtc,
    string[] GrantedScopes, string[] MissingScopes, long? TokenVersion);
