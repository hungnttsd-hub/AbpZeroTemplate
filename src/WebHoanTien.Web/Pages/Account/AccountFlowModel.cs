using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.IdentityExtensions;
using WebHoanTien.Web.IdentityExtensions;

namespace WebHoanTien.Web.Pages.Account;

[DisableAuditing, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountFlowModel : PageModel
{
    private readonly AnonymousAccountSession _session;
    private readonly IdentityUserManager _users;
    private readonly ICurrentUser _current;
    public AccountFlowModel(AnonymousAccountSession session, IdentityUserManager users, ICurrentUser current)
    { _session = session; _users = users; _current = current; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public bool ShowAnonymous { get; set; }
    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    public string Username { get; private set; } = "";
    public Guid UserId { get; private set; }
    public bool Remembered { get; private set; }
    public bool CreationEnabled => _session.CreationEnabled;
    public bool UpgradePending { get; private set; }
    public string? Error { get; private set; }
    public string SafeReturnUrl => AnonymousAccountSession.SafeReturn(ReturnUrl);
    public string WithReturn(string path) => path + "?returnUrl=" + Uri.EscapeDataString(SafeReturnUrl);

    public async Task<IActionResult> OnGetAsync()
    {
        var path = Request.Path.Value ?? "";
        if (path.EndsWith("/Anonymous", StringComparison.OrdinalIgnoreCase))
            return Redirect(WithReturn("/Account/Choice") + "&showAnonymous=true");
        if (path.EndsWith("UpgradeConfirmation", StringComparison.OrdinalIgnoreCase)) return Page();
        var protectedPage = path.EndsWith("Upgrade", StringComparison.OrdinalIgnoreCase) || path.EndsWith("AnonymousSuccess", StringComparison.OrdinalIgnoreCase);
        if (protectedPage)
        {
            if (!_current.IsAuthenticated) return Redirect(WithReturn("/Account/Choice"));
            var user = await _users.GetByIdAsync(_current.GetId());
            if (!user.IsAnonymous()) return Redirect("/Account/Profile");
            Username = user.UserName; UserId = user.Id;
            UpgradePending = await _session.Accounts.GetPendingAsync(user.Id) != null;
        }
        else
        {
            if (_current.IsAuthenticated) return Redirect(SafeReturnUrl);
            Remembered = await _session.Accounts.FindDeviceAsync(_session.DeviceSecret) != null;
            await _session.EnsureDeviceAsync();
        }
        return Page();
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        try
        {
            await _session.LimitAsync("confirm", 30, 10);
            var result = await _session.TransactionAsync(() => _session.Accounts.ConfirmUpgradeAsync(Token ?? ""));
            await _session.SignInAsync(result.User);
            return LocalRedirect(AnonymousAccountSession.SafeReturn(result.ReturnUrl));
        }
        catch (UserFriendlyException ex) { Error = ex.Message; return Page(); }
        catch (AccountRateLimitException ex) { Error = ex.Message; Response.StatusCode = 429; return Page(); }
    }
}
