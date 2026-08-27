using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebHoanTien.Web.Operations;

public static class ShopeeAutomationAuthenticationDefaults
{
    public const string AuthenticationScheme = "ShopeeAutomation";
    public const string TokenRateLimitPolicy = "ShopeeAutomationToken";
}

public sealed class ShopeeAutomationOptions
{
    public const string SectionName = "ShopeeAutomation";

    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 30;
    public int MaxReportSizeMb { get; set; } = 10;
}

public enum ShopeeAutomationTokenIssueStatus
{
    Success,
    InvalidCredentials,
    ConfigurationUnavailable
}

public sealed record ShopeeAutomationTokenIssueResult(
    ShopeeAutomationTokenIssueStatus Status,
    string? AccessToken,
    DateTimeOffset? ExpiresAt,
    int ExpiresInSeconds);

public class ShopeeAutomationTokenService
{
    private const string ProtectorPurpose = "CatsBack.ShopeeAutomation.AccessToken.v1";
    private readonly IDataProtector _protector;
    private readonly IOptionsMonitor<ShopeeAutomationOptions> _options;

    public ShopeeAutomationTokenService(IDataProtectionProvider dataProtectionProvider,
        IOptionsMonitor<ShopeeAutomationOptions> options)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _options = options;
    }

    public ShopeeAutomationTokenIssueResult Issue(string clientId, string clientSecret)
    {
        var options = _options.CurrentValue;
        if (!IsConfigured(options))
        {
            return new ShopeeAutomationTokenIssueResult(
                ShopeeAutomationTokenIssueStatus.ConfigurationUnavailable, null, null, 0);
        }

        if (!FixedTimeEquals(clientId, options.ClientId) || !FixedTimeEquals(clientSecret, options.ClientSecret))
        {
            return new ShopeeAutomationTokenIssueResult(
                ShopeeAutomationTokenIssueStatus.InvalidCredentials, null, null, 0);
        }

        var lifetimeMinutes = Math.Clamp(options.TokenLifetimeMinutes, 1, 1440);
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(lifetimeMinutes);
        var payload = new ShopeeAutomationTokenPayload
        {
            ClientId = options.ClientId,
            SecretFingerprint = Fingerprint(options.ClientSecret),
            IssuedAtUnixSeconds = issuedAt.ToUnixTimeSeconds(),
            ExpiresAtUnixSeconds = expiresAt.ToUnixTimeSeconds(),
            TokenId = Guid.NewGuid().ToString("N")
        };
        var accessToken = _protector.Protect(JsonSerializer.Serialize(payload));
        return new ShopeeAutomationTokenIssueResult(
            ShopeeAutomationTokenIssueStatus.Success,
            accessToken,
            expiresAt,
            lifetimeMinutes * 60);
    }

    public bool TryValidate(string accessToken, out string clientId)
    {
        clientId = string.Empty;
        var options = _options.CurrentValue;
        if (!IsConfigured(options) || string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ShopeeAutomationTokenPayload>(_protector.Unprotect(accessToken));
            if (payload is null || payload.ExpiresAtUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
                !FixedTimeEquals(payload.ClientId, options.ClientId) ||
                !FixedTimeEquals(payload.SecretFingerprint, Fingerprint(options.ClientSecret)))
            {
                return false;
            }

            clientId = payload.ClientId;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsConfigured(ShopeeAutomationOptions options) =>
        options.Enabled && !string.IsNullOrWhiteSpace(options.ClientId) &&
        !string.IsNullOrWhiteSpace(options.ClientSecret);

    private static bool FixedTimeEquals(string first, string second)
    {
        var firstHash = SHA256.HashData(Encoding.UTF8.GetBytes(first ?? string.Empty));
        var secondHash = SHA256.HashData(Encoding.UTF8.GetBytes(second ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(firstHash, secondHash);
    }

    private static string Fingerprint(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class ShopeeAutomationTokenPayload
    {
        public string ClientId { get; set; } = string.Empty;
        public string SecretFingerprint { get; set; } = string.Empty;
        public long IssuedAtUnixSeconds { get; set; }
        public long ExpiresAtUnixSeconds { get; set; }
        public string TokenId { get; set; } = string.Empty;
    }
}

public class ShopeeAutomationAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ShopeeAutomationTokenService _tokenService;

    public ShopeeAutomationAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ShopeeAutomationTokenService tokenService)
        : base(options, logger, encoder)
    {
        _tokenService = tokenService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        const string bearerPrefix = "Bearer ";
        if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header không hợp lệ."));
        }

        var accessToken = authorization[bearerPrefix.Length..].Trim();
        if (!_tokenService.TryValidate(accessToken, out var clientId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Access token không hợp lệ hoặc đã hết hạn."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"shopee-automation:{clientId}"),
            new(ClaimTypes.Name, clientId),
            new("catsback_integration", "shopee_report_import")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
