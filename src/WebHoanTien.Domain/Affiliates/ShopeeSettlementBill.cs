using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class ShopeeSettlementBill : CreationAuditedAggregateRoot<Guid>
{
    public Guid BatchId { get; private set; }
    public string SourceAffiliateId { get; private set; } = null!;
    public string ValidationId { get; private set; } = null!;
    public string PayoutId { get; private set; } = null!;
    public DateTime PaidAt { get; private set; }
    public DateTime? OrderCompletedFrom { get; private set; }
    public DateTime? OrderCompletedTo { get; private set; }
    public decimal EligibleCommission { get; private set; }
    public decimal AfterServiceFeeCommission { get; private set; }
    public decimal PaidCommission { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public bool HasAuthoritativeEligibleCommission { get; private set; }
    public int RecordCount { get; private set; }

    protected ShopeeSettlementBill() { }

    public ShopeeSettlementBill(Guid id, Guid batchId, string sourceAffiliateId, string validationId,
        string payoutId, DateTime paidAt, DateTime? orderCompletedFrom, DateTime? orderCompletedTo,
        decimal eligibleCommission, decimal afterServiceFeeCommission, decimal paidCommission,
        bool hasAuthoritativeEligibleCommission, int recordCount) : base(id)
    {
        BatchId = batchId;
        SourceAffiliateId = sourceAffiliateId.Trim();
        ValidationId = validationId.Trim();
        PayoutId = payoutId.Trim();
        PaidAt = paidAt;
        OrderCompletedFrom = orderCompletedFrom;
        OrderCompletedTo = orderCompletedTo;
        EligibleCommission = eligibleCommission;
        AfterServiceFeeCommission = afterServiceFeeCommission;
        PaidCommission = paidCommission;
        ServiceFeeAmount = eligibleCommission - afterServiceFeeCommission;
        TaxAmount = afterServiceFeeCommission - paidCommission;
        HasAuthoritativeEligibleCommission = hasAuthoritativeEligibleCommission;
        RecordCount = recordCount;
    }
}
