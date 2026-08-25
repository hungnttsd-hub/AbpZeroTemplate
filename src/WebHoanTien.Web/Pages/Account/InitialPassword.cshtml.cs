using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace WebHoanTien.Web.Pages.Account;

[Authorize(Roles = "admin")]
public class InitialPasswordModel : PageModel
{
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentUser _currentUser;

    [BindProperty, Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty, Required, DataType(DataType.Password), MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty, Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? Error { get; private set; }

    public InitialPasswordModel(IdentityUserManager userManager, ICurrentUser currentUser)
    { _userManager = userManager; _currentUser = currentUser; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        return user.GetProperty<bool>("MustChangePassword") ? Page() : Redirect("/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetByIdAsync(_currentUser.GetId());
        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            Error = string.Join(" ", result.Errors.Select(x => x.Description));
            return Page();
        }

        user.SetProperty("MustChangePassword", false);
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            Error = string.Join(" ", updateResult.Errors.Select(x => x.Description));
            return Page();
        }
        return Redirect("/Admin/Affiliates");
    }
}
