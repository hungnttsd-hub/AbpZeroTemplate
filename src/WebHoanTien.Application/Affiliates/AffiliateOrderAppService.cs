using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using WebHoanTien.Operations;

namespace WebHoanTien.Affiliates;

[Authorize]
public class AffiliateOrderAppService : WebHoanTienAppService, IAffiliateOrderAppService
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IAffiliateSyncCoordinator _syncCoordinator;

    public AffiliateOrderAppService(IRepository<AffiliateConversion, Guid> conversions, IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items, IAffiliateSyncCoordinator syncCoordinator)
    {
        _conversions = conversions;
        _orders = orders;
        _items = items;
        _syncCoordinator = syncCoordinator;
    }

    public async Task<PagedResultDto<AffiliateOrderDto>> GetListAsync(AffiliateOrderListInput input)
    {
        var userId = CurrentUser.GetId();
        var conversionQuery = (await _conversions.GetQueryableAsync()).Where(x => x.UserId == userId);
        if (input.From.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime >= input.From.Value);
        if (input.To.HasValue) conversionQuery = conversionQuery.Where(x => x.PurchaseTime < input.To.Value);

        var query = from order in await _orders.GetQueryableAsync()
                    join conversion in conversionQuery on order.ConversionId equals conversion.Id
                    select new { Order = order, Conversion = conversion };
        if (input.Status.HasValue) query = query.Where(x => x.Order.Status == input.Status.Value);
        query = query.OrderByDescending(x => x.Conversion.PurchaseTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AffiliateOrderDto>(total, rows.Select(x => Map(x.Order, x.Conversion)).ToList());
    }

    public async Task<AffiliateOrderDto> GetAsync(Guid id)
    {
        var order = await _orders.GetAsync(id);
        var conversion = await _conversions.GetAsync(order.ConversionId);
        if (conversion.UserId != CurrentUser.GetId()) throw new Volo.Abp.Authorization.AbpAuthorizationException();
        var dto = Map(order, conversion);
        dto.Items = (await _items.GetListAsync(x => x.OrderId == id)).OrderBy(x => x.ExternalItemId, StringComparer.Ordinal)
            .Select(x => new AffiliateOrderItemDto
            {
                Id = x.Id, ExternalItemId = x.ExternalItemId, ModelId = x.ModelId, ProductName = x.ProductName,
                PurchaseAmount = x.PurchaseAmount, Quantity = x.Quantity, UserCommission = x.UserCommissionSnapshot,
                RefundAmount = x.RefundAmount, IsFraud = x.IsFraud, ProviderStatus = x.ProviderStatus
            }).ToList();
        return dto;
    }

    public Task RequestSyncAsync() => _syncCoordinator.RequestPrioritySyncAsync(CurrentUser.GetId());

    private static AffiliateOrderDto Map(AffiliateOrder order, AffiliateConversion conversion) => new()
    {
        Id = order.Id, CreationTime = order.CreationTime, CreatorId = order.CreatorId,
        LastModificationTime = order.LastModificationTime, LastModifierId = order.LastModifierId,
        IsDeleted = order.IsDeleted, DeleterId = order.DeleterId, DeletionTime = order.DeletionTime,
        ConversionId = order.ConversionId, ExternalOrderId = order.ExternalOrderId, Status = order.Status,
        PurchaseTime = conversion.PurchaseTime, ShopType = order.ShopType, PurchaseAmount = order.PurchaseAmount,
        NetCommission = order.NetCommission, UserCommission = order.UserCommissionSnapshot,
        PayableUserCommission = order.PayableUserCommission,
        LastUpdatedAt = order.LastModificationTime ?? order.CreationTime
    };
}
