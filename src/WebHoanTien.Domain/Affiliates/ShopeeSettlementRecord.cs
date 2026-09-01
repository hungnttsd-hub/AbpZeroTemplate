using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class ShopeeSettlementRecord : CreationAuditedAggregateRoot<Guid>
{
    public Guid BatchId { get; private set; }
    public Guid BillId { get; private set; }
    public string ExternalOrderId { get; private set; } = null!;
    public decimal EligibleCommission { get; private set; }
    public decimal AllocatedServiceFee { get; private set; }
    public decimal AllocatedTax { get; private set; }
    public decimal ActualPaidCommission { get; private set; }
    public decimal ApprovedUserCommission { get; private set; }
    public ShopeeSettlementRecordStatus Status { get; private set; }
    public Guid? AffiliateOrderId { get; private set; }
    public Guid? AffiliateConversionId { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? Issue { get; private set; }

    protected ShopeeSettlementRecord() { }

    public ShopeeSettlementRecord(Guid id, Guid batchId, Guid billId, string externalOrderId,
        decimal eligibleCommission, decimal allocatedServiceFee, decimal allocatedTax,
        decimal actualPaidCommission) : base(id)
    {
        BatchId = batchId;
        BillId = billId;
        ExternalOrderId = externalOrderId.Trim();
        EligibleCommission = eligibleCommission;
        AllocatedServiceFee = allocatedServiceFee;
        AllocatedTax = allocatedTax;
        ActualPaidCommission = actualPaidCommission;
        Status = ShopeeSettlementRecordStatus.Unmatched;
    }

    public void SetPendingApproval(Guid orderId, Guid conversionId, Guid? userId)
    {
        AffiliateOrderId = orderId;
        AffiliateConversionId = conversionId;
        UserId = userId;
        Status = ShopeeSettlementRecordStatus.PendingApproval;
        Issue = null;
    }

    public void SetUnmatched(string issue)
    {
        Status = ShopeeSettlementRecordStatus.Unmatched;
        Issue = issue;
    }

    public void SetAlreadySettled(Guid orderId, Guid conversionId, Guid? userId)
    {
        AffiliateOrderId = orderId;
        AffiliateConversionId = conversionId;
        UserId = userId;
        Status = ShopeeSettlementRecordStatus.AlreadySettled;
        Issue = "Đơn hàng đã được ghi nhận thanh toán trước đó.";
    }

    public void SetAwaitingShopeePayment(Guid? orderId, Guid? conversionId, Guid? userId, string issue)
    {
        AffiliateOrderId = orderId;
        AffiliateConversionId = conversionId;
        UserId = userId;
        Status = ShopeeSettlementRecordStatus.AwaitingShopeePayment;
        Issue = issue;
    }

    public void UpdateAmounts(decimal eligibleCommission, decimal allocatedServiceFee,
        decimal allocatedTax, decimal actualPaidCommission)
    {
        if (Status == ShopeeSettlementRecordStatus.Approved) return;
        EligibleCommission = eligibleCommission;
        AllocatedServiceFee = allocatedServiceFee;
        AllocatedTax = allocatedTax;
        ActualPaidCommission = actualPaidCommission;
    }

    public void SetInvalid(Guid orderId, Guid conversionId, Guid? userId, string issue)
    {
        AffiliateOrderId = orderId;
        AffiliateConversionId = conversionId;
        UserId = userId;
        SetInvalid(issue);
    }

    public void SetInvalid(string issue)
    {
        Status = ShopeeSettlementRecordStatus.Invalid;
        Issue = issue;
    }

    public void Approve(Guid approvedByUserId, DateTime approvedAt, decimal approvedUserCommission)
    {
        Status = ShopeeSettlementRecordStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        ApprovedUserCommission = approvedUserCommission;
        Issue = null;
    }
}
