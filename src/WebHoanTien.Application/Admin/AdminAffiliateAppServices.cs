using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using WebHoanTien.Affiliates;
using WebHoanTien.Operations;
using WebHoanTien.Permissions;
using WebHoanTien.Settings;
using WebHoanTien.Integrations.Shopee;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Settings)]
public class AdminAffiliateSettingsAppService : WebHoanTienAppService, IAdminAffiliateSettingsAppService
{
    private readonly IConfiguration _configuration;
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly IShopeeAmsPermissionChecker _permissionChecker;
    public AdminAffiliateSettingsAppService(IConfiguration configuration, ISettingProvider settingProvider,
        ISettingManager settingManager, IShopeeAmsPermissionChecker permissionChecker)
    {
        _configuration = configuration;
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _permissionChecker = permissionChecker;
    }

    public async Task<AffiliateConnectionStatusDto> GetAsync() => new()
    {
        Platform = AffiliatePlatform.Shopee,
        Mode = _configuration["Affiliate:ProviderMode"] ?? "Shopee",
        IsConfigured = !string.IsNullOrWhiteSpace(_configuration["Shopee:AppId"]) && !string.IsNullOrWhiteSpace(_configuration["Shopee:Secret"]),
        Endpoint = _configuration["Shopee:Endpoint"] ?? "https://open-api.affiliate.shopee.vn/graphql",
        HourlyRateLimit = 8000,
        AllowTotalCommissionFallback = await _settingProvider.GetAsync<bool>(WebHoanTienSettings.AllowTotalCommissionFallback)
    };

    public async Task<AffiliateConnectionStatusDto> UpdateAsync(UpdateAffiliateSettingsInput input)
    {
        await _settingManager.SetGlobalAsync(
            WebHoanTienSettings.AllowTotalCommissionFallback,
            input.AllowTotalCommissionFallback.ToString().ToLowerInvariant());
        return await GetAsync();
    }

    public async Task<ShopeeAmsPermissionCheckDto> CheckPermissionAsync(CancellationToken cancellationToken = default)
    {
        var result = await _permissionChecker.CheckPermissionAsync(cancellationToken);
        return new ShopeeAmsPermissionCheckDto
        {
            IsConfigured = result.IsConfigured,
            HasPermission = result.HasPermission,
            CheckedAtUtc = result.CheckedAtUtc,
            HttpStatusCode = result.HttpStatusCode,
            Error = result.Error,
            Message = result.Message,
            RequestId = result.RequestId,
            ReturnedRecords = result.ReturnedRecords
        };
    }
}

[Authorize(WebHoanTienPermissions.Admin.CommissionRules)]
public class AdminCommissionRuleAppService : WebHoanTienAppService, IAdminCommissionRuleAppService
{
    private readonly IRepository<AffiliateCommissionRule, Guid> _repository;
    private readonly AffiliateCommissionRuleManager _manager;
    public AdminCommissionRuleAppService(IRepository<AffiliateCommissionRule, Guid> repository, AffiliateCommissionRuleManager manager)
    { _repository = repository; _manager = manager; }

    public async Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync() => new(
        (await _repository.GetListAsync()).OrderByDescending(x => x.EffectiveFrom).Select(Map).ToList());

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
                .Select(x => new AffiliateOrderItemDto
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

    private static AffiliateOrderDto MapOrder(AffiliateOrder order, AffiliateConversion conversion) => new()
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

[Authorize(WebHoanTienPermissions.Admin.Sync)]
public class AdminAffiliateSyncAppService : WebHoanTienAppService, IAdminAffiliateSyncAppService
{
    private readonly IRepository<AffiliateSyncState, Guid> _states;
    private readonly IRepository<AffiliateSyncRun, Guid> _runs;
    private readonly IAffiliateSyncCoordinator _coordinator;
    public AdminAffiliateSyncAppService(IRepository<AffiliateSyncState, Guid> states, IRepository<AffiliateSyncRun, Guid> runs, IAffiliateSyncCoordinator coordinator)
    { _states = states; _runs = runs; _coordinator = coordinator; }

    public async Task<ListResultDto<AffiliateSyncStateDto>> GetStatesAsync() => new(
        (await _states.GetListAsync()).Select(x => new AffiliateSyncStateDto
        {
            Id = x.Id, CreationTime = x.CreationTime, CreatorId = x.CreatorId, LastModificationTime = x.LastModificationTime,
            LastModifierId = x.LastModifierId, IsDeleted = x.IsDeleted, DeleterId = x.DeleterId, DeletionTime = x.DeletionTime,
            Platform = x.Platform, SyncKind = x.SyncKind, Watermark = x.Watermark, InitialStartDate = x.InitialStartDate,
            LastSucceededAt = x.LastSucceededAt, LastError = x.LastError
        }).ToList());

    public async Task<PagedResultDto<AffiliateSyncRunDto>> GetRunsAsync(PagedAndSortedResultRequestDto input)
    {
        var query = (await _runs.GetQueryableAsync()).OrderByDescending(x => x.StartedAt);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AffiliateSyncRunDto>(total, rows.Select(x => new AffiliateSyncRunDto
        {
            Id = x.Id, Platform = x.Platform, SyncKind = x.SyncKind, StartedAt = x.StartedAt, FinishedAt = x.FinishedAt,
            Status = x.Status, FetchedCount = x.FetchedCount, InsertedCount = x.InsertedCount, UpdatedCount = x.UpdatedCount,
            UnmatchedCount = x.UnmatchedCount, ErrorCount = x.ErrorCount, ErrorSummary = x.ErrorSummary
        }).ToList());
    }

    public async Task SetInitialDateAsync(SetInitialSyncDateInput input)
    {
        var now = Clock.Now;
        if (input.StartDate > now || input.StartDate < now.AddMonths(-3)) throw new UserFriendlyException("Ngày bắt đầu phải nằm trong 3 tháng gần nhất.");
        var state = (await _states.GetListAsync(x => x.Platform == AffiliatePlatform.Shopee && x.SyncKind == AffiliateSyncKind.Conversion)).FirstOrDefault();
        var isNew = state is null;
        state ??= new AffiliateSyncState(GuidGenerator.Create(), AffiliatePlatform.Shopee, AffiliateSyncKind.Conversion);
        state.SetInitialStartDate(input.StartDate);
        if (isNew) await _states.InsertAsync(state, autoSave: true);
        else await _states.UpdateAsync(state, autoSave: true);
    }

    public Task SyncNowAsync() => _coordinator.EnqueueSyncAsync(AffiliateSyncKind.Conversion);

    public Task ReconcileAsync(ReconcileInput input)
    {
        if (input.To <= input.From || input.To - input.From > TimeSpan.FromDays(93))
            throw new UserFriendlyException("Khoảng reconcile phải hợp lệ và không vượt quá 3 tháng.");
        return _coordinator.EnqueueSyncAsync(AffiliateSyncKind.Reconciliation, input.From, input.To);
    }
}
