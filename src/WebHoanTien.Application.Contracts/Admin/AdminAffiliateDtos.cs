using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

public sealed class AffiliateConnectionStatusDto
{
    public AffiliatePlatform Platform { get; set; }
    public string Mode { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int HourlyRateLimit { get; set; }
}

public interface IAdminAffiliateSettingsAppService : IApplicationService
{
    Task<AffiliateConnectionStatusDto> GetAsync();
}

public sealed class AffiliateCommissionRuleDto : FullAuditedEntityDto<Guid>
{
    public AffiliatePlatform Platform { get; set; }
    public decimal UserShareRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateCommissionRuleInput
{
    public AffiliatePlatform Platform { get; set; } = AffiliatePlatform.Shopee;
    [Range(0, 100)] public decimal UserShareRate { get; set; } = WebHoanTienConsts.DefaultUserShareRate;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class SetCurrentCommissionRateInput
{
    [Range(0, 100)] public decimal UserShareRate { get; set; } = WebHoanTienConsts.DefaultUserShareRate;
}

public interface IAdminCommissionRuleAppService : IApplicationService
{
    Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync();
    Task<AffiliateCommissionRuleDto> GetCurrentAsync();
    Task<AffiliateCommissionRuleDto> SetCurrentRateAsync(SetCurrentCommissionRateInput input);
    Task<AffiliateCommissionRuleDto> CreateAsync(CreateCommissionRuleInput input);
    Task DeactivateAsync(Guid id);
}

public sealed class ManualMatchInput
{
    public Guid TrackingId { get; set; }
}

public sealed class AdminAffiliateConversionListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)] public string? Filter { get; set; }
    public AffiliatePlatform? Platform { get; set; }
    public AffiliateConversionStatus? Status { get; set; }
    public bool? IsMatched { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class AdminAffiliateConversionDto : EntityDto<Guid>
{
    public AffiliatePlatform Platform { get; set; }
    public string ExternalConversionId { get; set; } = string.Empty;
    public Guid? TrackingId { get; set; }
    public Guid? UserId { get; set; }
    public string? AttributionValue { get; set; }
    public DateTime PurchaseTime { get; set; }
    public AffiliateConversionStatus Status { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal NetCommission { get; set; }
    public CommissionSource CommissionSource { get; set; }
    public decimal UserShareRate { get; set; }
    public decimal UserCommission { get; set; }
    public decimal PayableUserCommission { get; set; }
    public DateTime LastProviderUpdateAt { get; set; }
}

public sealed class AdminAffiliateConversionDetailsDto : AdminAffiliateConversionDto
{
    public List<AdminAffiliateOrderDto> Orders { get; set; } = new();
}

public sealed class AdminAffiliateOrderDto : FullAuditedEntityDto<Guid>
{
    public Guid ConversionId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public AffiliateOrderStatus Status { get; set; }
    public DateTime PurchaseTime { get; set; }
    public string? ShopType { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal NetCommission { get; set; }
    public decimal UserCommission { get; set; }
    public decimal PayableUserCommission { get; set; }
    public decimal? SettledNetCommission { get; set; }
    public decimal? SettledUserCommission { get; set; }
    public string? SettlementReference { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<AdminAffiliateOrderItemDto> Items { get; set; } = new();
}

public sealed class AdminAffiliateOrderItemDto : EntityDto<Guid>
{
    public string ExternalItemId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public decimal PurchaseAmount { get; set; }
    public int Quantity { get; set; }
    public decimal UserCommission { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsFraud { get; set; }
    public string? ProviderStatus { get; set; }
}

public interface IAdminAffiliateOrderAppService : IApplicationService
{
    Task<PagedResultDto<AdminAffiliateConversionDto>> GetListAsync(AdminAffiliateConversionListInput input);
    Task<AdminAffiliateConversionDetailsDto> GetAsync(Guid id);
    Task ManualMatchAsync(Guid conversionId, ManualMatchInput input);
}

public sealed class ShopeeReportImportResultDto
{
    public int ImportedRowCount { get; set; }
    public int ConversionCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public interface IAdminShopeeReportImportAppService : IApplicationService
{
    [DisableValidation]
    Task<ShopeeReportImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default);
}

public interface IShopeeAutomationImportAppService : IApplicationService
{
    [DisableValidation]
    Task<ShopeeReportImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default);
}

public sealed class ShopeeSettlementImportResultDto
{
    public Guid BatchId { get; set; }
    public int ImportedRowCount { get; set; }
    public int ValidationCount { get; set; }
    public int AlreadyImportedValidationCount { get; set; }
    public int UpdatedValidationCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int AlreadySettledCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int ErrorCount { get; set; }
    public int WaitingPaymentCount { get; set; }
    public bool IsDuplicate { get; set; }
    public List<string> Errors { get; set; } = new();
}

public interface IAdminShopeeSettlementImportAppService : IApplicationService
{
    [DisableValidation]
    Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default);
}

public interface IShopeeAutomationSettlementImportAppService : IApplicationService
{
    [DisableValidation]
    Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default);
}
