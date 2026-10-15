using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.Permissions;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Controllers;

[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TikTokShopOAuthController : Controller
{
    private readonly TikTokCreatorTokenService _tokens;
    private readonly ICurrentUser _user;
    private readonly ILogger<TikTokShopOAuthController> _logger;
    private readonly ICurrentTenant _tenant;
    private readonly TikTokDeveloperService _developers;

    public TikTokShopOAuthController(TikTokCreatorTokenService tokens, ICurrentUser user,
        ILogger<TikTokShopOAuthController> logger, ICurrentTenant tenant, TikTokDeveloperService developers)
    { _tokens = tokens; _user = user; _logger = logger; _tenant = tenant; _developers = developers; }

    [Authorize(Roles = "admin")]
    [Authorize(WebHoanTienPermissions.Admin.Settings)]
    [HttpPost("/api/admin/tiktok-shop/creator/authorize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AuthorizeCreator()
    {
        if (_tenant.Id.HasValue) return Forbid();
        if (!string.Equals(Request.Host.Host, "catback.id.vn", StringComparison.OrdinalIgnoreCase) || Request.Host.Port is not (null or 443))
            return BadRequest(new { error = "Hãy bắt đầu authorize tại https://catback.id.vn/Admin/TikTokCreator trên deployment nhận callback." });
        SetPrivateHeaders();
        var authorizationUrl = await _tokens.StartAuthorizationAsync(_user.GetId(), HttpContext.RequestAborted);
        return Json(new { authorizationUrl });
    }

    [Authorize(Roles = "admin")]
    [Authorize(WebHoanTienPermissions.Admin.Settings)]
    [HttpGet("/api/admin/tiktok-shop/creator/status")]
    public async Task<IActionResult> CreatorStatus()
    {
        if (_tenant.Id.HasValue) return Forbid();
        SetPrivateHeaders();
        return Json(await _tokens.GetStatusAsync(HttpContext.RequestAborted));
    }

    [Authorize(Roles = "admin")]
    [Authorize(WebHoanTienPermissions.Admin.Settings)]
    [HttpPost("/api/admin/tiktok-shop/creator/refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshCreator()
    {
        if (_tenant.Id.HasValue) return Forbid();
        SetPrivateHeaders();
        await _tokens.RefreshAccessTokenAsync(true, HttpContext.RequestAborted);
        return Json(await _tokens.GetStatusAsync(HttpContext.RequestAborted));
    }

    [Authorize(TikTokAffiliateAccess.ConnectionPolicy)]
    [HttpPost("/api/tiktok-shop/oauth/connect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connect()
    {
        if (_tenant.Id.HasValue) return Forbid();
        // Start on the deployment serving the app's registered callback; local reuses the DB credential.
        if (!string.Equals(Request.Host.Host, "catback.id.vn", StringComparison.OrdinalIgnoreCase) || Request.Host.Port is not (null or 443))
            return Redirect("https://catback.id.vn/tiktok-affiliate");
        SetPrivateHeaders();
        try
        {
            if (!ModelState.IsValid) return BadRequest();
            return Redirect(await _tokens.StartAuthorizationAsync(_user.GetId(), HttpContext.RequestAborted));
        }
        catch (UserFriendlyException ex) { return ResultRedirect(ex.Message, false); }
    }

    [AllowAnonymous]
    [HttpGet(TikTokShopOAuthRoutes.Callback)]
    [HttpHead(TikTokShopOAuthRoutes.Callback)]
    public async Task<IActionResult> Callback()
    {
        SetPrivateHeaders();
        if (HttpMethods.IsHead(Request.Method) || !Request.QueryString.HasValue)
            return View("Status", new TikTokShopOAuthStatusModel("TikTok Shop — CatBack",
                "Địa chỉ nhận kết quả authorize Creator. Hãy bắt đầu từ trang TikTok Creator trong quản trị CatBack.", false));
        var developerFlow = Request.Query["state"].Count == 1 && Request.Query["state"][0]?.StartsWith(TikTokDeveloperAccess.StatePrefix, StringComparison.Ordinal) == true;
        try
        {
            var state = SingleQuery("state", required: true)!;
            var code = SingleQuery("code");
            var appKey = SingleQuery("app_key");
            if (developerFlow)
            {
                var cookie = Request.Cookies[TikTokDeveloperAccess.StateCookie];
                if (!_user.IsAuthenticated || cookie is null || cookie.Length != state.Length ||
                    !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(state)))
                    throw new UserFriendlyException("Phiên Developer không khớp trình duyệt hoặc đã hết đăng nhập. Hãy kết nối lại từ Developer API.");
                Response.Cookies.Delete(TikTokDeveloperAccess.StateCookie, new CookieOptions { Path = "/", Secure = true, HttpOnly = true });
                await _developers.CompleteAuthorizationAsync(_user.GetId(), state, code, appKey,
                    Request.Query.ContainsKey("error") || Request.Query.ContainsKey("error_description"), HttpContext.RequestAborted);
                TempData["TikTokDeveloperNotice"] = "Đã kết nối Creator riêng. Mỗi API chỉ hoạt động khi Creator đã cấp đủ scope tương ứng.";
                return LocalRedirect(TikTokDeveloperAccess.Portal);
            }
            var status = await _tokens.CompleteAuthorizationAsync(state, code, appKey,
                Request.Query.ContainsKey("error") || Request.Query.ContainsKey("error_description"), HttpContext.RequestAborted);
            var message = status.Status == "MissingScopes"
                ? "Đã lưu token Creator, nhưng còn thiếu quyền: " + string.Join(", ", status.MissingScopes)
                : "Đã authorize Creator và lưu token an toàn vào database CatBack.";
            return ResultRedirect(message, status.IsConnected);
        }
        catch (UserFriendlyException ex)
        {
            if (developerFlow) { TempData["TikTokDeveloperError"] = ex.Message; return LocalRedirect(TikTokDeveloperAccess.Portal); }
            return ResultRedirect(ex.Message, false);
        }
        catch (Exception ex)
        {
            var reference = Guid.NewGuid().ToString("N");
            // Exception messages/objects may include the token exchange URL. Log only the type.
            _logger.LogWarning("TikTok OAuth failed: reference {Reference}, exception type {Type}", reference, ex.GetType().Name);
            if (developerFlow)
            {
                TempData["TikTokDeveloperError"] = "Chưa hoàn tất kết nối Creator riêng. Mã hỗ trợ: " + reference;
                return LocalRedirect(TikTokDeveloperAccess.Portal);
            }
            return ResultRedirect("Chưa hoàn tất authorize. Hãy thử lại từ trang quản trị. Mã hỗ trợ: " + reference, false);
        }
    }

    [Authorize(TikTokAffiliateAccess.ConnectionPolicy)]
    [HttpPost("/api/tiktok-shop/oauth/disconnect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disconnect()
    {
        if (_tenant.Id.HasValue) return Forbid();
        SetPrivateHeaders();
        await _tokens.DisconnectAsync(HttpContext.RequestAborted);
        TempData["TikTokConnectionNotice"] = "Đã ngắt kết nối TikTok Creator tại CatBack. Hãy kết nối lại để tiếp tục.";
        return LocalRedirect("/tiktok-affiliate");
    }

    [AllowAnonymous]
    [HttpGet(TikTokShopOAuthRoutes.Result)]
    [HttpHead(TikTokShopOAuthRoutes.Result)]
    public IActionResult Result()
    {
        SetPrivateHeaders();
        var message = TempData["TikTokCreatorMessage"] as string;
        var connected = TempData["TikTokCreatorConnected"] is true;
        return View("Status", new TikTokShopOAuthStatusModel(connected ? "Đã kết nối TikTok Creator" : "Kết quả authorize TikTok Creator",
            message ?? "Không có kết quả authorize trong phiên này. Hãy xem trạng thái tại trang quản trị.", true, connected));
    }

    private string? SingleQuery(string name, bool required = false)
    {
        var values = Request.Query[name];
        if (values.Count > 1 || (required && (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))))
            throw new UserFriendlyException("Tham số callback không hợp lệ. Hãy authorize lại từ trang quản trị.");
        return values.Count == 1 ? values[0] : null;
    }

    private IActionResult ResultRedirect(string message, bool connected)
    {
        TempData["TikTokCreatorConnected"] = connected;
        TempData["TikTokCreatorMessage"] = message;
        return LocalRedirect(TikTokShopOAuthRoutes.Result);
    }

    private void SetPrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; img-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
    }
}

public sealed record TikTokShopOAuthStatusModel(string Title, string Description, bool IsCallbackResult, bool IsConnected = false);
