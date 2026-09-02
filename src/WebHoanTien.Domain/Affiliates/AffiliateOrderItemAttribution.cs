using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateOrderItemAttribution : FullAuditedEntity<Guid>
{
    public Guid OrderItemId { get; private set; }
    public string AttributionValue { get; private set; } = null!;
    public Guid? TrackingId { get; private set; }
    public Guid? UserId { get; private set; }
    public AffiliateAttributionStatus Status { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public int Quantity { get; private set; }
    public decimal ItemTotalCommission { get; private set; }
    public decimal AllocatedNetCommission { get; private set; }
    public decimal UserShareRate { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal? SettledNetCommission { get; private set; }
    public decimal? SettledUserCommission { get; private set; }
    public decimal RefundAmount { get; private set; }
    public bool IsFraud { get; private set; }
    public string? ProviderStatus { get; private set; }

    protected AffiliateOrderItemAttribution()
    {
    }

    public AffiliateOrderItemAttribution(Guid id, Guid orderItemId, string attributionValue) : base(id)
    {
        OrderItemId = orderItemId;
        AttributionValue = attributionValue.Trim();
        Status = AffiliateAttributionStatus.Unmatched;
    }

    public void UpdateSource(decimal purchaseAmount, int quantity, decimal itemTotalCommission,
        decimal allocatedNetCommission, decimal refundAmount, bool isFraud, string? providerStatus)
    {
        PurchaseAmount = purchaseAmount;
        Quantity = quantity;
        ItemTotalCommission = itemTotalCommission;
        AllocatedNetCommission = allocatedNetCommission;
        RefundAmount = refundAmount;
        IsFraud = isFraud;
        ProviderStatus = providerStatus;
    }

    public void Match(Guid trackingId, Guid userId, decimal userShareRate, decimal userCommission)
    {
        TrackingId = trackingId;
        UserId = userId;
        UserShareRate = userShareRate;
        UserCommissionSnapshot = userCommission;
        Status = AffiliateAttributionStatus.Matched;
    }

    public void MarkUnmatched()
    {
        TrackingId = null;
        UserId = null;
        UserShareRate = 0m;
        UserCommissionSnapshot = 0m;
        Status = AffiliateAttributionStatus.Unmatched;
    }

    public void MarkConflict() => Status = AffiliateAttributionStatus.Conflict;

    public void Settle(decimal settledNetCommission, decimal settledUserCommission)
    {
        if (SettledNetCommission.HasValue || SettledUserCommission.HasValue) return;
        SettledNetCommission = settledNetCommission;
        SettledUserCommission = settledUserCommission;
    }
}
