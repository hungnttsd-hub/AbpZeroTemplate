using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Settings)]
public class AdminAffiliateSettingsAppService : WebHoanTienAppService, IAdminAffiliateSettingsAppService
{
    private readonly IConfiguration _configuration;
    public AdminAffiliateSettingsAppService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<AffiliateConnectionStatusDto> GetAsync() => Task.FromResult(new AffiliateConnectionStatusDto
    {
        Platform = AffiliatePlatform.Shopee,
        Mode = "affiliate_id + sub_id",
        IsConfigured = !string.IsNullOrWhiteSpace(_configuration["Shopee:AffiliateId"]),
        Endpoint = _configuration["Shopee:ProductDataEndpoint"] ?? "https://data.addlivetag.com/product-data/product-data.php",
        HourlyRateLimit = 300
    });
}

[Authorize(WebHoanTienPermissions.Admin.CommissionRules)]
public class AdminCommissionRuleAppService : WebHoanTienAppService, IAdminCommissionRuleAppService
{
    private readonly IRepository<AffiliateCommissionRule, Guid> _repository;
    private readonly AffiliateCommissionRuleManager _manager;
    private readonly IClock _clock;
    public AdminCommissionRuleAppService(IRepository<AffiliateCommissionRule, Guid> repository, AffiliateCommissionRuleManager manager, IClock clock)
    { _repository = repository; _manager = manager; _clock = clock; }

    public async Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync() => new(
        (await _repository.GetListAsync()).OrderByDescending(x => x.EffectiveFrom).Select(Map).ToList());

    public async Task<AffiliateCommissionRuleDto> GetCurrentAsync() => Map(
        await _manager.GetForPurchaseAsync(AffiliatePlatform.Shopee, _clock.Now));

    [UnitOfWork]
    public async Task<AffiliateCommissionRuleDto> SetCurrentRateAsync(SetCurrentCommissionRateInput input)
    {
        var now = _clock.Now;
        var activeRules = await _repository.GetListAsync(x => x.Platform == AffiliatePlatform.Shopee && x.IsActive);
        var current = activeRules.Where(x => x.AppliesAt(now)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault()
            ?? throw new BusinessException(WebHoanTienDomainErrorCodes.CommissionRuleNotFound);
        if (current.UserShareRate == input.UserShareRate) return Map(current);

        if (current.EffectiveFrom >= now)
        {
            current.ChangeUserShareRate(input.UserShareRate);
            await _repository.UpdateAsync(current, autoSave: true);
            return Map(current);
        }

        var nextEffectiveFrom = activeRules.Where(x => x.EffectiveFrom > now)
            .OrderBy(x => x.EffectiveFrom).Select(x => (DateTime?)x.EffectiveFrom).FirstOrDefault();
        current.CloseAt(now);
        await _repository.UpdateAsync(current, autoSave: true);

        var replacement = new AffiliateCommissionRule(GuidGenerator.Create(), AffiliatePlatform.Shopee,
            input.UserShareRate, now, nextEffectiveFrom);
        await _repository.InsertAsync(replacement, autoSave: true);
        return Map(replacement);
    }

    public async Task<AffiliateCommissionRuleDto> CreateAsync(CreateCommissionRuleInput input)
    {
        await _manager.EnsureNoOverlapAsync(input.Platform, input.EffectiveFrom, input.EffectiveTo);
        var entity = new AffiliateCommissionRule(GuidGenerator.Create(), input.Platform, input.UserShareRate, input.EffectiveFrom, input.EffectiveTo);
        await _repository.InsertAsync(entity, autoSave: true);
        return Map(entity);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var rule = await _repository.GetAsync(id);
        rule.Deactivate();
        await _repository.UpdateAsync(rule, autoSave: true);
    }

    private static AffiliateCommissionRuleDto Map(AffiliateCommissionRule x) => new()
    {
        Id = x.Id, CreationTime = x.CreationTime, CreatorId = x.CreatorId, LastModificationTime = x.LastModificationTime,
        LastModifierId = x.LastModifierId, IsDeleted = x.IsDeleted, DeleterId = x.DeleterId, DeletionTime = x.DeletionTime,
        Platform = x.Platform, UserShareRate = x.UserShareRate, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsActive = x.IsActive
    };
}

[Authorize(WebHoanTienPermissions.Admin.Orders)]
public class AdminAffiliateOrderAppService : WebHoanTienAppService, IAdminAffiliateOrderAppService
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    public AdminAffiliateOrderAppService(IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateTracking, Guid> trackings, IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items)
    { _conversions = conversions; _trackings = trackings; _orders = orders; _items = items; }

    public async Task<PagedResultDto<AdminAffiliateConversionDto>> GetListAsync(AdminAffiliateConversionListInput input)
    {
        var query = await _conversions.GetQueryableAsync();
        if (input.Platform.HasValue) query = query.Where(x => x.Platform == input.Platform.Value);
        if (input.Status.HasValue) query = query.Where(x => x.Status == input.Status.Value);
        if (input.IsMatched.HasValue) query = input.IsMatched.Value
            ? query.Where(x => x.TrackingId.HasValue)
            : query.Where(x => !x.TrackingId.HasValue);
        if (input.From.HasValue) query = query.Where(x => x.PurchaseTime >= input.From.Value);
        if (input.To.HasValue) query = query.Where(x => x.PurchaseTime < input.To.Value);
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim();
            var orderConversionIds = (await _orders.GetQueryableAsync())
                .Where(x => x.ExternalOrderId.Contains(filter))
                .Select(x => x.ConversionId);
            query = query.Where(x => x.ExternalConversionId.Contains(filter) ||
                (x.AttributionValue != null && x.AttributionValue.Contains(filter)) ||
                orderConversionIds.Contains(x.Id));
        }

        query = query.OrderByDescending(x => x.PurchaseTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AdminAffiliateConversionDto>(total, rows.Select(MapConversion).ToList());
    }

    public async Task<AdminAffiliateConversionDetailsDto> GetAsync(Guid id)
    {
        var conversion = await _conversions.GetAsync(id);
        var result = MapConversionDetails(conversion);
        var orders = (await _orders.GetListAsync(x => x.ConversionId == id)).OrderBy(x => x.ExternalOrderId, StringComparer.Ordinal).ToList();
        foreach (var order in orders)
        {
            var dto = MapOrder(order, conversion);
            dto.Items = (await _items.GetListAsync(x => x.OrderId == order.Id))
                .OrderBy(x => x.ExternalItemId, StringComparer.Ordinal)
                .Select(x => new AdminAffiliateOrderItemDto
                {
                    Id = x.Id, ExternalItemId = x.ExternalItemId, ModelId = x.ModelId, ProductName = x.ProductName,
                    PurchaseAmount = x.PurchaseAmount, Quantity = x.Quantity, UserCommission = x.UserCommissionSnapshot,
                    RefundAmount = x.RefundAmount, IsFraud = x.IsFraud, ProviderStatus = x.ProviderStatus
                }).ToList();
            result.Orders.Add(dto);
        }
        return result;
    }

    [Authorize(WebHoanTienPermissions.Admin.ManualMatch)]
    public async Task ManualMatchAsync(Guid conversionId, ManualMatchInput input)
    {
        var conversion = await _conversions.GetAsync(conversionId);
        var tracking = await _trackings.GetAsync(input.TrackingId);
        if (conversion.Platform != tracking.Platform) throw new UserFriendlyException("Tracking và conversion phải cùng nền tảng.");
        conversion.MapTo(tracking.Id, tracking.UserId, conversion.AttributionValue);
        await _conversions.UpdateAsync(conversion, autoSave: true);
    }

    private static AdminAffiliateConversionDto MapConversion(AffiliateConversion x) => new()
    {
        Id = x.Id, Platform = x.Platform, ExternalConversionId = x.ExternalConversionId,
        TrackingId = x.TrackingId, UserId = x.UserId, AttributionValue = x.AttributionValue,
        PurchaseTime = x.PurchaseTime, Status = x.Status, GrossCommission = x.GrossCommission,
        NetCommission = x.NetCommission, CommissionSource = x.CommissionSource,
        UserShareRate = x.UserShareRate, UserCommission = x.UserCommissionSnapshot,
        PayableUserCommission = x.PayableUserCommission, LastProviderUpdateAt = x.LastProviderUpdateAt
    };

    private static AdminAffiliateConversionDetailsDto MapConversionDetails(AffiliateConversion x) => new()
    {
        Id = x.Id, Platform = x.Platform, ExternalConversionId = x.ExternalConversionId,
        TrackingId = x.TrackingId, UserId = x.UserId, AttributionValue = x.AttributionValue,
        PurchaseTime = x.PurchaseTime, Status = x.Status, GrossCommission = x.GrossCommission,
        NetCommission = x.NetCommission, CommissionSource = x.CommissionSource,
        UserShareRate = x.UserShareRate, UserCommission = x.UserCommissionSnapshot,
        PayableUserCommission = x.PayableUserCommission, LastProviderUpdateAt = x.LastProviderUpdateAt
    };

    private static AdminAffiliateOrderDto MapOrder(AffiliateOrder order, AffiliateConversion conversion) => new()
    {
        Id = order.Id, CreationTime = order.CreationTime, CreatorId = order.CreatorId,
        LastModificationTime = order.LastModificationTime, LastModifierId = order.LastModifierId,
        IsDeleted = order.IsDeleted, DeleterId = order.DeleterId, DeletionTime = order.DeletionTime,
        ConversionId = order.ConversionId, ExternalOrderId = order.ExternalOrderId, Status = order.Status,
        PurchaseTime = conversion.PurchaseTime, ShopType = order.ShopType, PurchaseAmount = order.PurchaseAmount,
        NetCommission = order.NetCommission, UserCommission = order.UserCommissionSnapshot,
        PayableUserCommission = order.PayableUserCommission,
        SettledNetCommission = order.SettledNetCommission, SettledUserCommission = order.SettledUserCommission,
        SettlementReference = order.SettlementReference, SettledAt = order.SettledAt,
        LastUpdatedAt = order.LastModificationTime ?? order.CreationTime
    };
}
