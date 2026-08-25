using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using WebHoanTien.Permissions;

namespace WebHoanTien.Affiliates;

[Authorize]
public class AffiliateOrderAppService : WebHoanTienAppService, IAffiliateOrderAppService
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<IdentityUser, Guid> _users;
    public AffiliateOrderAppService(IRepository<AffiliateConversion, Guid> conversions, IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items, IRepository<AffiliateTracking, Guid> trackings,
        IRepository<IdentityUser, Guid> users)
    {
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _trackings = trackings;
        _users = users;
    }

    public async Task<PagedResultDto<AffiliateOrderDto>> GetListAsync(AffiliateOrderListInput input)
    {
        var canManageAllOrders = await AuthorizationService.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders);
        var conversionQuery = await _conversions.GetQueryableAsync();
        if (!canManageAllOrders)
        {
            var userId = CurrentUser.GetId();
            conversionQuery = conversionQuery.Where(x => x.UserId == userId);
        }
        if (input.From.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime >= input.From.Value);
        if (input.To.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime < input.To.Value);

        var query = from order in await _orders.GetQueryableAsync()
                    join conversion in conversionQuery on order.ConversionId equals conversion.Id
                    select new { Order = order, Conversion = conversion };
        if (input.Status.HasValue) query = query.Where(x => x.Order.Status == input.Status.Value);
        query = query.OrderByDescending(x => x.Conversion.PurchaseTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        var trackingIds = rows.Where(x => x.Conversion.TrackingId.HasValue)
            .Select(x => x.Conversion.TrackingId!.Value).Distinct().ToList();
        var trackingById = trackingIds.Count == 0
            ? new Dictionary<Guid, AffiliateTracking>()
            : (await _trackings.GetListAsync(x => trackingIds.Contains(x.Id))).ToDictionary(x => x.Id);
        var userEmails = canManageAllOrders
            ? await GetUserEmailsAsync(rows.Select(x => x.Conversion.UserId))
            : new Dictionary<Guid, string>();
        var result = rows.Select(x => Map(x.Order, x.Conversion,
            x.Conversion.TrackingId.HasValue && trackingById.TryGetValue(x.Conversion.TrackingId.Value, out var tracking)
                ? tracking
                : null,
            x.Conversion.UserId.HasValue && userEmails.TryGetValue(x.Conversion.UserId.Value, out var email)
                ? email
                : null)).ToList();
        var orderIds = result.Select(x => x.Id).ToList();
        if (orderIds.Count > 0)
        {
            var itemsByOrder = (await _items.GetListAsync(x => orderIds.Contains(x.OrderId)))
                .GroupBy(x => x.OrderId)
                .ToDictionary(group => group.Key, group => group.OrderBy(x => x.ExternalItemId, StringComparer.Ordinal).Select(MapItem).ToList());
            foreach (var order in result)
            {
                if (itemsByOrder.TryGetValue(order.Id, out var items)) order.Items = items;
            }
        }

        return new PagedResultDto<AffiliateOrderDto>(total, result);
    }

    public async Task<AffiliateOrderDto> GetAsync(Guid id)
    {
        var order = await _orders.GetAsync(id);
        var conversion = await _conversions.GetAsync(order.ConversionId);
        var canManageAllOrders = await AuthorizationService.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders);
        if (!canManageAllOrders && conversion.UserId != CurrentUser.GetId())
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        var tracking = conversion.TrackingId.HasValue
            ? await _trackings.FindAsync(conversion.TrackingId.Value)
            : null;
        var userEmails = canManageAllOrders
            ? await GetUserEmailsAsync(new[] { conversion.UserId })
            : new Dictionary<Guid, string>();
        var dto = Map(order, conversion, tracking,
            conversion.UserId.HasValue && userEmails.TryGetValue(conversion.UserId.Value, out var email) ? email : null);
        dto.Items = (await _items.GetListAsync(x => x.OrderId == id)).OrderBy(x => x.ExternalItemId, StringComparer.Ordinal)
            .Select(MapItem).ToList();
        return dto;
    }

    private async Task<Dictionary<Guid, string>> GetUserEmailsAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var query = (await _users.GetQueryableAsync()).Where(x => ids.Contains(x.Id));
        return (await AsyncExecuter.ToListAsync(query))
            .ToDictionary(x => x.Id, x => x.Email ?? x.UserName);
    }

    private static AffiliateOrderDto Map(AffiliateOrder order, AffiliateConversion conversion, AffiliateTracking? tracking,
        string? userEmail) => new()
    {
        Id = order.Id, CreationTime = order.CreationTime, CreatorId = order.CreatorId,
        LastModificationTime = order.LastModificationTime, LastModifierId = order.LastModifierId,
        IsDeleted = order.IsDeleted, DeleterId = order.DeleterId, DeletionTime = order.DeletionTime,
        ConversionId = order.ConversionId, UserId = conversion.UserId, UserEmail = userEmail,
        ExternalOrderId = order.ExternalOrderId, Status = order.Status,
        PurchaseTime = conversion.PurchaseTime, ClickTime = conversion.ClickTime, ShopType = order.ShopType,
        ProductImageUrl = tracking?.ImageUrl, PurchaseAmount = order.PurchaseAmount,
        ExpectedUserCommission = order.UserCommissionSnapshot,
        PayableUserCommission = order.PayableUserCommission,
        SettledNetCommission = order.SettledNetCommission, SettlementReference = order.SettlementReference,
        SettledAt = order.SettledAt,
        LastUpdatedAt = order.LastModificationTime ?? order.CreationTime
    };

    private static AffiliateOrderItemDto MapItem(AffiliateOrderItem item) => new()
    {
        Id = item.Id, ProductName = item.ProductName, PurchaseAmount = item.PurchaseAmount, Quantity = item.Quantity
    };
}
