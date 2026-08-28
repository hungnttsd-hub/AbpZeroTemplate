using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly IdentityUserManager _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ICurrentUser _currentUser;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có từ 6 đến 128 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public ChangePasswordModel(
        IdentityUserManager userManager,
        SignInManager<IdentityUser> signInManager,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        if (await _userManager.HasPasswordAsync(user))
        {
            return Page();
        }

        TempData["PasswordUnavailableMessage"] =
            "Tài khoản này chưa có mật khẩu đăng nhập trên CatBack. Hãy quản lý phương thức đăng nhập của bạn.";
        return RedirectToPage("/Account/Profile");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        if (!await _userManager.HasPasswordAsync(user))
        {
            TempData["PasswordUnavailableMessage"] =
                "Tài khoản này chưa có mật khẩu đăng nhập trên CatBack. Hãy quản lý phương thức đăng nhập của bạn.";
            return RedirectToPage("/Account/Profile");
        }

        if (string.Equals(CurrentPassword, NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(x => string.Equals(x.Code, "PasswordMismatch", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(CurrentPassword), "Mật khẩu hiện tại không chính xác.");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        // ChangePasswordAsync refreshes the security stamp. Refresh the current cookie
        // so this browser stays signed in while other stale sessions can be invalidated.
        await _signInManager.RefreshSignInAsync(user);

        TempData["PasswordStatusMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToPage("/Account/Profile");
    }
}
