using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Admin;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Wallets;

[Authorize(WebHoanTienPermissions.Admin.Payouts)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel : PageModel
{
    private const int PageSize = 20;
    private readonly IAdminWalletAppService _wallets;

    [BindProperty(SupportsGet = true), StringLength(256)] public string? Filter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public PagedResultDto<AdminUserWalletDto> Data { get; private set; } = new();
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.TotalCount / (double)PageSize));

    public IndexModel(IAdminWalletAppService wallets) => _wallets = wallets;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ModelState.IsValid) return BadRequest("Bộ lọc hoặc số trang không hợp lệ.");
        PageNumber = Math.Clamp(PageNumber, 1, int.MaxValue / PageSize);
        Data = await LoadAsync();
        if (PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
            Data = await LoadAsync();
        }
        return Page();
    }

    private Task<PagedResultDto<AdminUserWalletDto>> LoadAsync() => _wallets.GetListAsync(new AdminWalletListInput
    {
        Filter = Filter,
        SkipCount = (PageNumber - 1) * PageSize,
        MaxResultCount = PageSize
    });

    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
}
