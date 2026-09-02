using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateOrder : FullAuditedAggregateRoot<Guid>
{
    public Guid ConversionId { get; private set; }
    public AffiliatePlatform Platform { get; private set; }
    public string ExternalOrderId { get; private set; } = null!;
    public AffiliateOrderStatus Status { get; private set; }
    public string? ShopType { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public decimal NetCommission { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal PayableUserCommission { get; private set; }
    public decimal? SettledNetCommission { get; private set; }
    public decimal? SettledUserCommission { get; private set; }
    public string? SettlementReference { get; private set; }
    public DateTime? SettledAt { get; private set; }

    protected AffiliateOrder() { }

    public AffiliateOrder(Guid id, Guid conversionId, string externalOrderId,
        AffiliatePlatform platform = AffiliatePlatform.Shopee) : base(id)
    {
        ConversionId = conversionId;
        Platform = platform;
        ExternalOrderId = externalOrderId;
        Status = AffiliateOrderStatus.Unpaid;
    }

    public void Update(AffiliateOrderStatus status, string? shopType, decimal purchaseAmount, decimal netCommission, decimal userCommission)
    {
        if (Status == AffiliateOrderStatus.Settled && status is not AffiliateOrderStatus.Cancelled
            and not AffiliateOrderStatus.Refunded and not AffiliateOrderStatus.Rejected)
        {
            return;
        }

        ShopType = shopType;
        PurchaseAmount = purchaseAmount;
        NetCommission = netCommission;
        UserCommissionSnapshot = userCommission;

        Status = status;
        PayableUserCommission = 0m;
    }

    public void Settle(decimal settledNetCommission, decimal settledUserCommission, string settlementReference,
        DateTime settledAt)
    {
        if (Status == AffiliateOrderStatus.Settled) return;
        if (Status != AffiliateOrderStatus.Completed)
            throw new BusinessException(WebHoanTienDomainErrorCodes.AffiliateOrderSettlementInvalidState);
        if (settledNetCommission < 0m || settledUserCommission < 0m || settledUserCommission > settledNetCommission)
            throw new BusinessException(WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);

        SettledNetCommission = settledNetCommission;
        SettledUserCommission = settledUserCommission;
        SettlementReference = settlementReference.Trim();
        SettledAt = settledAt;
        PayableUserCommission = settledUserCommission;
        Status = AffiliateOrderStatus.Settled;
    }
}
