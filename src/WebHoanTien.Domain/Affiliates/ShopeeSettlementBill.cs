using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class ShopeeSettlementBill : CreationAuditedAggregateRoot<Guid>
{
    public Guid BatchId { get; private set; }
    public string SourceAffiliateId { get; private set; } = null!;
    public string ValidationId { get; private set; } = null!;
    public string PayoutId { get; private set; } = null!;
    public DateTime? PaidAt { get; private set; }
    public DateTime? OrderCompletedFrom { get; private set; }
    public DateTime? OrderCompletedTo { get; private set; }
    public decimal EligibleCommission { get; private set; }
    public decimal AfterServiceFeeCommission { get; private set; }
    public decimal PaidCommission { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public bool HasAuthoritativeEligibleCommission { get; private set; }
    public int RecordCount { get; private set; }
    public int PaymentStatus { get; private set; }
    public int ValidationPayoutStatus { get; private set; }
    public int? OverallValidationStatus { get; private set; }
    public int? BillValidationStatus { get; private set; }
    public int? SettlementCycle { get; private set; }
    public bool HasAdjustment { get; private set; }
    public bool HasClawback { get; private set; }
    public bool IsCumulative { get; private set; }
    public bool HasBonus { get; private set; }
    public bool HasPpp { get; private set; }
    public bool IsShopeePaid => PaymentStatus == 4 && ValidationPayoutStatus == 2 &&
        !string.IsNullOrWhiteSpace(PayoutId) && PaidAt.HasValue &&
        !HasAdjustment && !HasClawback && !IsCumulative && !HasBonus && !HasPpp;

    protected ShopeeSettlementBill() { }

    public ShopeeSettlementBill(Guid id, Guid batchId, string sourceAffiliateId, string validationId,
        string payoutId, DateTime? paidAt, DateTime? orderCompletedFrom, DateTime? orderCompletedTo,
        decimal eligibleCommission, decimal afterServiceFeeCommission, decimal paidCommission,
        bool hasAuthoritativeEligibleCommission, int recordCount, int paymentStatus = 4,
        int validationPayoutStatus = 2, int? overallValidationStatus = null,
        int? billValidationStatus = null, int? settlementCycle = null, bool hasAdjustment = false,
        bool hasClawback = false, bool isCumulative = false, bool hasBonus = false, bool hasPpp = false) : base(id)
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
        SetShopeeState(paymentStatus, validationPayoutStatus, overallValidationStatus, billValidationStatus,
            settlementCycle, hasAdjustment, hasClawback, isCumulative, hasBonus, hasPpp);
    }

    public void UpdateFromImport(string payoutId, DateTime? paidAt, DateTime? orderCompletedFrom,
        DateTime? orderCompletedTo, decimal eligibleCommission, decimal afterServiceFeeCommission,
        decimal paidCommission, bool hasAuthoritativeEligibleCommission, int recordCount, int paymentStatus,
        int validationPayoutStatus, int? overallValidationStatus, int? billValidationStatus,
        int? settlementCycle, bool hasAdjustment, bool hasClawback, bool isCumulative, bool hasBonus,
        bool hasPpp)
    {
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
        SetShopeeState(paymentStatus, validationPayoutStatus, overallValidationStatus, billValidationStatus,
            settlementCycle, hasAdjustment, hasClawback, isCumulative, hasBonus, hasPpp);
    }

    private void SetShopeeState(int paymentStatus, int validationPayoutStatus,
        int? overallValidationStatus, int? billValidationStatus, int? settlementCycle,
        bool hasAdjustment, bool hasClawback, bool isCumulative, bool hasBonus, bool hasPpp)
    {
        PaymentStatus = paymentStatus;
        ValidationPayoutStatus = validationPayoutStatus;
        OverallValidationStatus = overallValidationStatus;
        BillValidationStatus = billValidationStatus;
        SettlementCycle = settlementCycle;
        HasAdjustment = hasAdjustment;
        HasClawback = hasClawback;
        IsCumulative = isCumulative;
        HasBonus = hasBonus;
        HasPpp = hasPpp;
    }
}
