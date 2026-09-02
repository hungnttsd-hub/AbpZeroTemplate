using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Admin;
using WebHoanTien.Notifications;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Notifications;

[Authorize(WebHoanTienPermissions.Admin.Notifications)]
public class IndexModel : PageModel
{
    private const int PageSize = 20;
    private readonly IAdminNotificationAppService _notifications;

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty] public SendPromotionNotificationInput Form { get; set; } = new();
    public PagedResultDto<AdminNotificationCampaignDto> Campaigns { get; private set; } = new();
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Campaigns.TotalCount / (double)PageSize));

    public IndexModel(IAdminNotificationAppService notifications)
    {
        _notifications = notifications;
    }

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        Campaigns = await _notifications.GetListAsync(new GetAdminNotificationCampaignsInput
        {
            SkipCount = (PageNumber - 1) * PageSize,
            MaxResultCount = PageSize
        });

        if (PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
            Campaigns = await _notifications.GetListAsync(new GetAdminNotificationCampaignsInput
            {
                SkipCount = (PageNumber - 1) * PageSize,
                MaxResultCount = PageSize
            });
        }
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        if (!ModelState.IsValid)
        {
            var validationError = ModelState.Values.SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
            return BadRequest(new
            {
                success = false,
                error = validationError ?? "Thông tin gửi thông báo chưa hợp lệ."
            });
        }

        try
        {
            var campaign = await _notifications.SendPromotionAsync(Form);
            return new JsonResult(new
            {
                success = true,
                message = "Đã gửi thông báo ưu đãi.",
                campaign = new
                {
                    campaign.Id,
                    campaign.Title,
                    campaign.Message,
                    campaign.ActionUrl,
                    Audience = (int)campaign.Audience,
                    campaign.TargetEmail,
                    campaign.RecipientCount,
                    campaign.PublishedAt
                }
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            var error = exception.Code switch
            {
                WebHoanTienDomainErrorCodes.NotificationTargetNotFound => "Không tìm thấy tài khoản đang hoạt động với email đã nhập.",
                WebHoanTienDomainErrorCodes.NotificationInvalidActionUrl => "Đường dẫn phải là URL nội bộ bắt đầu bằng dấu /.",
                _ => exception.Message
            };
            return BadRequest(new { success = false, error });
        }
    }
}
