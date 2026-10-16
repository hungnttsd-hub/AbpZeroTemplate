using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.TikTokAffiliate;

public enum TikTokSettlementAmountSource { ActualCommission = 1, ManualGross = 2 }

public sealed class TikTokSettlementInput
{
    [Range(1, 2)] public TikTokSettlementAmountSource Source { get; set; } = TikTokSettlementAmountSource.ManualGross;
    [Range(typeof(decimal), "0", "1000000000000")] public decimal? GrossCommission { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal TaxPercent { get; set; } = 10m;
    [StringLength(1000)] public string? Reason { get; set; }
}

public sealed class TikTokSettlementPreview
{
    public Guid OrderId { get; set; }
    public string ExternalOrderId { get; set; } = "";
    public string? CreatorOpenId { get; set; }
    public string? ProviderStatus { get; set; }
    public string OrderStatus { get; set; } = "";
    public DateTime? LastCheckedAt { get; set; }
    public bool AlreadySettled { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovalReason { get; set; }
    public string? SettlementReference { get; set; }
    public decimal? ActualCommission { get; set; }
    public decimal? EstimatedCommission { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetCommission { get; set; }
    public decimal UserCommission { get; set; }
    public decimal UnmatchedNetCommission { get; set; }
    public string PlanHash { get; set; } = "";
    public TikTokSettlementInput Input { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<TikTokSettlementRecipient> Recipients { get; set; } = new();
    public bool CanApprove => !AlreadySettled && Issues.Count == 0;
}

public sealed record TikTokSettlementRecipient(Guid UserId, string? UserName, decimal NetCommission, decimal UserCommission);
public sealed record TikTokSettlementApprovalResult(bool AlreadySettled, decimal CreditedAmount);

public interface IAdminTikTokSettlementAppService : IApplicationService
{
    Task<TikTokSettlementPreview> PreviewAsync(Guid orderId, TikTokSettlementInput? input = null);
    Task<TikTokSettlementApprovalResult> ApproveAsync(Guid orderId, TikTokSettlementInput input, string expectedPlanHash);
}

// Reads the registered cashback Creator identity without refreshing tokens or calling TikTok.
public interface ITikTokCashbackCreatorRegistry
{
    Task<string?> GetOpenIdAsync(CancellationToken ct = default);
}
