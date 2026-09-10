using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using WebHoanTien.IdentityExtensions;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.IdentityExtensions;

public class AnonymousAccountSecurityMiddleware
{
    private readonly RequestDelegate _next;
    public AnonymousAccountSecurityMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase))
        { context.Response.Headers.CacheControl = "no-store"; context.Response.Headers["Referrer-Policy"] = "no-referrer"; }
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirst(AbpClaimTypes.UserId)?.Value, out var id))
        {
            var users = context.RequestServices.GetRequiredService<IdentityUserManager>();
            var user = await users.FindByIdAsync(id.ToString());
            var anonymousClaim = context.User.HasClaim(CatBackAccountProperties.AnonymousClaim, "1");
            if (anonymousClaim || user?.IsAnonymous() == true)
            {
                var devices = context.RequestServices.GetRequiredService<IRepository<AnonymousDevice, Guid>>();
                var deviceHash = context.User.FindFirst(CatBackAccountProperties.CredentialClaim)?.Value;
                var now = DateTime.UtcNow;
                var valid = user != null && user.IsAnonymous() && user.IsActive && anonymousClaim && !await users.IsLockedOutAsync(user)
                    && await devices.AnyAsync(x => x.UserId == id && x.SecretHash == deviceHash && x.RevokedAt == null && x.ExpiresAt > now);
                if (!valid)
                {
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
                    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
                    { context.Response.StatusCode = 401; await context.Response.WriteAsJsonAsync(new { message = "Phiên đã hết hiệu lực. Vui lòng đăng nhập lại." }); }
                    else context.Response.Redirect("/Account/Choice");
                    return;
                }
                // Stock Account/Identity endpoints must not add passwords, email, logins or profile changes to anonymous accounts.
                var ownApi = new[] { "/api/account/anonymous", "/api/account/upgrade", "/api/account/device/current", "/api/account/logout" };
                if ((path.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase) && !ownApi.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) || path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase))) ||
                    path.StartsWith("/api/identity", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Account/Manage", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/account/google/connect", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/account/google/callback", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { code = CatBackAccountProperties.RegistrationRequired, message = "Vui lòng nâng cấp tài khoản trước." });
                    return;
                }
                context.Items["CatBackAnonymous"] = true;
            }
        }
        await _next(context);
    }
}
