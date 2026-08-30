using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Emailing;
using Volo.Abp.SettingManagement;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Settings;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Settings)]
public class IndexModel : PageModel
{
    private readonly ISettingManager _settingManager;

    [BindProperty]
    [Required(ErrorMessage = "Nhập mật khẩu SMTP.")]
    [StringLength(1024, ErrorMessage = "Mật khẩu SMTP không được vượt quá 1024 ký tự.")]
    [DataType(DataType.Password)]
    public string SmtpPassword { get; set; } = string.Empty;

    public bool IsPasswordConfigured { get; private set; }

    public IndexModel(ISettingManager settingManager)
    {
        _settingManager = settingManager;
    }

    public async Task OnGetAsync()
    {
        await LoadStatusAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStatusAsync();
            return Page();
        }

        await _settingManager.SetGlobalAsync(
            EmailSettingNames.Smtp.Password,
            SmtpPassword);

        TempData["EmailSettingsMessage"] = "Đã cập nhật mật khẩu SMTP cho hệ thống.";
        return RedirectToPage();
    }

    private async Task LoadStatusAsync()
    {
        var currentPassword = await _settingManager.GetOrNullGlobalAsync(
            EmailSettingNames.Smtp.Password);
        IsPasswordConfigured = !string.IsNullOrWhiteSpace(currentPassword);
    }
}
