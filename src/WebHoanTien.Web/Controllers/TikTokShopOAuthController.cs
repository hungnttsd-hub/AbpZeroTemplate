using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Controllers;

[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TikTokShopOAuthController : Controller
{
    private const string StateCookie = "CatBack.TikTokCreator.State";
    private readonly TikTokCreatorOAuthClient _client;
    private readonly TikTokCreatorConnection _connection;
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _stateProtector;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    private readonly IAuthorizationService _authorization;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TikTokShopOAuthController> _logger;
    private readonly TikTokCreatorSelection _selection;
    private readonly ITikTokAffiliateService _affiliate;
    private string Owner => $"{_tenant.Id}:{_user.GetId()}:{_client.AppKey}";

    public TikTokShopOAuthController(TikTokCreatorOAuthClient client, TikTokCreatorConnection connection,
        IDistributedCache cache, IDataProtectionProvider protection, ICurrentUser user, ICurrentTenant tenant,
        IAuthorizationService authorization, IConfiguration configuration, ILogger<TikTokShopOAuthController> logger,
        TikTokCreatorSelection selection, ITikTokAffiliateService affiliate)
    {
        _client = client; _connection = connection; _cache = cache; _user = user; _tenant = tenant;
        _authorization = authorization; _configuration = configuration; _logger = logger;
        _selection = selection; _affiliate = affiliate;
        _stateProtector = protection.CreateProtector("CatBack.TikTokCreator.State.v1");
    }

    [Authorize(TikTokAffiliateAccess.Policy)]
    [HttpPost("/api/tiktok-shop/oauth/connect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connect([FromForm] string? creatorUsername)
    {
        SetPrivateHeaders();
        try
        {
            if (!ModelState.IsValid) throw new UserFriendlyException("Vui lòng nhập Username TikTok hợp lệ.");
            creatorUsername = TikTokCreatorSelection.ValidateUsername(creatorUsername);
            // The special local flow never calls TikTok or requires live OAuth configuration.
            if (TikTokCreatorSelection.IsLocal(creatorUsername))
            {
                await _selection.SelectAsync(creatorUsername);
                await _connection.Disconnect();
                await ClearState();
                return LocalRedirect("/tiktok-affiliate/authorize");
            }
            if (!_client.IsConfigured)
                throw new UserFriendlyException("Chưa cấu hình TikTokShop.AppKey và TikTokShop.AppSecret trên máy chủ CatBack.");
            var callback = _configuration["TikTokShop:RedirectUrl"]
                ?? _configuration["App:SelfUrl"]?.TrimEnd('/') + TikTokShopOAuthRoutes.Callback;
            if (!Uri.TryCreate(callback, UriKind.Absolute, out var uri) || uri.Scheme != "https"
                || uri.AbsolutePath != TikTokShopOAuthRoutes.Callback || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new UserFriendlyException("Redirect URL phải là URL HTTPS callback CatBack đã đăng ký với TikTok.");
            // A TLS-terminating proxy can leave the backend request as HTTP. Browser cookies
            // remain Secure and the configured callback stays HTTPS; scheme alone must not block OAuth.
            // A different host/port needs a fresh CatBack session on the callback domain before
            // creating state. Never send a local state/code/token to another deployment.
            if (!Uri.TryCreate("https://" + Request.Host.Value, UriKind.Absolute, out var requestOrigin) ||
                !string.Equals(uri.IdnHost, requestOrigin.IdnHost, StringComparison.OrdinalIgnoreCase) ||
                uri.Port != requestOrigin.Port)
                return Redirect(uri.GetLeftPart(UriPartial.Authority) + "/tiktok-affiliate?creatorUsername=" +
                    Uri.EscapeDataString(creatorUsername));
            await _selection.SelectAsync(creatorUsername);
            await _connection.Disconnect();
            await ClearState();
            var selectedUsername = await _selection.GetPendingAsync();
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            await _cache.SetStringAsync(StateKey(state), _stateProtector.Protect(Owner + ":" + selectedUsername),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            Response.Cookies.Append(StateCookie, state, StateCookieOptions(DateTimeOffset.UtcNow.AddMinutes(15)));
            return Redirect(_client.AuthorizationUrl(state));
        }
        catch (UserFriendlyException ex) { return Failure(ex.Message); }
    }

    [AllowAnonymous]
    [HttpGet(TikTokShopOAuthRoutes.Callback)]
    [HttpHead(TikTokShopOAuthRoutes.Callback)]
    public async Task<IActionResult> Callback()
    {
        SetPrivateHeaders();
        // Keep the endpoint reachable for TikTok URL validation without accepting unsolicited codes.
        if (HttpMethods.IsHead(Request.Method) || !Request.QueryString.HasValue)
            return View("Status", new TikTokShopOAuthStatusModel("TikTok Shop — CatBack",
                "Địa chỉ nhận kết quả cấp quyền nhà sáng tạo TikTok. Hãy bắt đầu từ nút kết nối trong CatBack.", false));
        try
        {
            if (!_user.IsAuthenticated || !(await _authorization.AuthorizeAsync(User, TikTokAffiliateAccess.Policy)).Succeeded)
                throw new UserFriendlyException("Phiên CatBack đã hết hạn hoặc không có quyền TikTok. Đăng nhập lại rồi bắt đầu kết nối từ CatBack.");
            var stateValues = Request.Query["state"];
            var state = stateValues.Count == 1 ? stateValues[0] : null;
            Request.Cookies.TryGetValue(StateCookie, out var cookie);
            var stateMatches = state is { Length: 64 } && cookie is { Length: 64 }
                && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(cookie));
            if (state is null || !stateMatches)
                throw new UserFriendlyException("Không xác minh được phiên cấp quyền (state). Hãy kết nối lại từ CatBack.");
            var selectedUsername = await _selection.GetPendingAsync();
            var cached = await _cache.GetStringAsync(StateKey(state));
            if (string.IsNullOrEmpty(selectedUsername) || TikTokCreatorSelection.IsLocal(selectedUsername) || cached is null ||
                !string.Equals(_stateProtector.Unprotect(cached), Owner + ":" + selectedUsername, StringComparison.Ordinal))
                throw new UserFriendlyException("Phiên cấp quyền đã hết hạn hoặc đã được sử dụng. Hãy kết nối lại.");
            await _cache.RemoveAsync(StateKey(state));
            Response.Cookies.Delete(StateCookie, StateCookieOptions(null));
            if (Request.Query.ContainsKey("error"))
                throw new UserFriendlyException("TikTok chưa cấp quyền nhà sáng tạo hoặc bạn đã từ chối. Hãy kiểm tra điều kiện tài khoản và quyền thử nghiệm của ứng dụng.");
            var codeValues = Request.Query["code"];
            var code = codeValues.Count == 1 ? codeValues[0] : null;
            if (string.IsNullOrWhiteSpace(code) || code == "null" || code.Length > 2048)
                throw new UserFriendlyException("TikTok không trả về mã cấp quyền hợp lệ. Chưa có kết nối nào được xác nhận.");
            var key = Request.Query["app_key"];
            if (key.Count > 1 || (key.Count == 1 && key[0] != _client.AppKey))
                throw new UserFriendlyException("App key trong callback không khớp CatBack.");
            var profile = await _connection.Complete(code);
            if (!string.Equals(TikTokCreatorSelection.Normalize(profile.Username), selectedUsername, StringComparison.Ordinal))
            {
                await _connection.Disconnect();
                throw new UserFriendlyException("Tài khoản TikTok vừa cấp quyền không khớp Username đã nhập. Hãy kết nối lại bằng đúng tài khoản.");
            }
            TempData["TikTokConnectionNotice"] = "Kết nối nhà sáng tạo TikTok thành công.";
            return LocalRedirect("/tiktok-affiliate");
        }
        catch (UserFriendlyException ex) { return Failure(ex.Message); }
        catch (Exception ex)
        {
            // Never log an exception object/message that might contain token request query values.
            var reference = Guid.NewGuid().ToString("N");
            _logger.LogWarning("TikTok OAuth failed: reference {Reference}, exception type {Type}", reference, ex.GetType().Name);
            return Failure("Chưa xác nhận kết nối nhà sáng tạo. Hãy bắt đầu lại. Mã hỗ trợ: " + reference);
        }
    }

    [Authorize(TikTokAffiliateAccess.Policy)]
    [HttpPost("/api/tiktok-shop/oauth/disconnect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disconnect()
    {
        SetPrivateHeaders();
        await _affiliate.DisconnectCreator();
        await ClearState();
        return LocalRedirect("/tiktok-affiliate");
    }

    private async Task ClearState()
    {
        if (Request.Cookies.TryGetValue(StateCookie, out var state) && state.Length == 64)
            await _cache.RemoveAsync(StateKey(state));
        Response.Cookies.Delete(StateCookie, StateCookieOptions(null));
    }

    [AllowAnonymous]
    [HttpGet(TikTokShopOAuthRoutes.Result)]
    [HttpHead(TikTokShopOAuthRoutes.Result)]
    public IActionResult Result()
    {
        SetPrivateHeaders();
        var message = TempData["TikTokCreatorMessage"] as string;
        var connected = TempData["TikTokCreatorConnected"] is true;
        return View("Status", new TikTokShopOAuthStatusModel(connected ? "Đã kết nối nhà sáng tạo TikTok" : "Chưa hoàn tất kết nối nhà sáng tạo TikTok",
            message ?? "Không có kết quả cấp quyền trong phiên này. Hãy bắt đầu kết nối từ CatBack.", true, connected));
    }

    private IActionResult Failure(string message)
    {
        TempData["TikTokCreatorConnected"] = false;
        TempData["TikTokCreatorMessage"] = message;
        return LocalRedirect(TikTokShopOAuthRoutes.Result);
    }
    private static string StateKey(string state) => "CatBack:TikTokCreator:state:" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
    private static CookieOptions StateCookieOptions(DateTimeOffset? expires) => new()
    { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, IsEssential = true, Path = "/", Expires = expires };
    private void SetPrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'self'; img-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
    }
}

public sealed record TikTokShopOAuthStatusModel(string Title, string Description, bool IsCallbackResult, bool IsConnected = false);
