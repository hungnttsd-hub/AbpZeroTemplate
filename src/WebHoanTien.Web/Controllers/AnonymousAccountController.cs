using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.IdentityExtensions;
using WebHoanTien.Web.IdentityExtensions;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Controllers;

[Route("api/account"), DisableAuditing, AutoValidateAntiforgeryToken, UnitOfWork(IsDisabled = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AnonymousAccountController : AbpController
{
    private readonly AnonymousAccountSession _session;
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly IdentityUserManager _users;
    public AnonymousAccountController(AnonymousAccountSession session, SignInManager<IdentityUser> signIn, IdentityUserManager users)
    { _session = session; _signIn = signIn; _users = users; }

    [HttpPost("anonymous"), AllowAnonymous]
    public Task<IActionResult> Create([FromBody] AnonymousCreateInput input) => Execute(async () =>
    {
        if (CurrentUser.IsAuthenticated) throw new UserFriendlyException("Bạn đang đăng nhập. Hãy dùng tài khoản hiện tại.");
        if (!input.AcceptedTerms) throw new UserFriendlyException("Bạn cần chấp thuận Điều khoản và Chính sách riêng tư.");
        var secret = _session.DeviceSecret;
        if (secret.Length != 64 || !secret.All(Uri.IsHexDigit)) throw new UserFriendlyException("Vui lòng tải lại trang để ghi nhớ thiết bị.");
        await _session.LimitAsync("create", 10, 5, 60);
        var result = await _session.TransactionAsync(() => _session.Accounts.CreateOrResumeAsync(secret, _session.CreationEnabled));
        await _session.SignInAsync(result.User, secret);
        return new {
            redirectUrl = result.IsNewAccount
                ? "/Account/AnonymousSuccess?returnUrl=" + Uri.EscapeDataString(AnonymousAccountSession.SafeReturn(input.ReturnUrl))
                : "/"
        };
    });

    [HttpPost("anonymous/recover"), AllowAnonymous]
    public Task<IActionResult> Recover([FromBody] AnonymousRecoverInput input) => Execute(async () =>
    {
        if (CurrentUser.IsAuthenticated) throw new UserFriendlyException("Hãy đăng xuất trước khi khôi phục tài khoản khác.");
        await _session.LimitAsync("recover");
        var secret = AnonymousAccountManager.NewSecret();
        var user = await _session.TransactionAsync(() => _session.Accounts.RecoverAsync(input.RecoveryCode, secret));
        _session.SetDevice(secret);
        await _session.SignInAsync(user, secret);
        return new { redirectUrl = AnonymousAccountSession.SafeReturn(input.ReturnUrl) };
    });

    [HttpGet("anonymous/recovery"), Authorize]
    public Task<IActionResult> Recovery() => Execute(async () => new {
        recoveryCode = await _session.TransactionAsync(() => _session.Accounts.GetRecoveryAsync(CurrentUser.GetId())) });

    [HttpPost("anonymous/recovery/regenerate"), Authorize]
    public Task<IActionResult> Regenerate() => Execute(async () =>
    {
        await _session.LimitAsync("regenerate", 30, 5);
        return new { recoveryCode = await _session.TransactionAsync(() => _session.Accounts.RegenerateAsync(CurrentUser.GetId())) };
    });

    [HttpPut("anonymous/username"), Authorize]
    public Task<IActionResult> Rename([FromBody] AnonymousUsernameInput input) => Execute(async () =>
    {
        await _session.TransactionAsync(async () => { await _session.Accounts.RenameAsync(CurrentUser.GetId(), input.Username); return true; });
        await _session.SignInAsync(await _users.GetByIdAsync(CurrentUser.GetId()), _session.DeviceSecret);
        return new { redirectUrl = "/Account/Profile" };
    });

    [HttpGet("device/current"), Authorize]
    public Task<IActionResult> Device() => Execute(async () =>
    {
        var device = await _session.Accounts.FindDeviceAsync(_session.DeviceSecret);
        var belongs = device?.UserId == CurrentUser.GetId();
        return new { remembered = belongs, expiresAt = belongs ? device?.ExpiresAt : null };
    });

    [HttpDelete("device/current"), Authorize]
    public Task<IActionResult> Forget() => Execute(async () =>
    {
        await _session.TransactionAsync(async () => { await _session.Accounts.ForgetAsync(CurrentUser.GetId(), _session.DeviceSecret); return true; });
        _session.ForgetCookie(); await _signIn.SignOutAsync();
        return new { redirectUrl = "/Account/Choice" };
    });

    [HttpPost("upgrade"), Authorize]
    public Task<IActionResult> Upgrade([FromBody] AccountUpgradeInput input) => Execute(async () =>
    {
        if (!input.AcceptedTerms) throw new UserFriendlyException("Bạn cần chấp thuận Điều khoản và Chính sách riêng tư.");
        await _session.LimitAsync("upgrade", 20, 5, 60);
        var token = AnonymousAccountManager.NewSecret();
        var item = await _session.TransactionAsync(() => _session.Accounts.BeginUpgradeAsync(CurrentUser.GetId(),
            input.Username, input.Email, input.Password, token, AnonymousAccountSession.SafeReturn(input.ReturnUrl)));
        if (!_session.RequireEmailConfirmation)
        {
            var result = await _session.TransactionAsync(() => _session.Accounts.ConfirmUpgradeAsync(token, emailVerified: false));
            await _session.SignInAsync(result.User);
            return new { redirectUrl = result.ReturnUrl };
        }
        await SendEmail(item, token);
        return new { pending = true, message = "Đã gửi email xác minh. Tài khoản vẫn là ẩn danh cho tới khi bạn xác minh email." };
    });

    [HttpPost("upgrade/resend"), Authorize]
    public Task<IActionResult> Resend() => Execute(async () =>
    {
        await _session.LimitAsync("upgrade", 20, 5, 60);
        var token = AnonymousAccountManager.NewSecret();
        var item = await _session.TransactionAsync(() => _session.Accounts.RenewUpgradeAsync(CurrentUser.GetId(), token));
        await SendEmail(item, token);
        return new { pending = true, message = "Đã gửi lại email xác minh. Chỉ liên kết mới nhất còn hiệu lực." };
    });

    private async Task SendEmail(PendingAccountUpgrade item, string token)
    {
        try { await _session.SendUpgradeEmailAsync(item, token); }
        catch { throw new PendingUpgradeEmailException(); }
    }
    private async Task<IActionResult> Execute(Func<Task<object>> action)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Vui lòng kiểm tra các trường đã nhập." });
        try { return Ok(await action()); }
        catch (AccountRateLimitException ex)
        { Response.Headers.RetryAfter = ex.RetryAfter.ToString(); return StatusCode(429, new { message = ex.Message }); }
        catch (PendingUpgradeEmailException) { return BadRequest(new { pending = true, message = "Thông tin nâng cấp đã được lưu nhưng chưa gửi được email. Hãy bấm Gửi lại email." }); }
        catch (UserFriendlyException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
public class PendingUpgradeEmailException : Exception { }

[DisableAuditing]
public class AnonymousCreateInput
{
    public bool AcceptedTerms { get; set; }
    [StringLength(2048)] public string? ReturnUrl { get; set; }
}
[DisableAuditing]
public class AnonymousRecoverInput
{
    [Required, StringLength(128)] public string RecoveryCode { get; set; } = "";
    [StringLength(2048)] public string? ReturnUrl { get; set; }
}
public class AnonymousUsernameInput
{
    [Required, StringLength(256)] public string Username { get; set; } = "";
}
[DisableAuditing]
public class AccountUpgradeInput : AnonymousCreateInput
{
    [Required, StringLength(256)] public string Username { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}
