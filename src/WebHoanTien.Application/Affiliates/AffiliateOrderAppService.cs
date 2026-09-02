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
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<IdentityUser, Guid> _users;

    public AffiliateOrderAppService(IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateOrderItem, Guid> items,
        IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        IRepository<AffiliateTracking, Guid> trackings, IRepository<IdentityUser, Guid> users)
    {
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _attributions = attributions;
        _trackings = trackings;
        _users = users;
    }

    public async Task<PagedResultDto<AffiliateOrderDto>> GetListAsync(AffiliateOrderListInput input)
    {
        var canManageAllOrders = await AuthorizationService.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders);
        Guid? scopedUserId = canManageAllOrders ? null : CurrentUser.GetId();
        var allowedOrderIds = scopedUserId.HasValue
            ? await GetAttributedOrderIdsAsync(scopedUserId.Value)
            : null;

        var conversionQuery = await _conversions.GetQueryableAsync();
        if (input.From.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime >= input.From.Value);
        if (input.To.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime < input.To.Value);
        var query = from order in await _orders.GetQueryableAsync()
                    join conversion in conversionQuery on order.ConversionId equals conversion.Id
                    select new { Order = order, Conversion = conversion };
        if (allowedOrderIds is not null) query = query.Where(x => allowedOrderIds.Contains(x.Order.Id));
        if (input.Status.HasValue) query = query.Where(x => x.Order.Status == input.Status.Value);
        query = query.OrderByDescending(x => x.Conversion.PurchaseTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AffiliateOrderDto>(total,
            await MapRowsAsync(rows.Select(x => (x.Order, x.Conversion)).ToList(), scopedUserId,
                canManageAllOrders));
    }

    public async Task<AffiliateOrderDto> GetAsync(Guid id)
    {
        var order = await _orders.GetAsync(id);
        var conversion = await _conversions.GetAsync(order.ConversionId);
        var canManageAllOrders = await AuthorizationService.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders);
        Guid? scopedUserId = canManageAllOrders ? null : CurrentUser.GetId();
        if (scopedUserId.HasValue)
        {
            var itemIds = (await _items.GetListAsync(x => x.OrderId == id)).Select(x => x.Id).ToList();
            if (itemIds.Count == 0 || !await _attributions.AnyAsync(x => itemIds.Contains(x.OrderItemId) &&
                    x.UserId == scopedUserId && x.Status != AffiliateAttributionStatus.Unmatched))
                throw new Volo.Abp.Authorization.AbpAuthorizationException();
        }

        return (await MapRowsAsync(new List<(AffiliateOrder, AffiliateConversion)> { (order, conversion) },
            scopedUserId, canManageAllOrders))[0];
    }

    private async Task<List<Guid>> GetAttributedOrderIdsAsync(Guid userId)
    {
        var attributions = await _attributions.GetListAsync(x => x.UserId == userId &&
            x.Status != AffiliateAttributionStatus.Unmatched);
        var itemIds = attributions.Select(x => x.OrderItemId).Distinct().ToList();
        if (itemIds.Count == 0) return new List<Guid>();
        return (await _items.GetListAsync(x => itemIds.Contains(x.Id))).Select(x => x.OrderId).Distinct().ToList();
    }

    private async Task<List<AffiliateOrderDto>> MapRowsAsync(
        IReadOnlyCollection<(AffiliateOrder Order, AffiliateConversion Conversion)> rows,
        Guid? scopedUserId, bool includeAdminData)
    {
        var orderIds = rows.Select(x => x.Order.Id).ToList();
        var items = orderIds.Count == 0
            ? new List<AffiliateOrderItem>()
            : await _items.GetListAsync(x => orderIds.Contains(x.OrderId));
        var itemIds = items.Select(x => x.Id).ToList();
        var allAttributions = itemIds.Count == 0
            ? new List<AffiliateOrderItemAttribution>()
            : await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId));
        var visibleAttributions = scopedUserId.HasValue
            ? allAttributions.Where(x => x.UserId == scopedUserId &&
                x.Status != AffiliateAttributionStatus.Unmatched).ToList()
            : allAttributions;
        var trackingIds = visibleAttributions.Where(x => x.TrackingId.HasValue)
            .Select(x => x.TrackingId!.Value).Distinct().ToList();
        var trackingById = trackingIds.Count == 0
            ? new Dictionary<Guid, AffiliateTracking>()
            : (await _trackings.GetListAsync(x => trackingIds.Contains(x.Id))).ToDictionary(x => x.Id);
        var recipientIds = allAttributions.Where(x => x.UserId.HasValue &&
                x.Status != AffiliateAttributionStatus.Unmatched)
            .Select(x => x.UserId!.Value).Distinct().ToList();
        var userEmails = includeAdminData ? await GetUserEmailsAsync(recipientIds) : new Dictionary<Guid, string>();
        var result = new List<AffiliateOrderDto>();
        foreach (var row in rows)
        {
            var orderItems = items.Where(x => x.OrderId == row.Order.Id)
                .OrderBy(x => x.ExternalItemId, StringComparer.Ordinal).ToList();
            var orderItemIds = orderItems.Select(x => x.Id).ToHashSet();
            var orderAllAttributions = allAttributions.Where(x => orderItemIds.Contains(x.OrderItemId)).ToList();
            var orderVisibleAttributions = visibleAttributions.Where(x => orderItemIds.Contains(x.OrderItemId)).ToList();
            var displayedItems = new List<AffiliateOrderItemDto>();
            foreach (var item in orderItems)
            {
                var itemAttributions = orderVisibleAttributions.Where(x => x.OrderItemId == item.Id).ToList();
                if (scopedUserId.HasValue && itemAttributions.Count == 0) continue;
                var imageUrl = itemAttributions.Where(x => x.TrackingId.HasValue)
                    .Select(x => trackingById.GetValueOrDefault(x.TrackingId!.Value)?.ImageUrl)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                displayedItems.Add(new AffiliateOrderItemDto
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    ImageUrl = imageUrl,
                    PurchaseAmount = scopedUserId.HasValue ? itemAttributions.Sum(x => x.PurchaseAmount) : item.PurchaseAmount,
                    Quantity = scopedUserId.HasValue ? itemAttributions.Sum(x => x.Quantity) : item.Quantity,
                    ExpectedUserCommission = itemAttributions.Sum(x => x.UserCommissionSnapshot),
                    SettledUserCommission = itemAttributions.Any(x => x.SettledUserCommission.HasValue)
                        ? itemAttributions.Sum(x => x.SettledUserCommission ?? 0m)
                        : null,
                    Attributions = includeAdminData
                        ? itemAttributions.OrderBy(x => x.AttributionValue, StringComparer.Ordinal)
                            .Select(x => new AffiliateOrderItemAttributionDto
                            {
                                Id = x.Id,
                                AttributionValue = x.AttributionValue,
                                Status = x.Status,
                                UserId = x.UserId,
                                UserEmail = x.UserId.HasValue
                                    ? userEmails.GetValueOrDefault(x.UserId.Value)
                                    : null,
                                AllocatedNetCommission = x.AllocatedNetCommission,
                                SettledNetCommission = x.SettledNetCommission,
                                ExpectedUserCommission = x.UserCommissionSnapshot,
                                SettledUserCommission = x.SettledUserCommission
                            }).ToList()
                        : new List<AffiliateOrderItemAttributionDto>()
                });
            }

            var recipients = orderAllAttributions.Where(x => x.UserId.HasValue &&
                    x.Status != AffiliateAttributionStatus.Unmatched)
                .GroupBy(x => x.UserId!.Value)
                .Select(group => new AffiliateOrderRecipientDto
                {
                    UserId = group.Key,
                    UserEmail = userEmails.GetValueOrDefault(group.Key),
                    ExpectedUserCommission = group.Sum(x => x.UserCommissionSnapshot),
                    SettledUserCommission = group.Any(x => x.SettledUserCommission.HasValue)
                        ? group.Sum(x => x.SettledUserCommission ?? 0m)
                        : null
                }).OrderBy(x => x.UserEmail ?? x.UserId.ToString(), StringComparer.OrdinalIgnoreCase).ToList();
            var expected = scopedUserId.HasValue
                ? orderVisibleAttributions.Sum(x => x.UserCommissionSnapshot)
                : row.Order.UserCommissionSnapshot;
            var payable = scopedUserId.HasValue
                ? orderVisibleAttributions.Sum(x => x.SettledUserCommission ?? 0m)
                : row.Order.PayableUserCommission;
            var primaryRecipient = scopedUserId.HasValue
                ? recipients.FirstOrDefault(x => x.UserId == scopedUserId.Value)
                : recipients.FirstOrDefault();
            result.Add(new AffiliateOrderDto
            {
                Id = row.Order.Id,
                CreationTime = row.Order.CreationTime,
                CreatorId = row.Order.CreatorId,
                LastModificationTime = row.Order.LastModificationTime,
                LastModifierId = row.Order.LastModifierId,
                IsDeleted = row.Order.IsDeleted,
                DeleterId = row.Order.DeleterId,
                DeletionTime = row.Order.DeletionTime,
                ConversionId = row.Order.ConversionId,
                UserId = scopedUserId ?? primaryRecipient?.UserId,
                UserEmail = primaryRecipient?.UserEmail,
                ExternalOrderId = row.Order.ExternalOrderId,
                Status = row.Order.Status,
                PurchaseTime = row.Conversion.PurchaseTime,
                ClickTime = row.Conversion.ClickTime,
                ShopType = row.Order.ShopType,
                ProductImageUrl = displayedItems.Select(x => x.ImageUrl)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                PurchaseAmount = scopedUserId.HasValue
                    ? orderVisibleAttributions.Sum(x => x.PurchaseAmount)
                    : row.Order.PurchaseAmount,
                ExpectedUserCommission = expected,
                PayableUserCommission = payable,
                SettledNetCommission = scopedUserId.HasValue &&
                    orderVisibleAttributions.Any(x => x.SettledNetCommission.HasValue)
                    ? orderVisibleAttributions.Sum(x => x.SettledNetCommission ?? 0m)
                    : scopedUserId.HasValue ? null : row.Order.SettledNetCommission,
                SettlementReference = row.Order.SettlementReference,
                SettledAt = row.Order.SettledAt,
                LastUpdatedAt = row.Order.LastModificationTime ?? row.Order.CreationTime,
                AttributionCount = scopedUserId.HasValue
                    ? orderVisibleAttributions.Count
                    : orderAllAttributions.Count,
                UnmatchedAttributionCount = scopedUserId.HasValue
                    ? 0
                    : orderAllAttributions.Count(x => x.Status != AffiliateAttributionStatus.Matched),
                Recipients = includeAdminData ? recipients : new List<AffiliateOrderRecipientDto>(),
                Items = displayedItems
            });
        }

        return result;
    }

    private async Task<Dictionary<Guid, string>> GetUserEmailsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        return (await _users.GetListAsync(x => ids.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Email ?? x.UserName);
    }
}
