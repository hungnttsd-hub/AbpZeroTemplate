using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using WebHoanTien.Notifications;

namespace WebHoanTien.Admin;

public sealed class SendPromotionNotificationInput
{
    [Required, StringLength(WebHoanTienConsts.NotificationTitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(WebHoanTienConsts.NotificationMessageMaxLength)]
    public string Message { get; set; } = string.Empty;

    [StringLength(WebHoanTienConsts.NotificationActionUrlMaxLength)]
    public string? ActionUrl { get; set; }

    public NotificationAudience Audience { get; set; }

    [EmailAddress, StringLength(256)]
    public string? TargetEmail { get; set; }
}

public sealed class AdminNotificationCampaignDto : EntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public NotificationAudience Audience { get; set; }
    public string? TargetEmail { get; set; }
    public int RecipientCount { get; set; }
    public DateTime PublishedAt { get; set; }
}

public sealed class GetAdminNotificationCampaignsInput : PagedAndSortedResultRequestDto
{
}

public interface IAdminNotificationAppService : IApplicationService
{
    Task<PagedResultDto<AdminNotificationCampaignDto>> GetListAsync(GetAdminNotificationCampaignsInput input);
    Task<AdminNotificationCampaignDto> SendPromotionAsync(SendPromotionNotificationInput input);
}
