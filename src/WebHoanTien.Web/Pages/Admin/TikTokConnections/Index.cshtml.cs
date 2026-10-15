using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using WebHoanTien.Permissions;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Web.Integrations;

namespace WebHoanTien.Web.Pages.Admin.TikTokConnections;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Settings)]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel : PageModel
{
    private readonly ITikTokDeveloperRepository _store;
    private readonly TikTokCreatorOAuthClient _client;
    private readonly ICurrentTenant _tenant;
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public TikTokDeveloperAdminPage Connections { get; private set; } = null!;
    public DateTimeOffset SnapshotAtUtc { get; private set; }
    public long TotalPages => Math.Max(1, (Connections.FilteredCount + 49) / 50);

    public IndexModel(ITikTokDeveloperRepository store, TikTokCreatorOAuthClient client, ICurrentTenant tenant)
    { _store = store; _client = client; _tenant = tenant; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_tenant.Id.HasValue) return Forbid();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Search = Search?.Trim();
        if (Search?.Length > 128) return BadRequest();
        if (Status is not (null or "" or "Active" or "ReauthRequired" or "Disabled" or "NotConnected")) return BadRequest();
        PageNumber = Math.Clamp(PageNumber, 1, 100_000);
        SnapshotAtUtc = DateTimeOffset.UtcNow;
        Connections = await _store.GetAdminPageAsync(_client.AppKey, Search, Status, PageNumber, HttpContext.RequestAborted);
        return Page();
    }

    public static string StatusLabel(string status, DateTimeOffset? refreshExpiresAtUtc) => status switch
    {
        "Active" or "MissingScopes" when refreshExpiresAtUtc is null || refreshExpiresAtUtc <= DateTimeOffset.UtcNow => "Cần cấp quyền lại",
        "Active" => "Đã kết nối",
        "MissingScopes" => "Thiếu quyền",
        "ReauthRequired" => "Cần cấp quyền lại",
        "Disabled" => "Đã ngắt",
        _ => "Chưa kết nối"
    };

    public static string[] Scopes(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch (JsonException) { return Array.Empty<string>(); }
    }
    public static string LocalTime(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss") ?? "—";
}
