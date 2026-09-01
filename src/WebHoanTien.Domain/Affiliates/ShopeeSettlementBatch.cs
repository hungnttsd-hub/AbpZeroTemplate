using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class ShopeeSettlementBatch : CreationAuditedAggregateRoot<Guid>
{
    public ShopeeSettlementImportSource Source { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string ContentHash { get; private set; } = null!;
    public ShopeeSettlementBatchStatus Status { get; private set; }
    public int BillCount { get; private set; }
    public int RecordCount { get; private set; }
    public int PendingCount { get; private set; }
    public int ApprovedCount { get; private set; }
    public int UnmatchedCount { get; private set; }
    public int AlreadySettledCount { get; private set; }
    public int InvalidCount { get; private set; }
    public int WaitingPaymentCount { get; private set; }
    public decimal TotalEligibleCommission { get; private set; }
    public decimal TotalPaidCommission { get; private set; }
    public decimal PendingPaidCommission { get; private set; }
    public decimal ApprovedPaidCommission { get; private set; }

    protected ShopeeSettlementBatch() { }

    public ShopeeSettlementBatch(Guid id, ShopeeSettlementImportSource source, string originalFileName,
        string contentHash) : base(id)
    {
        Source = source;
        OriginalFileName = originalFileName.Trim();
        ContentHash = contentHash.Trim().ToLowerInvariant();
        Status = ShopeeSettlementBatchStatus.PendingApproval;
    }

    public void UpdateSummary(int billCount, int recordCount, int pendingCount, int approvedCount,
        int unmatchedCount, int alreadySettledCount, int invalidCount, int waitingPaymentCount,
        decimal totalEligibleCommission,
        decimal totalPaidCommission, decimal pendingPaidCommission, decimal approvedPaidCommission)
    {
        BillCount = billCount;
        RecordCount = recordCount;
        PendingCount = pendingCount;
        ApprovedCount = approvedCount;
        UnmatchedCount = unmatchedCount;
        AlreadySettledCount = alreadySettledCount;
        InvalidCount = invalidCount;
        WaitingPaymentCount = waitingPaymentCount;
        TotalEligibleCommission = totalEligibleCommission;
        TotalPaidCommission = totalPaidCommission;
        PendingPaidCommission = pendingPaidCommission;
        ApprovedPaidCommission = approvedPaidCommission;

        var hasIssues = unmatchedCount > 0 || alreadySettledCount > 0 || invalidCount > 0;
        Status = pendingCount > 0
            ? approvedCount > 0
                ? ShopeeSettlementBatchStatus.PartiallyApproved
                : ShopeeSettlementBatchStatus.PendingApproval
            : waitingPaymentCount > 0 && approvedCount == 0 && !hasIssues
                ? ShopeeSettlementBatchStatus.WaitingForShopee
            : hasIssues || waitingPaymentCount > 0
                ? ShopeeSettlementBatchStatus.CompletedWithIssues
                : ShopeeSettlementBatchStatus.Approved;
    }
}
