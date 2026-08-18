using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.IdentityExtensions;

public class LegalConsentMiddleware
{
    private readonly RequestDelegate _next;
    public LegalConsentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true || IsExempt(path))
        { await _next(context); return; }

        var idValue = user.FindFirst(Volo.Abp.Security.Claims.AbpClaimTypes.UserId)?.Value;
        if (!Guid.TryParse(idValue, out var userId)) { await _next(context); return; }

        if (user.IsInRole("admin"))
        {
            var users = context.RequestServices.GetRequiredService<IRepository<IdentityUser, Guid>>();
            var administrator = await users.FindAsync(userId);
            if (administrator?.GetProperty<bool>("MustChangePassword") == true)
            {
                if (path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
                    await context.Response.WriteAsJsonAsync(new { error = "initial_password_change_required", changeUrl = "/Account/InitialPassword" });
                    return;
                }
                context.Response.Redirect("/Account/InitialPassword");
                return;
            }
            await _next(context);
            return;
        }

        var repository = context.RequestServices.GetRequiredService<IRepository<UserLegalConsent, Guid>>();
        if (await repository.AnyAsync(x => x.UserId == userId && x.TermsVersion == WebHoanTienConsts.TermsVersion &&
            x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion))
        { await _next(context); return; }

        if (path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
            await context.Response.WriteAsJsonAsync(new { error = "legal_consent_required", consentUrl = "/Legal/Consent" });
            return;
        }
        context.Response.Redirect("/Legal/Consent?returnUrl=" + Uri.EscapeDataString(path + context.Request.QueryString));
    }

    private static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/Legal") || path.StartsWithSegments("/Account/InitialPassword") ||
        path.StartsWithSegments("/Account/Logout") ||
        path.StartsWithSegments("/signin-google") || path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/Abp") || path.StartsWithSegments("/libs") ||
        path.StartsWithSegments("/customer.css") || path.StartsWithSegments("/customer.js");
}
