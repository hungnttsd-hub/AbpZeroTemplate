using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Users;

namespace WebHoanTien.Web.Controllers;

[Route("api/game/session")]
public class WordySessionController : AbpController
{
    [HttpGet, AllowAnonymous]
    public object Get([FromServices] IAntiforgery antiforgery, [FromServices] ICurrentUser user)
    {
        Response.Headers.CacheControl = "no-store";
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return new { userId = user.Id, name = user.UserName, isAdmin = user.IsInRole("admin"), csrfToken = tokens.RequestToken, csrfHeader = tokens.HeaderName };
    }
}
