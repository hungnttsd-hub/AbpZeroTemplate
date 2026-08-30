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
    private const string AdminRoleName = "admin";
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
        var query = RestrictToCurrentUser(await _notifications.GetQueryableAsync(), userId);
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
        var query = RestrictToCurrentUser(await _notifications.GetQueryableAsync(), userId)
            .Where(x => !x.IsRead);
        return await AsyncExecuter.CountAsync(query);
    }

    [UnitOfWork]
    public async Task<CustomerNotificationDetailDto> GetAsync(Guid id)
    {
        var notification = await GetOwnedAsync(id);
        if (notification.MarkAsRead(Clock.Now))
            await _notifications.UpdateAsync(notification, autoSave: true);
        return new CustomerNotificationDetailDto
        {
            Id = notification.Id,
            Category = notification.Category,
            Kind = notification.Kind,
            Title = notification.Title,
            Message = notification.Message,
            ActionUrl = notification.ActionUrl,
            CreationTime = notification.CreationTime,
            IsRead = notification.IsRead
        };
    }

    [UnitOfWork]
    public async Task<ReadNotificationResultDto> MarkAsReadAsync(Guid id)
    {
        var notification = await GetOwnedAsync(id);
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
        var query = RestrictToCurrentUser(await _notifications.GetQueryableAsync(), userId)
            .Where(x => !x.IsRead);
        var unread = await AsyncExecuter.ToListAsync(query);
        if (unread.Count == 0) return 0;
        var now = Clock.Now;
        foreach (var notification in unread) notification.MarkAsRead(now);
        await _notifications.UpdateManyAsync(unread, autoSave: true);
        return 0;
    }

    [UnitOfWork]
    public async Task DeleteAsync(Guid id)
    {
        var notification = await GetOwnedAsync(id);
        await _notifications.DeleteAsync(notification, autoSave: true);
    }

    private async Task<CustomerNotification> GetOwnedAsync(Guid id)
    {
        var notification = await _notifications.GetAsync(id);
        if (notification.UserId != CurrentUser.GetId() ||
            (notification.Category == CustomerNotificationCategory.Administration &&
             !CurrentUser.IsInRole(AdminRoleName)))
            throw new BusinessException(WebHoanTienDomainErrorCodes.NotificationNotOwned);
        return notification;
    }

    private IQueryable<CustomerNotification> RestrictToCurrentUser(
        IQueryable<CustomerNotification> query,
        Guid userId)
    {
        query = query.Where(x => x.UserId == userId);
        if (!CurrentUser.IsInRole(AdminRoleName))
        {
            query = query.Where(x => x.Category != CustomerNotificationCategory.Administration);
        }
        return query;
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
