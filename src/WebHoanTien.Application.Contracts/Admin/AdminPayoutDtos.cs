using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

public sealed class AdminPayoutListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)] public string? Filter { get; set; }
    public WithdrawalRequestStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class AdminPayoutRequestDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string RequestCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public WithdrawalRequestStatus Status { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public string? PaymentReference { get; set; }
    public string? AdminNote { get; set; }
    public string? RejectionReason { get; set; }
    public bool HasProof { get; set; }
    public bool IsBacked { get; set; }
    public decimal UserConfirmedAmount { get; set; }
    public decimal UserPaidAmount { get; set; }
}

public sealed class AdminPayoutSummaryDto
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int PaidCount { get; set; }
    public decimal PaidAmount { get; set; }
    public int RejectedCount { get; set; }
}

public sealed class AdminPayoutPageDto
{
    public AdminPayoutSummaryDto Summary { get; set; } = new();
    public PagedResultDto<AdminPayoutRequestDto> Requests { get; set; } = new();
}

public sealed class MarkWithdrawalPaidInput
{
    [Required, StringLength(128)] public string PaymentReference { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    [StringLength(1000)] public string? AdminNote { get; set; }
}

public sealed class RejectWithdrawalInput
{
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
    [StringLength(1000)] public string? AdminNote { get; set; }
}

public interface IAdminPayoutAppService : IApplicationService
{
    Task<AdminPayoutPageDto> GetListAsync(AdminPayoutListInput input);
    Task<AdminPayoutRequestDto> GetAsync(Guid id);
    [DisableValidation]
    Task<AdminPayoutRequestDto> MarkPaidAsync(Guid id, MarkWithdrawalPaidInput input, Stream proofStream,
        string proofFileName, string proofContentType, long proofLength, CancellationToken cancellationToken = default);
    Task<AdminPayoutRequestDto> RejectAsync(Guid id, RejectWithdrawalInput input);
    Task<WithdrawalProofDto> GetProofAsync(Guid id);
}
