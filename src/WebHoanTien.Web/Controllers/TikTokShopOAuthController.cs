using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Controllers;

[AllowAnonymous]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TikTokShopOAuthController : Controller
{
    [HttpGet(TikTokShopOAuthRoutes.Callback)]
    [HttpHead(TikTokShopOAuthRoutes.Callback)]
    public IActionResult Callback()
    {
        SetPrivateHeaders();

        // Registration preparation only: no authorization flow has been initiated, so no
        // callback can be trusted. Do not bind, store, echo or exchange code/state values.
        if (Request.QueryString.HasValue)
            return LocalRedirect(TikTokShopOAuthRoutes.Result);

        return View("Status", new TikTokShopOAuthStatusModel(
            "TikTok Shop — CatBack",
            "Đây là địa chỉ chuyển hướng dành cho kết nối TikTok Shop với CatBack. Tính năng kết nối đang được chuẩn bị; truy cập trang này không cấp quyền hoặc liên kết tài khoản.",
            IsCallbackResult: false));
    }

    [HttpGet(TikTokShopOAuthRoutes.Result)]
    [HttpHead(TikTokShopOAuthRoutes.Result)]
    public IActionResult Result()
    {
        SetPrivateHeaders();
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("Status", new TikTokShopOAuthStatusModel(
            "Chưa hoàn tất kết nối TikTok Shop",
            "CatBack chưa bật luồng ủy quyền TikTok Shop. Tài khoản chưa được liên kết và mã ủy quyền chưa được sử dụng để lấy token. Vui lòng quay lại CatBack hoặc liên hệ hỗ trợ.",
            IsCallbackResult: true));
    }

    private void SetPrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'self'; img-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
    }
}

public sealed record TikTokShopOAuthStatusModel(string Title, string Description, bool IsCallbackResult);
