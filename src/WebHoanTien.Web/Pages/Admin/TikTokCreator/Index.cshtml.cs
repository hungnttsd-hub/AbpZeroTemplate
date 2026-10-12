using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.Permissions;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Pages.Admin.TikTokCreator;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Settings)]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel : PageModel
{
    private readonly TikTokCreatorTokenService _tokens;
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    public TikTokCreatorStatusDto Connection { get; private set; } = null!;
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public bool IsConfigured { get; private set; }
    public string StatusLabel => Connection.Status switch
    {
        "Active" => "Đã kết nối", "MissingScopes" => "Thiếu quyền", "ReauthRequired" => "Cần authorize lại",
        "Disabled" => "Đã tắt", _ => "Chưa kết nối"
    };

    public IndexModel(TikTokCreatorTokenService tokens, ICurrentUser user, ICurrentTenant tenant) { _tokens = tokens; _user = user; _tenant = tenant; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAuthorizeAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        // The OAuth state must be created on the deployment serving the registered callback.
        if (!string.Equals(Request.Host.Host, "catback.id.vn", StringComparison.OrdinalIgnoreCase) || Request.Host.Port is not (null or 443))
            return Redirect("https://catback.id.vn/Admin/TikTokCreator");
        try { return Redirect(await _tokens.StartAuthorizationAsync(_user.GetId(), HttpContext.RequestAborted)); }
        catch (UserFriendlyException ex) { Error = ex.Message; await LoadAsync(); return Page(); }
    }

    public async Task<IActionResult> OnPostRefreshAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        try { await _tokens.RefreshAccessTokenAsync(true, HttpContext.RequestAborted); Notice = "Đã làm mới token Creator."; }
        catch (UserFriendlyException ex) { Error = ex.Message; }
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        try { _tokens.ValidateConfiguration(); IsConfigured = true; }
        catch (UserFriendlyException ex) { Error ??= ex.Message; }
        Connection = await _tokens.GetStatusAsync(HttpContext.RequestAborted);
    }
}
