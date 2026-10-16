namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokOrderSyncOptions
{
    public const string SectionName = "TikTokShop:OrderSync";
    public bool Enabled { get; set; } = true;
    public int LookbackDays { get; set; } = 90;
}
