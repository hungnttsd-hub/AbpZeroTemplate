using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

// Provider ledger. Commission here is the Creator's amount, not a customer wallet credit.
public class RioHubOrderSku : AuditedAggregateRoot<Guid>
{
    public string CreatorUsername { get; private set; } = string.Empty;
    public string OrderId { get; private set; } = string.Empty;
    public string SkuId { get; private set; } = string.Empty;
    public string? ProductId { get; private set; }
    public string? ProductName { get; private set; }
    public string? SubId { get; private set; }
    public string? TraceId { get; private set; }
    public string? TraceType { get; private set; }
    public int Status { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal EstimatedCommission { get; private set; }
    public decimal? ActualCommission { get; private set; }
    public int Quantity { get; private set; }
    public int RefundedQuantity { get; private set; }
    public bool FullyRefunded { get; private set; }
    public string? SettlementStatus { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProviderUpdatedAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public DateTime FetchedAtUtc { get; private set; }
    public string ProviderJson { get; private set; } = "{}";

    protected RioHubOrderSku() { }
    public RioHubOrderSku(Guid id, string creatorUsername, string orderId, string skuId) : base(id)
    { CreatorUsername = creatorUsername; OrderId = orderId; SkuId = skuId; }

    public bool Apply(string? productId, string? productName, string? subId, string? traceId, string? traceType,
        int status, string currency, decimal estimatedCommission, decimal? actualCommission,
        int quantity, int refundedQuantity, bool fullyRefunded, string? settlementStatus,
        DateTime createdAtUtc, DateTime? updatedAtUtc, DateTime? settledAtUtc, DateTime fetchedAtUtc, string providerJson)
    {
        if (status is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(status));
        if (ProviderUpdatedAtUtc.HasValue && updatedAtUtc.HasValue && updatedAtUtc < ProviderUpdatedAtUtc) return false;
        ProductId = productId; ProductName = productName; SubId = subId; TraceId = traceId; TraceType = traceType;
        Status = status; Currency = currency; EstimatedCommission = estimatedCommission; ActualCommission = actualCommission;
        Quantity = quantity; RefundedQuantity = refundedQuantity; FullyRefunded = fullyRefunded;
        SettlementStatus = settlementStatus; CreatedAtUtc = createdAtUtc;
        ProviderUpdatedAtUtc = updatedAtUtc ?? ProviderUpdatedAtUtc; SettledAtUtc = status == 3 ? null : settledAtUtc;
        FetchedAtUtc = fetchedAtUtc; ProviderJson = providerJson;
        return true;
    }
}
