using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using WebHoanTien.Notifications;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Notifications)]
[RemoteService(IsEnabled = false)]
public class AdminNotificationAppService : WebHoanTienAppService, IAdminNotificationAppService
{
    private readonly IRepository<NotificationCampaign, Guid> _campaigns;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly CustomerNotificationManager _notificationManager;

    public AdminNotificationAppService(IRepository<NotificationCampaign, Guid> campaigns,
        IRepository<IdentityUser, Guid> users, CustomerNotificationManager notificationManager)
    {
        _campaigns = campaigns;
        _users = users;
        _notificationManager = notificationManager;
    }

    public async Task<PagedResultDto<AdminNotificationCampaignDto>> GetListAsync(
        GetAdminNotificationCampaignsInput input)
    {
        var maxResultCount = Math.Clamp(input.MaxResultCount <= 0 ? 20 : input.MaxResultCount, 1, 50);
        var skipCount = Math.Max(0, input.SkipCount);
        var query = await _campaigns.GetQueryableAsync();
        var totalCount = await AsyncExecuter.CountAsync(query);
        var campaigns = await AsyncExecuter.ToListAsync(query.OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.Id).Skip(skipCount).Take(maxResultCount));
        var targetIds = campaigns.Where(x => x.TargetUserId.HasValue).Select(x => x.TargetUserId!.Value)
            .Distinct().ToList();
        var emails = targetIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _users.GetListAsync(x => targetIds.Contains(x.Id)))
                .ToDictionary(x => x.Id, x => x.Email ?? x.UserName);
        return new PagedResultDto<AdminNotificationCampaignDto>(totalCount,
            campaigns.Select(x => Map(x, x.TargetUserId.HasValue
                ? emails.GetValueOrDefault(x.TargetUserId.Value) ?? "Không xác định"
                : null)).ToList());
    }

    public async Task<AdminNotificationCampaignDto> SendPromotionAsync(SendPromotionNotificationInput input)
    {
        var title = RequireText(input.Title, "Vui lòng nhập tiêu đề thông báo.");
        var message = RequireText(input.Message, "Vui lòng nhập nội dung thông báo.");
        var actionUrl = CustomerNotificationManager.NormalizeActionUrl(input.ActionUrl);
        List<Guid> recipientIds;
        Guid? targetUserId = null;
        string? targetEmail = null;

        if (input.Audience == NotificationAudience.SingleUser)
        {
            targetEmail = input.TargetEmail?.Trim();
            if (string.IsNullOrWhiteSpace(targetEmail))
                throw new UserFriendlyException("Vui lòng nhập email người nhận.");
            var normalizedEmail = targetEmail.ToUpperInvariant();
            var target = (await _users.GetListAsync(x => x.NormalizedEmail == normalizedEmail && x.IsActive))
                .FirstOrDefault() ?? throw new BusinessException(WebHoanTienDomainErrorCodes.NotificationTargetNotFound);
            targetUserId = target.Id;
            targetEmail = target.Email ?? target.UserName;
            recipientIds = new List<Guid> { target.Id };
        }
        else if (input.Audience == NotificationAudience.AllUsers)
        {
            recipientIds = (await _users.GetListAsync(x => x.IsActive)).Select(x => x.Id).Distinct().ToList();
            if (recipientIds.Count == 0)
                throw new BusinessException(WebHoanTienDomainErrorCodes.NotificationTargetNotFound);
        }
        else
        {
            throw new UserFriendlyException("Đối tượng nhận thông báo không hợp lệ.");
        }

        var campaign = new NotificationCampaign(GuidGenerator.Create(), title, message, actionUrl, input.Audience,
            targetUserId, recipientIds.Count, Clock.Now);
        await _campaigns.InsertAsync(campaign);
        await _notificationManager.CreatePromotionForUsersAsync(recipientIds, campaign.Id, title, message, actionUrl);
        return Map(campaign, targetEmail);
    }

    private static string RequireText(string? value, string errorMessage)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new UserFriendlyException(errorMessage);
        return normalized;
    }

    private static AdminNotificationCampaignDto Map(NotificationCampaign campaign, string? targetEmail) => new()
    {
        Id = campaign.Id,
        Title = campaign.Title,
        Message = campaign.Message,
        ActionUrl = campaign.ActionUrl,
        Audience = campaign.Audience,
        TargetEmail = targetEmail,
        RecipientCount = campaign.RecipientCount,
        PublishedAt = campaign.PublishedAt
    };
}
