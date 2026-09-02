using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateConversion : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public string ExternalConversionId { get; private set; } = null!;
    public Guid? TrackingId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AttributionValue { get; private set; }
    public DateTime PurchaseTime { get; private set; }
    public DateTime? ClickTime { get; private set; }
    public AffiliateConversionStatus Status { get; private set; }
    public decimal GrossCommission { get; private set; }
    public decimal NetCommission { get; private set; }
    public CommissionSource CommissionSource { get; private set; }
    public decimal UserShareRate { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal PlatformRevenueSnapshot { get; private set; }
    public decimal PayableUserCommission { get; private set; }
    public DateTime LastProviderUpdateAt { get; private set; }

    protected AffiliateConversion() { }

    public AffiliateConversion(Guid id, AffiliatePlatform platform, string externalId, DateTime purchaseTime)
        : base(id)
    {
        Platform = platform;
        ExternalConversionId = externalId;
        PurchaseTime = purchaseTime;
        Status = AffiliateConversionStatus.Estimated;
        LastProviderUpdateAt = purchaseTime;
    }

    public void MapTo(Guid trackingId, Guid userId, string? attributionValue)
    {
        TrackingId = trackingId;
        UserId = userId;
        AttributionValue = attributionValue;
    }

    public void ClearMapping()
    {
        TrackingId = null;
        UserId = null;
        AttributionValue = null;
    }

    public void SetClickTime(DateTime? value) => ClickTime = value;

    public void ApplyCommission(decimal gross, decimal net, CommissionSource source, decimal userShareRate)
    {
        GrossCommission = gross;
        NetCommission = net;
        CommissionSource = source;
        UserShareRate = userShareRate;
        UserCommissionSnapshot = decimal.Round(net * userShareRate / 100m, 0, MidpointRounding.AwayFromZero);
        PlatformRevenueSnapshot = net - UserCommissionSnapshot;
        RefreshPayable();
    }

    public void ApplyAttributedCommission(decimal gross, decimal net, CommissionSource source,
        decimal? soleUserShareRate, decimal aggregateUserCommission)
    {
        GrossCommission = gross;
        NetCommission = net;
        CommissionSource = source;
        UserShareRate = soleUserShareRate ?? 0m;
        UserCommissionSnapshot = Math.Max(0m, aggregateUserCommission);
        PlatformRevenueSnapshot = net - UserCommissionSnapshot;
        RefreshPayable();
    }

    public void ChangeStatus(AffiliateConversionStatus status, DateTime providerUpdateAt)
    {
        Status = status;
        LastProviderUpdateAt = providerUpdateAt;
        RefreshPayable();
    }

    private void RefreshPayable()
    {
        PayableUserCommission = Status is AffiliateConversionStatus.Cancelled
            or AffiliateConversionStatus.Refunded
            or AffiliateConversionStatus.Rejected
            ? 0m
            : UserCommissionSnapshot;
    }
}
