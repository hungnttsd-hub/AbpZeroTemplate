using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Controllers;

// Cookie/Bearer identity is deliberately ignored: every action authenticates its CatBack API key.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(65536)]
[Produces("application/json")]
public class TikTokDeveloperApiController : ControllerBase
{
    private readonly TikTokDeveloperService _developers;
    private readonly TikTokDeveloperProxy _proxy;
    private readonly ILogger<TikTokDeveloperApiController> _logger;
    public TikTokDeveloperApiController(TikTokDeveloperService developers, TikTokDeveloperProxy proxy, ILogger<TikTokDeveloperApiController> logger)
    { _developers = developers; _proxy = proxy; _logger = logger; }
    [HttpGet(TikTokDeveloperAccess.ProfilePath)] public Task<IActionResult> Profile() => Execute(TikTokDeveloperAccess.ProfilePath);
    [HttpPost(TikTokDeveloperAccess.ProductsPath)] public Task<IActionResult> Products() => Execute(TikTokDeveloperAccess.ProductsPath);
    [HttpPost(TikTokDeveloperAccess.LinksPath)] public Task<IActionResult> Links() => Execute(TikTokDeveloperAccess.LinksPath);
    [HttpPost(TikTokDeveloperAccess.OrdersPath)] public Task<IActionResult> Orders() => Execute(TikTokDeveloperAccess.OrdersPath);
    [HttpPost(TikTokDeveloperAccess.TokenPath)] public Task<IActionResult> Token() => Execute(TikTokDeveloperAccess.TokenPath);

    private async Task<IActionResult> Execute(string path)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        try
        {
            var header = Request.Headers[TikTokDeveloperAccess.ApiKeyHeader];
            var owner = await _developers.AuthenticateKeyAsync(header.Count == 1 ? header[0] : null, HttpContext.RequestAborted);
            if (path == TikTokDeveloperAccess.TokenPath)
            {
                if (!Request.IsHttps) return Error(403, "API lấy token chỉ hỗ trợ HTTPS.");
                if (Request.Query.Count != 0 || Request.ContentLength > 0 ||
                    await Request.Body.ReadAsync(new byte[1], HttpContext.RequestAborted) != 0)
                    return Error(400, "API lấy token không nhận query hoặc body. Creator được xác định từ API key.");
                // Only the authenticated key owner's isolated Developer credential can be exported.
                // Never use TikTokCreatorTokenService or the cashback Creator as a fallback.
                var tokens = await _developers.GetTokensAsync(owner, HttpContext.RequestAborted);
                var json = JsonSerializer.Serialize(new
                {
                    code = 0, message = "Success", request_id = HttpContext.TraceIdentifier,
                    data = new
                    {
                        access_token = tokens.AccessToken, refresh_token = tokens.RefreshToken,
                        access_token_expire_in = tokens.AccessExpiresAt.ToUnixTimeSeconds(),
                        refresh_token_expire_in = tokens.RefreshExpiresAt.ToUnixTimeSeconds(),
                        open_id = tokens.OpenId, user_type = 1, granted_scopes = tokens.GrantedScopes
                    }
                });
                // ContentResult avoids serializing/logging a token-bearing DTO through MVC/ABP auditing.
                return new ContentResult { StatusCode = 200, ContentType = "application/json; charset=utf-8", Content = json };
            }
            string? body = null;
            if (path != TikTokDeveloperAccess.ProfilePath)
            {
                if (Request.ContentLength > 65536) return Error(413, "Body vượt quá 64 KiB.");
                using var bytes = new MemoryStream();
                var buffer = new byte[8192];
                int read;
                while ((read = await Request.Body.ReadAsync(buffer, HttpContext.RequestAborted)) > 0)
                {
                    if (bytes.Length + read > 65536) return Error(413, "Body vượt quá 64 KiB.");
                    bytes.Write(buffer, 0, read);
                }
                body = bytes.Length == 0 ? "{}" : Encoding.UTF8.GetString(bytes.ToArray());
            }
            var result = await _proxy.ForwardAsync(owner, path, Request.Query, body, HttpContext.RequestAborted);
            if (result.RetryAfter is not null) Response.Headers.RetryAfter = result.RetryAfter;
            return new ContentResult { StatusCode = result.Status, ContentType = "application/json; charset=utf-8", Content = result.Json };
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == 413) { return Error(413, "Body vượt quá 64 KiB."); }
        catch (TikTokDeveloperException ex)
        {
            if (ex.HttpStatus == 429) Response.Headers.RetryAfter = "60";
            return Error(ex.HttpStatus, ex.Message);
        }
        catch (TikTokCreatorApiException ex)
        {
            if (ex.IsTransient)
            {
                Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return Error(ex.HttpStatus == 429 ? 429 : 503, "TikTok tạm thời chưa làm mới được token. Thử lại sau thời gian Retry-After.");
            }
            return Error(409, "Kết nối Creator riêng cần được kiểm tra hoặc cấp quyền lại trong Developer API.");
        }
        catch (UserFriendlyException ex) { return Error(409, ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { return new EmptyResult(); }
        catch (Exception ex)
        {
            _logger.LogWarning("TikTok Developer request failed: {ErrorType}", ex.GetType().Name);
            return Error(503, "Developer API tạm thời chưa sẵn sàng.");
        }
    }
    private ObjectResult Error(int status, string message) => StatusCode(status,
        new { code = -status, message, request_id = HttpContext.TraceIdentifier, data = (object?)null });
}
