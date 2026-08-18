using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

public sealed class AffiliateConnectionStatusDto
{
    public AffiliatePlatform Platform { get; set; }
    public string Mode { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int HourlyRateLimit { get; set; }
    public bool AllowTotalCommissionFallback { get; set; }
}

public sealed class UpdateAffiliateSettingsInput
{
    public bool AllowTotalCommissionFallback { get; set; }
}

public interface IAdminAffiliateSettingsAppService : IApplicationService
{
    Task<AffiliateConnectionStatusDto> GetAsync();
    Task<AffiliateConnectionStatusDto> UpdateAsync(UpdateAffiliateSettingsInput input);
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

public interface IAdminCommissionRuleAppService : IApplicationService
{
    Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync();
    Task<AffiliateCommissionRuleDto> CreateAsync(CreateCommissionRuleInput input);
    Task DeactivateAsync(Guid id);
}

public sealed class AffiliateSyncStateDto : FullAuditedEntityDto<Guid>
{
    public AffiliatePlatform Platform { get; set; }
    public AffiliateSyncKind SyncKind { get; set; }
    public DateTime? Watermark { get; set; }
    public DateTime? InitialStartDate { get; set; }
    public DateTime? LastSucceededAt { get; set; }
    public string? LastError { get; set; }
}

public sealed class AffiliateSyncRunDto : EntityDto<Guid>
{
    public AffiliatePlatform Platform { get; set; }
    public AffiliateSyncKind SyncKind { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public AffiliateSyncRunStatus Status { get; set; }
    public int FetchedCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorSummary { get; set; }
}

public sealed class SetInitialSyncDateInput
{
    public DateTime StartDate { get; set; }
}

public sealed class ReconcileInput
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public interface IAdminAffiliateSyncAppService : IApplicationService
{
    Task<ListResultDto<AffiliateSyncStateDto>> GetStatesAsync();
    Task<PagedResultDto<AffiliateSyncRunDto>> GetRunsAsync(PagedAndSortedResultRequestDto input);
    Task SetInitialDateAsync(SetInitialSyncDateInput input);
    Task SyncNowAsync();
    Task ReconcileAsync(ReconcileInput input);
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
    public List<AffiliateOrderDto> Orders { get; set; } = new();
}

public interface IAdminAffiliateOrderAppService : IApplicationService
{
    Task<PagedResultDto<AdminAffiliateConversionDto>> GetListAsync(AdminAffiliateConversionListInput input);
    Task<AdminAffiliateConversionDetailsDto> GetAsync(Guid id);
    Task ManualMatchAsync(Guid conversionId, ManualMatchInput input);
}
