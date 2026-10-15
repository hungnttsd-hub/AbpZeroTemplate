using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

[DisableAuditing]
public class TikTokDeveloperService : ITransientDependency
{
    private readonly ITikTokDeveloperRepository _store;
    private readonly IIdentityUserRepository _users;
    private readonly ICurrentTenant _tenant;
    private readonly TikTokCreatorOAuthClient _client;
    private readonly TikTokCreatorTokenService _configurationGuard;
    private readonly IDataProtectionProvider _protection;
    private readonly IConfiguration _config;
    public TikTokDeveloperService(ITikTokDeveloperRepository store, IIdentityUserRepository users, ICurrentTenant tenant,
        TikTokCreatorOAuthClient client, TikTokCreatorTokenService configurationGuard, IDataProtectionProvider protection, IConfiguration config)
    { _store = store; _users = users; _tenant = tenant; _client = client; _configurationGuard = configurationGuard; _protection = protection; _config = config; }

    public int RequestsPerMinute => Math.Clamp(_config.GetValue("TikTokDeveloper:RequestsPerMinute", 60), 1, 600);
    public int KeyLifetimeDays => Math.Clamp(_config.GetValue("TikTokDeveloper:KeyLifetimeDays", 90), 1, 365);
    public bool IsConfigured => _client.IsConfigured;

    public async Task RequireAllowedAsync(Guid owner)
    {
        if (_tenant.Id.HasValue) throw new TikTokDeveloperException(403, "Developer API chỉ hỗ trợ tài khoản hệ thống CatBack.");
        var user = await _users.FindAsync(owner);
        if (user is not { IsActive: true } || !user.GetProperty<bool>(TikTokDeveloperAccess.UserProperty))
            throw new TikTokDeveloperException(403, "Tài khoản chưa được quản trị cấp quyền TikTok Developer API.");
    }

    public async Task<(string Url, string State)> BeginAuthorizationAsync(Guid owner, CancellationToken ct)
    {
        await RequireAllowedAsync(owner);
        _configurationGuard.ValidateConfiguration(); // configuration/certificates only; never read the primary Creator's tokens
        var state = TikTokDeveloperAccess.StatePrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        await _store.BeginAuthorizationAsync(new(Guid.NewGuid()) { OwnerUserId = owner, AppKey = _client.AppKey,
            StateHash = Hash(state), ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10) }, ct);
        return (_client.AuthorizationUrl(state), state);
    }

    public async Task CompleteAuthorizationAsync(Guid owner, string state, string? code, string? appKey, bool denied, CancellationToken ct)
    {
        await RequireAllowedAsync(owner); _configurationGuard.ValidateConfiguration();
        if (state.Length != TikTokDeveloperAccess.StatePrefix.Length + 64 || !state.StartsWith(TikTokDeveloperAccess.StatePrefix, StringComparison.Ordinal) ||
            state[TikTokDeveloperAccess.StatePrefix.Length..].Any(c => !char.IsAsciiHexDigit(c)))
            throw new UserFriendlyException("State Developer không hợp lệ.");
        if (appKey is not null && appKey != _client.AppKey) throw new UserFriendlyException("App key callback không khớp CatBack.");
        var session = await _store.ConsumeStateAsync(owner, _client.AppKey, Hash(state), ct)
            ?? throw new UserFriendlyException("Phiên cấp quyền đã hết hạn, đã dùng hoặc không thuộc tài khoản của bạn.");
        if (denied) throw new UserFriendlyException("Bạn đã từ chối hoặc TikTok chưa cấp quyền Creator.");
        if (string.IsNullOrWhiteSpace(code) || code == "null" || code.Length > 2048) throw new UserFriendlyException("TikTok chưa trả mã cấp quyền hợp lệ.");
        var tokens = await _client.Exchange(code, ct);
        string? username = null;
        if (tokens.GrantedScopes.Contains("creator.affiliate.info", StringComparer.Ordinal))
            username = (await _client.GetProfile(tokens)).Username;
        var incoming = Encrypt(owner, tokens, username);
        await RequireAllowedAsync(owner);
        await _store.SaveAuthorizationAsync(session, incoming, ct);
    }

    private IDataProtector Protector(Guid owner, string openId) => _protection.CreateProtector(
        "CatBack.TikTokDeveloper.Tokens.v1", _client.AppKey, owner.ToString("N"), openId);
    private TikTokDeveloperAccount Encrypt(Guid owner, TikTokCreatorTokens tokens, string? username) => new(owner)
    {
        AppKey = _client.AppKey, OpenId = tokens.OpenId, Username = username, Status = "Active",
        AccessTokenEncrypted = Protector(owner, tokens.OpenId).Protect(tokens.AccessToken),
        RefreshTokenEncrypted = Protector(owner, tokens.OpenId).Protect(tokens.RefreshToken),
        GrantedScopesJson = JsonSerializer.Serialize(tokens.GrantedScopes),
        AccessExpiresAtUtc = tokens.AccessExpiresAt, RefreshExpiresAtUtc = tokens.RefreshExpiresAt
    };
    private TikTokCreatorTokens Decrypt(TikTokDeveloperAccount account)
    {
        try
        {
            if (account.OpenId is null || account.AccessExpiresAtUtc is null || account.RefreshExpiresAtUtc is null)
                throw new CryptographicException();
            return new(Protector(account.Id, account.OpenId).Unprotect(account.AccessTokenEncrypted),
                Protector(account.Id, account.OpenId).Unprotect(account.RefreshTokenEncrypted), account.OpenId,
                account.AccessExpiresAtUtc.Value, account.RefreshExpiresAtUtc.Value,
                JsonSerializer.Deserialize<string[]>(account.GrantedScopesJson) ?? Array.Empty<string>()) { TokenVersion = account.TokenVersion };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        { throw new TikTokDeveloperException(409, "Không đọc được kết nối Creator riêng. Hãy kết nối lại hoặc kiểm tra DataProtection."); }
    }

    public async Task<TikTokCreatorTokens> GetTokensAsync(Guid owner, CancellationToken ct, bool force = false, long? observedVersion = null)
    {
        await RequireAllowedAsync(owner); _configurationGuard.ValidateConfiguration();
        var watch = Stopwatch.StartNew();
        long? startingVersion = observedVersion;
        while (watch.Elapsed < TimeSpan.FromSeconds(45))
        {
            ct.ThrowIfCancellationRequested();
            var account = await _store.FindAccountAsync(owner, _client.AppKey, ct);
            if (account is null || account.Status != "Active") throw new TikTokDeveloperException(409, "Bạn cần kết nối Creator riêng trong trang Developer API.");
            startingVersion ??= account.TokenVersion;
            if (account.RefreshExpiresAtUtc is null || account.RefreshExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                await _store.MarkReauthAsync(owner, _client.AppKey, account.TokenVersion, ct);
                throw new TikTokDeveloperException(409, "Refresh token đã hết hạn. Hãy cấp quyền Creator lại.");
            }
            if (account.AccessExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5) && (!force || account.TokenVersion != startingVersion)) return Decrypt(account);
            var lease = Guid.NewGuid();
            if (!await _store.AcquireRefreshAsync(owner, _client.AppKey, account.TokenVersion, lease, ct))
            { await Task.Delay(500, ct); continue; }
            var cooldown = 0;
            try
            {
                var refreshed = await _client.Refresh(Decrypt(account).RefreshToken, ct);
                if (refreshed.OpenId != account.OpenId) throw new TikTokDeveloperException(409, "Danh tính Creator thay đổi khi refresh. Hãy kết nối lại.");
                if (await _store.SaveRefreshAsync(Encrypt(owner, refreshed, account.Username), account.TokenVersion, lease, ct))
                    return refreshed with { TokenVersion = account.TokenVersion + 1 };
            }
            catch (TikTokCreatorApiException ex)
            {
                if (ex.RequiresReauthorization) await _store.MarkReauthAsync(owner, _client.AppKey, account.TokenVersion, ct);
                else cooldown = ex.RetryAfterSeconds;
                throw;
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or OperationCanceledException)
            {
                cooldown = 10;
                throw;
            }
            finally
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await _store.ReleaseRefreshAsync(owner, _client.AppKey, account.TokenVersion, lease, cooldown, cleanup.Token); } catch { }
            }
        }
        throw new TikTokDeveloperException(503, "Creator token đang được làm mới. Hãy thử lại sau.");
    }

    public async Task<TikTokDeveloperStatus> GetStatusAsync(Guid owner, CancellationToken ct)
    {
        await RequireAllowedAsync(owner);
        var account = await _store.FindAccountAsync(owner, _client.AppKey, ct);
        if (account is null) return new("NotConnected", null, null, null, Array.Empty<string>());
        var status = account.Status == "Active" && account.RefreshExpiresAtUtc <= DateTimeOffset.UtcNow ? "ReauthRequired" : account.Status;
        return new(status, account.OpenId, account.Username, account.AccessExpiresAtUtc,
            JsonSerializer.Deserialize<string[]>(account.GrantedScopesJson) ?? Array.Empty<string>());
    }
    public async Task DisconnectAsync(Guid owner, CancellationToken ct)
    { await RequireAllowedAsync(owner); await _store.DisconnectAsync(owner, _client.AppKey, ct); }

    public async Task<string> CreateKeyAsync(Guid owner, string? name, CancellationToken ct)
    {
        await RequireAllowedAsync(owner);
        if (!_client.IsConfigured) throw new UserFriendlyException("TikTok Developer API chưa được cấu hình trên máy chủ.");
        name = name?.Trim();
        if (name is not { Length: > 0 and <= 100 } || name.Any(char.IsControl)) throw new UserFriendlyException("Tên API key cần từ 1–100 ký tự.");
        var key = "cbtt_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        await _store.CreateKeyAsync(new(Guid.NewGuid()) { OwnerUserId = owner, AppKey = _client.AppKey, Name = name,
            KeyHash = Hash(key), KeyPreview = key[..9] + "…" + key[^4..], CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(KeyLifetimeDays) }, ct);
        return key;
    }
    public async Task<IReadOnlyList<TikTokDeveloperKeyInfo>> GetKeysAsync(Guid owner, CancellationToken ct)
    {
        await RequireAllowedAsync(owner);
        return (await _store.GetKeysAsync(owner, _client.AppKey, ct)).Select(x => new TikTokDeveloperKeyInfo(
            x.Id, x.Name, x.KeyPreview, x.CreatedAtUtc, x.ExpiresAtUtc, x.RevokedAtUtc)).ToArray();
    }
    public async Task RevokeKeyAsync(Guid owner, Guid keyId, CancellationToken ct)
    { await RequireAllowedAsync(owner); await _store.RevokeKeyAsync(owner, keyId, _client.AppKey, ct); }

    public async Task<Guid> AuthenticateKeyAsync(string? key, CancellationToken ct)
    {
        if (key is null || key.Length != 69 || !key.StartsWith("cbtt_", StringComparison.Ordinal) || key[5..].Any(c => !char.IsAsciiHexDigit(c)))
            throw new TikTokDeveloperException(401, "API key CatBack thiếu hoặc không hợp lệ.");
        var saved = await _store.FindKeyAsync(Hash(key), _client.AppKey, ct);
        if (saved is null) throw new TikTokDeveloperException(401, "API key CatBack không hợp lệ, đã thu hồi hoặc hết hạn.");
        await RequireAllowedAsync(saved.OwnerUserId);
        if (!await _store.ConsumeQuotaAsync(saved.OwnerUserId, _client.AppKey, RequestsPerMinute, ct))
            throw new TikTokDeveloperException(429, "Vượt hạn mức CatBack theo tài khoản. Thử lại sau 60 giây.");
        return saved.OwnerUserId;
    }
    public Task MarkReauthAsync(Guid owner, long version, CancellationToken ct) => _store.MarkReauthAsync(owner, _client.AppKey, version, ct);
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

public sealed record TikTokDeveloperStatus(string Status, string? OpenId, string? Username, DateTimeOffset? AccessExpiresAtUtc, string[] GrantedScopes);
public sealed record TikTokDeveloperKeyInfo(Guid Id, string Name, string Preview, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed class TikTokDeveloperException : UserFriendlyException
{
    public int HttpStatus { get; }
    public TikTokDeveloperException(int status, string message) : base(message) => HttpStatus = status;
}
