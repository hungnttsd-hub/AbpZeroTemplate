using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using WebHoanTien.IdentityExtensions;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.IdentityExtensions;

public class RecoveryCodeProtector : IRecoveryCodeProtector, ITransientDependency
{
    private readonly IDataProtector _protector;
    private readonly IConfiguration _configuration;
    public RecoveryCodeProtector(IDataProtectionProvider provider, IConfiguration configuration)
    { _protector = provider.CreateProtector("CatBack.Anonymous.Recovery.v1"); _configuration = configuration; }
    public string Protect(string code)
    {
        var thumbprint = _configuration["DataProtection:CertificateThumbprint"];
        if (string.IsNullOrWhiteSpace(thumbprint)) return _protector.Protect(code);
        // The existing application key ring can contain legacy unencrypted keys in the same DB.
        // An RSA envelope keeps recovery codes confidential even before that ring is rotated.
        using var certificate = FindCertificate(thumbprint);
        using var rsa = certificate.GetRSAPublicKey() ?? throw new InvalidOperationException("Recovery protection requires an RSA certificate.");
        return _protector.Protect("rsa1:" + certificate.Thumbprint + ":" + Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(code), RSAEncryptionPadding.OaepSHA256)));
    }
    public string Unprotect(string ciphertext)
    {
        var plaintext = _protector.Unprotect(ciphertext);
        if (!plaintext.StartsWith("rsa1:", StringComparison.Ordinal)) return plaintext;
        var parts = plaintext.Split(':');
        if (parts.Length != 3) throw new CryptographicException("Invalid recovery envelope.");
        var allowed = (_configuration["DataProtection:CertificateThumbprint"] + ";" +
            _configuration["DataProtection:PreviousCertificateThumbprints"]).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowed.Contains(parts[1], StringComparer.OrdinalIgnoreCase)) throw new CryptographicException("Recovery certificate is not configured.");
        using var certificate = FindCertificate(parts[1]);
        using var rsa = certificate.GetRSAPrivateKey() ?? throw new CryptographicException("Recovery certificate private key is unavailable.");
        return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(parts[2]), RSAEncryptionPadding.OaepSHA256));
    }
    internal static X509Certificate2 FindCertificate(string thumbprint)
    {
        foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
        {
            using var store = new X509Store(StoreName.My, location); store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            foreach (var certificate in certificates) if (certificate.HasPrivateKey) return certificate;
        }
        throw new InvalidOperationException("Recovery protection certificate is unavailable to the application identity.");
    }
}

[DisableAuditing]
public class AnonymousAccountSession : ITransientDependency
{
    public const string CookieName = "__Host-CatBackDevice";
    private readonly IHttpContextAccessor _http;
    private readonly IUnitOfWorkManager _uow;
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly IdentityDynamicClaimsPrincipalContributorCache _claims;
    private readonly IAccountIdentityStore _store;
    private readonly IEmailSender _email;
    private readonly IConfiguration _configuration;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly ILogger<AnonymousAccountSession> _logger;
    public AnonymousAccountManager Accounts { get; }
    public bool CreationEnabled => _configuration.GetValue("Authentication:Anonymous:Enabled", false);
    public bool RequireEmailConfirmation => _identityOptions.Value.SignIn.RequireConfirmedEmail;
    public string DeviceSecret => _http.HttpContext!.Request.Cookies[CookieName] ?? "";
    public AnonymousAccountSession(IHttpContextAccessor http, IUnitOfWorkManager uow, SignInManager<IdentityUser> signIn,
        IdentityDynamicClaimsPrincipalContributorCache claims, IAccountIdentityStore store, IEmailSender email,
        IConfiguration configuration, IOptions<IdentityOptions> identityOptions, AnonymousAccountManager accounts,
        ILogger<AnonymousAccountSession> logger)
    { _http = http; _uow = uow; _signIn = signIn; _claims = claims; _store = store; _email = email;
      _configuration = configuration; _identityOptions = identityOptions; Accounts = accounts; _logger = logger; }

    public static string SafeReturn(string? url) => !string.IsNullOrWhiteSpace(url) && url.Length <= 2048 &&
        url.StartsWith('/') && !url.StartsWith("//") && !url.Contains('\\') && !url.Any(char.IsControl) ? url : "/";
    public void SetDevice(string secret) => _http.HttpContext!.Response.Cookies.Append(CookieName, secret,
        new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", IsEssential = true, Expires = DateTimeOffset.UtcNow.AddYears(WebHoanTienConsts.AnonymousDeviceLifetimeYears) });
    public void ForgetCookie() => _http.HttpContext!.Response.Cookies.Delete(CookieName, new CookieOptions { Secure = true, HttpOnly = true, Path = "/" });
    public async Task EnsureDeviceAsync()
    {
        var secret = DeviceSecret;
        if (secret.Length != 64 || !secret.All(Uri.IsHexDigit) ||
            (await Accounts.IsKnownDeviceAsync(secret) && await Accounts.FindDeviceAsync(secret) == null))
            SetDevice(AnonymousAccountManager.NewSecret());
    }
    public async Task<T> TransactionAsync<T>(Func<Task<T>> operation)
    {
        using var scope = _uow.Begin(requiresNew: true, isTransactional: true);
        var result = await operation();
        await scope.CompleteAsync();
        return result;
    }
    public async Task LimitAsync(string action, int ipLimit = 30, int deviceLimit = 5, int minutes = 15)
    {
        var ip = _http.HttpContext!.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = TimeSpan.FromMinutes(minutes);
        var ipAllowed = await _store.ConsumeLimitAsync(action + ":ip:" + AnonymousAccountManager.Hash(ip), ipLimit, window);
        var partition = _http.HttpContext.User.FindFirst(CatBackAccountProperties.CredentialClaim)?.Value
            ?? AnonymousAccountManager.Hash(DeviceSecret);
        var deviceAllowed = await _store.ConsumeLimitAsync(action + ":device:" + partition, deviceLimit, window);
        if (!ipAllowed || !deviceAllowed) throw new AccountRateLimitException(minutes * 60);
    }
    public async Task SignInAsync(IdentityUser user, string? secret = null)
    {
        await _claims.ClearAsync(user.Id, user.TenantId);
        if (user.IsAnonymous())
        {
            await _signIn.SignInWithClaimsAsync(user, true, new[] {
                new Claim(CatBackAccountProperties.AnonymousClaim, "1"),
                new Claim(CatBackAccountProperties.CredentialClaim, AnonymousAccountManager.Hash(secret!)) });
            SetDevice(secret!);
        }
        else
        {
            ForgetCookie();
            await _signIn.SignInAsync(user, true);
        }
        _logger.LogInformation("CatBack account session established for {UserId}, account type {AccountType}", user.Id, user.GetAccountType());
    }
    public async Task SendUpgradeEmailAsync(PendingAccountUpgrade item, string token)
    {
        // Configured public origin prevents Host-header poisoning of verification links.
        var origin = _configuration["App:SelfUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(origin)) throw new UserFriendlyException("Chưa cấu hình địa chỉ website để gửi email xác minh.");
        var url = origin + "/Account/UpgradeConfirmation?token=" + Uri.EscapeDataString(token);
        await _email.SendAsync(item.Email, "Xác minh nâng cấp tài khoản CatBack",
            "<p>Xác minh email để nâng cấp tài khoản và giữ nguyên số dư, link và đơn hàng.</p><p><a href=\"" +
            HtmlEncoder.Default.Encode(url) + "\">Xác minh email</a></p><p>Liên kết có hiệu lực trong 24 giờ. Không chia sẻ liên kết này.</p>", true);
    }
}
public class AccountRateLimitException : Exception
{
    public int RetryAfter { get; }
    public AccountRateLimitException(int retryAfter) : base("Bạn đã thử quá nhiều lần. Vui lòng đợi và thử lại.") => RetryAfter = retryAfter;
}
