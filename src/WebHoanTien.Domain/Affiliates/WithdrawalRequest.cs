using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class WithdrawalRequest : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string RequestCode { get; private set; } = null!;
    public Guid PayoutAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public WithdrawalRequestStatus Status { get; private set; }
    public string BankCode { get; private set; } = null!;
    public string AccountNumber { get; private set; } = null!;
    public string AccountHolderName { get; private set; } = null!;
    public DateTime? ProcessedAt { get; private set; }
    public Guid? ProcessedByUserId { get; private set; }
    public string? PaymentReference { get; private set; }
    public string? AdminNote { get; private set; }
    public string? RejectionReason { get; private set; }

    protected WithdrawalRequest() { }

    public WithdrawalRequest(Guid id, Guid userId, string requestCode, UserPayoutAccount payoutAccount,
        decimal amount, decimal feeAmount) : base(id)
    {
        if (amount < WebHoanTienConsts.MinimumWithdrawalAmount)
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalBelowMinimum);
        if (feeAmount < 0 || feeAmount > amount) throw new ArgumentOutOfRangeException(nameof(feeAmount));

        UserId = userId;
        RequestCode = Check.NotNullOrWhiteSpace(requestCode, nameof(requestCode), 32).Trim().ToUpperInvariant();
        PayoutAccountId = payoutAccount.Id;
        Amount = amount;
        FeeAmount = feeAmount;
        NetAmount = amount - feeAmount;
        Status = WithdrawalRequestStatus.Pending;
        BankCode = payoutAccount.BankCode;
        AccountNumber = payoutAccount.AccountNumber;
        AccountHolderName = payoutAccount.AccountHolderName;
    }

    public void MarkPaid(Guid adminUserId, string paymentReference, DateTime paidAt, string? adminNote)
    {
        EnsurePending();
        Status = WithdrawalRequestStatus.Paid;
        ProcessedAt = paidAt;
        ProcessedByUserId = adminUserId;
        PaymentReference = Check.NotNullOrWhiteSpace(paymentReference, nameof(paymentReference), 128).Trim();
        AdminNote = NormalizeOptional(adminNote, 1000);
        RejectionReason = null;
    }

    public void Reject(Guid adminUserId, string reason, DateTime processedAt, string? adminNote)
    {
        EnsurePending();
        Status = WithdrawalRequestStatus.Rejected;
        ProcessedAt = processedAt;
        ProcessedByUserId = adminUserId;
        RejectionReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), 500).Trim();
        AdminNote = NormalizeOptional(adminNote, 1000);
        PaymentReference = null;
    }

    public void Cancel(Guid userId, DateTime cancelledAt)
    {
        EnsurePending();
        if (UserId != userId) throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalNotOwned);
        Status = WithdrawalRequestStatus.Cancelled;
        ProcessedAt = cancelledAt;
        ProcessedByUserId = userId;
        PaymentReference = null;
        RejectionReason = null;
    }

    private void EnsurePending()
    {
        if (Status != WithdrawalRequestStatus.Pending)
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new ArgumentException($"Value cannot exceed {maxLength} characters.", nameof(value));
        return normalized;
    }
}
