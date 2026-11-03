namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokAutoSettlementResult
{
    public int ConsideredCount { get; set; }
    public int ApprovedCount { get; set; }
    public int AlreadySettledCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal CreditedAmount { get; set; }
}
