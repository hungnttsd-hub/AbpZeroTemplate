using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Notifications;

public class CustomerNotification : AuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public CustomerNotificationCategory Category { get; private set; }
    public CustomerNotificationKind Kind { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? ActionUrl { get; private set; }
    public string EventKey { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    protected CustomerNotification()
    {
    }

    public CustomerNotification(Guid id, Guid userId, CustomerNotificationCategory category,
        CustomerNotificationKind kind, string title, string message, string? actionUrl, string eventKey)
        : base(id)
    {
        UserId = userId;
        Category = category;
        Kind = kind;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), WebHoanTienConsts.NotificationTitleMaxLength).Trim();
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), WebHoanTienConsts.NotificationMessageMaxLength).Trim();
        ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
        EventKey = Check.NotNullOrWhiteSpace(eventKey, nameof(eventKey), WebHoanTienConsts.NotificationEventKeyMaxLength).Trim();
    }

    public bool MarkAsRead(DateTime readAt)
    {
        if (IsRead) return false;
        IsRead = true;
        ReadAt = readAt;
        return true;
    }

    public void UpdateContent(string title, string message, string? actionUrl)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), WebHoanTienConsts.NotificationTitleMaxLength).Trim();
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), WebHoanTienConsts.NotificationMessageMaxLength)
            .Trim();
        ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
    }
}
