using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

public sealed class AdminShopeeSettlementBatchListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)] public string? Filter { get; set; }
    public ShopeeSettlementBatchStatus? Status { get; set; }
}

public sealed class AdminShopeeSettlementSummaryDto
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int IssueCount { get; set; }
}

public sealed class AdminShopeeSettlementBatchDto : CreationAuditedEntityDto<Guid>
{
    public ShopeeSettlementImportSource Source { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public ShopeeSettlementBatchStatus Status { get; set; }
    public int BillCount { get; set; }
    public int RecordCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int AlreadySettledCount { get; set; }
    public int InvalidCount { get; set; }
    public int WaitingPaymentCount { get; set; }
    public decimal TotalEligibleCommission { get; set; }
    public decimal TotalPaidCommission { get; set; }
    public decimal PendingPaidCommission { get; set; }
    public decimal ApprovedPaidCommission { get; set; }
}

public sealed class AdminShopeeSettlementRecordDto : CreationAuditedEntityDto<Guid>
{
    public Guid BatchId { get; set; }
    public Guid BillId { get; set; }
    public string SourceAffiliateId { get; set; } = string.Empty;
    public string ValidationId { get; set; } = string.Empty;
    public string PayoutId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public int PaymentStatus { get; set; }
    public int ValidationPayoutStatus { get; set; }
    public int? OverallValidationStatus { get; set; }
    public int? BillValidationStatus { get; set; }
    public int? SettlementCycle { get; set; }
    public bool IsShopeePaid { get; set; }
    public bool HasAdjustment { get; set; }
    public bool HasClawback { get; set; }
    public bool IsCumulative { get; set; }
    public bool HasBonus { get; set; }
    public bool HasPpp { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public decimal EligibleCommission { get; set; }
    public decimal AllocatedServiceFee { get; set; }
    public decimal AllocatedTax { get; set; }
    public decimal ActualPaidCommission { get; set; }
    public decimal ProjectedUserCommission { get; set; }
    public decimal ApprovedUserCommission { get; set; }
    public ShopeeSettlementRecordStatus Status { get; set; }
    public Guid? AffiliateOrderId { get; set; }
    public Guid? UserId { get; set; }
    public List<string> ProductNames { get; set; } = new();
    public string? UserEmail { get; set; }
    public List<AdminShopeeSettlementRecipientDto> Recipients { get; set; } = new();
    public DateTime? ApprovedAt { get; set; }
    public string? Issue { get; set; }
}

public sealed class AdminShopeeSettlementRecipientDto
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public decimal ProjectedUserCommission { get; set; }
    public decimal ApprovedUserCommission { get; set; }
}

public sealed class AdminShopeeSettlementPageDto
{
    public AdminShopeeSettlementSummaryDto Summary { get; set; } = new();
    public PagedResultDto<AdminShopeeSettlementBatchDto> Batches { get; set; } = new();
}

public sealed class AdminShopeeSettlementBatchDetailsDto
{
    public AdminShopeeSettlementBatchDto Batch { get; set; } = new();
    public PagedResultDto<AdminShopeeSettlementRecordDto> Records { get; set; } = new();
}

public sealed class AdminShopeeSettlementApprovalResultDto
{
    public Guid BatchId { get; set; }
    public int ApprovedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal ApprovedCommission { get; set; }
    public decimal CreditedUserCommission { get; set; }
    public AdminShopeeSettlementBatchDto Batch { get; set; } = new();
}

public sealed class AdminShopeeSettlementRefreshResultDto
{
    public Guid BatchId { get; set; }
    public int CheckedCount { get; set; }
    public int ReadyForApprovalCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int AlreadySettledCount { get; set; }
    public int InvalidCount { get; set; }
    public AdminShopeeSettlementBatchDto Batch { get; set; } = new();
}

public interface IAdminShopeeSettlementApprovalAppService : IApplicationService
{
    Task<AdminShopeeSettlementPageDto> GetListAsync(AdminShopeeSettlementBatchListInput input);
    Task<AdminShopeeSettlementBatchDetailsDto> GetAsync(Guid batchId, int skipCount = 0,
        int maxResultCount = 50);
    Task<AdminShopeeSettlementApprovalResultDto> ApproveAsync(Guid recordId);
    Task<AdminShopeeSettlementApprovalResultDto> ApproveAllAsync(Guid batchId);
    Task<AdminShopeeSettlementRefreshResultDto> RefreshMatchesAsync(Guid batchId);
}
