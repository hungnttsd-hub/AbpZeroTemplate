using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace WebHoanTien.Notifications;

[Authorize]
[RemoteService(IsEnabled = false)]
public class CustomerNotificationAppService : WebHoanTienAppService, ICustomerNotificationAppService
{
    private readonly IRepository<CustomerNotification, Guid> _notifications;

    public CustomerNotificationAppService(IRepository<CustomerNotification, Guid> notifications)
    {
        _notifications = notifications;
    }

    public async Task<CustomerNotificationPageDto> GetListAsync(GetCustomerNotificationsInput input)
    {
        var userId = CurrentUser.GetId();
        var maxResultCount = Math.Clamp(input.MaxResultCount <= 0 ? 20 : input.MaxResultCount, 1, 50);
        var skipCount = Math.Max(0, input.SkipCount);
        var query = (await _notifications.GetQueryableAsync()).Where(x => x.UserId == userId);
        if (input.Category.HasValue) query = query.Where(x => x.Category == input.Category.Value);
        if (input.UnreadOnly) query = query.Where(x => !x.IsRead);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.OrderByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.Id).Skip(skipCount).Take(maxResultCount));
        return new CustomerNotificationPageDto
        {
            TotalCount = totalCount,
            UnreadCount = await GetUnreadCountAsync(),
            Items = items.Select(Map).ToList()
        };
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var userId = CurrentUser.GetId();
        var query = (await _notifications.GetQueryableAsync()).Where(x => x.UserId == userId && !x.IsRead);
        return await AsyncExecuter.CountAsync(query);
    }

    [UnitOfWork]
    public async Task<ReadNotificationResultDto> MarkAsReadAsync(Guid id)
    {
        var notification = await _notifications.GetAsync(id);
        if (notification.UserId != CurrentUser.GetId())
            throw new BusinessException(WebHoanTienDomainErrorCodes.NotificationNotOwned);
        if (notification.MarkAsRead(Clock.Now))
            await _notifications.UpdateAsync(notification, autoSave: true);
        return new ReadNotificationResultDto
        {
            ActionUrl = notification.ActionUrl,
            UnreadCount = await GetUnreadCountAsync()
        };
    }

    [UnitOfWork]
    public async Task<int> MarkAllAsReadAsync()
    {
        var userId = CurrentUser.GetId();
        var unread = await _notifications.GetListAsync(x => x.UserId == userId && !x.IsRead);
        if (unread.Count == 0) return 0;
        var now = Clock.Now;
        foreach (var notification in unread) notification.MarkAsRead(now);
        await _notifications.UpdateManyAsync(unread, autoSave: true);
        return 0;
    }

    private static CustomerNotificationListItemDto Map(CustomerNotification notification) => new()
    {
        Id = notification.Id,
        Category = notification.Category,
        Kind = notification.Kind,
        Title = notification.Title,
        Message = notification.Message,
        CreationTime = notification.CreationTime,
        IsRead = notification.IsRead
    };
}
