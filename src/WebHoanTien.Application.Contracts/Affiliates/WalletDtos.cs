using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace WebHoanTien.Affiliates;

public sealed class CustomerWalletOverviewDto
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalRecordedAmount { get; set; }
    public decimal PendingCommissionAmount { get; set; }
    public decimal MinimumWithdrawalAmount { get; set; }
    public bool HasPayoutAccount { get; set; }
    public WithdrawalRequestDto? PendingWithdrawal { get; set; }
    public List<WalletMovementDto> RecentMovements { get; set; } = new();
}

public sealed class WithdrawalPreparationDto
{
    public CustomerWalletOverviewDto Wallet { get; set; } = new();
    public PayoutAccountDto? PayoutAccount { get; set; }
    public decimal FeeAmount { get; set; }
    public string ProcessingTimeLabel { get; set; } = "1–3 ngày làm việc";
    public List<WithdrawalRequestDto> RecentWithdrawals { get; set; } = new();
}

public sealed class WalletMovementDto
{
    public Guid Id { get; set; }
    public WalletMovementKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public decimal Amount { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public bool CanCancel { get; set; }
    public bool HasProof { get; set; }
}

public sealed class WithdrawalRequestDto : FullAuditedEntityDto<Guid>
{
    public string RequestCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public WithdrawalRequestStatus Status { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string MaskedAccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? PaymentReference { get; set; }
    public string? AdminNote { get; set; }
    public string? RejectionReason { get; set; }
    public bool CanCancel { get; set; }
    public bool HasProof { get; set; }
}

public sealed class CreateWithdrawalRequestInput
{
    [Range(typeof(decimal), "10000", "999999999999999", ErrorMessage = "Số tiền rút tối thiểu là 10.000đ.")]
    public decimal Amount { get; set; }
}

public sealed class WalletHistoryInput : PagedAndSortedResultRequestDto
{
    public WalletMovementKind? Kind { get; set; }
}

public sealed class WithdrawalProofDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public interface ICustomerWalletAppService : IApplicationService
{
    Task<CustomerWalletOverviewDto> GetOverviewAsync();
    Task<WithdrawalPreparationDto> GetWithdrawalPreparationAsync();
    Task<PagedResultDto<WalletMovementDto>> GetHistoryAsync(WalletHistoryInput input);
    Task<WithdrawalRequestDto> CreateWithdrawalRequestAsync(CreateWithdrawalRequestInput input);
    Task<WithdrawalRequestDto> CancelWithdrawalRequestAsync(Guid id);
    Task<WithdrawalProofDto> GetProofAsync(Guid id);
}
