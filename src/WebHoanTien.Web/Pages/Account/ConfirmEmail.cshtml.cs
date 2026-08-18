using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Pages.Account;

public class ConfirmEmailModel : PageModel
{
    private readonly IdentityUserManager _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    [BindProperty(SupportsGet = true)]
    public Guid UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; private set; }

    public ConfirmEmailModel(IdentityUserManager userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == Guid.Empty || string.IsNullOrWhiteSpace(Token))
        {
            Error = "Liên kết xác minh không hợp lệ.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(UserId.ToString());
        if (user is null)
        {
            Error = "Không tìm thấy tài khoản cần xác minh.";
            return Page();
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Token));
        }
        catch (FormatException)
        {
            Error = "Liên kết xác minh không hợp lệ hoặc đã hỏng.";
            return Page();
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            Error = "Liên kết xác minh đã hết hạn hoặc đã được sử dụng.";
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : Redirect("/");
    }
}
