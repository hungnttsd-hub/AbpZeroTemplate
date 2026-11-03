using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.AffiliateIds;

[Authorize(WebHoanTienPermissions.Admin.Settings)]
public class IndexModel : PageModel
{
    private readonly IAdminUserAffiliateIdAppService _userAffiliateIds;
    public PagedResultDto<AdminUserAffiliateIdDto> AffiliateIdOverrides { get; private set; } = new();
    public ListResultDto<AdminAffiliateUserOptionDto> AffiliateUsers { get; private set; } = new();
    [BindProperty(SupportsGet = true)] public string? AffiliateOverrideFilter { get; set; }

    public IndexModel(IAdminUserAffiliateIdAppService userAffiliateIds) => _userAffiliateIds = userAffiliateIds;

    public async Task OnGetAsync()
    {
        AffiliateIdOverrides = await _userAffiliateIds.GetListAsync(new AdminUserAffiliateIdListInput
        {
            Filter = AffiliateOverrideFilter,
            MaxResultCount = 100
        });
        AffiliateUsers = await _userAffiliateIds.GetUserOptionsAsync();
    }

    public async Task<IActionResult> OnPostSetAffiliateOverrideAsync(string userEmail, string affiliateId,
        string? adminNote)
    {
        try
        {
            var result = await _userAffiliateIds.SetAsync(new SetUserAffiliateIdInput
            {
                UserEmail = userEmail,
                Platform = AffiliatePlatform.Shopee,
                AffiliateId = affiliateId,
                AdminNote = adminNote
            });
            return new JsonResult(new { success = true, item = result });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception) when (exception.Code == WebHoanTienDomainErrorCodes.AffiliateUserNotFound)
        {
            return BadRequest(new { success = false, error = "Không tìm thấy tài khoản đang hoạt động với email này." });
        }
    }

    public async Task<IActionResult> OnPostRemoveAffiliateOverrideAsync(Guid userId)
    {
        try
        {
            await _userAffiliateIds.RemoveAsync(userId, AffiliatePlatform.Shopee);
            return new JsonResult(new { success = true, userId });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
    }

}
