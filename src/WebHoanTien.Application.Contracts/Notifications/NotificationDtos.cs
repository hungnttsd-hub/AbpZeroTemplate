using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace WebHoanTien.Notifications;

public sealed class GetCustomerNotificationsInput : PagedAndSortedResultRequestDto
{
    public CustomerNotificationCategory? Category { get; set; }
    public bool UnreadOnly { get; set; }
}

public sealed class CustomerNotificationListItemDto : EntityDto<Guid>
{
    public CustomerNotificationCategory Category { get; set; }
    public CustomerNotificationKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public bool IsRead { get; set; }
}

public sealed class CustomerNotificationPageDto
{
    public long TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<CustomerNotificationListItemDto> Items { get; set; } = new();
}

public sealed class ReadNotificationResultDto
{
    public string? ActionUrl { get; set; }
    public int UnreadCount { get; set; }
}

public interface ICustomerNotificationAppService : IApplicationService
{
    Task<CustomerNotificationPageDto> GetListAsync(GetCustomerNotificationsInput input);
    Task<int> GetUnreadCountAsync();
    Task<ReadNotificationResultDto> MarkAsReadAsync(Guid id);
    Task<int> MarkAllAsReadAsync();
}
