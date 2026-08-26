using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Notifications;

public class NotificationCampaign : CreationAuditedAggregateRoot<Guid>
{
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? ActionUrl { get; private set; }
    public NotificationAudience Audience { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public int RecipientCount { get; private set; }
    public DateTime PublishedAt { get; private set; }

    protected NotificationCampaign()
    {
    }

    public NotificationCampaign(Guid id, string title, string message, string? actionUrl,
        NotificationAudience audience, Guid? targetUserId, int recipientCount, DateTime publishedAt)
        : base(id)
    {
        if (audience == NotificationAudience.SingleUser && !targetUserId.HasValue)
            throw new ArgumentException("Target user is required for a single-user campaign.", nameof(targetUserId));
        if (recipientCount < 1) throw new ArgumentOutOfRangeException(nameof(recipientCount));

        Title = Check.NotNullOrWhiteSpace(title, nameof(title), WebHoanTienConsts.NotificationTitleMaxLength).Trim();
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), WebHoanTienConsts.NotificationMessageMaxLength).Trim();
        ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
        Audience = audience;
        TargetUserId = targetUserId;
        RecipientCount = recipientCount;
        PublishedAt = publishedAt;
    }
}
