using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateOrderItem : FullAuditedEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public string ExternalItemId { get; private set; } = null!;
    public string ModelId { get; private set; } = string.Empty;
    public string? ProductName { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public int Quantity { get; private set; }
    public decimal ItemTotalCommission { get; private set; }
    public decimal AllocatedNetCommission { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal RefundAmount { get; private set; }
    public bool IsFraud { get; private set; }
    public string? ProviderStatus { get; private set; }

    protected AffiliateOrderItem() { }

    public AffiliateOrderItem(Guid id, Guid orderId, string externalItemId, string? modelId) : base(id)
    {
        OrderId = orderId;
        ExternalItemId = externalItemId;
        ModelId = modelId?.Trim() ?? string.Empty;
    }

    public void Update(string? name, decimal purchaseAmount, int quantity, decimal itemCommission,
        decimal allocatedNet, decimal userCommission, decimal refundAmount, bool isFraud, string? providerStatus)
    {
        ProductName = name;
        PurchaseAmount = purchaseAmount;
        Quantity = quantity;
        ItemTotalCommission = itemCommission;
        AllocatedNetCommission = allocatedNet;
        UserCommissionSnapshot = userCommission;
        RefundAmount = refundAmount;
        IsFraud = isFraud;
        ProviderStatus = providerStatus;
    }
}
