namespace WebHoanTien.TikTokAffiliate;

public static class TikTokAffiliateAccess
{
    // Keep the persisted key to preserve existing access; the UI label refers to TikTok Affiliate.
    public const string UserProperty = "IsTiktokDemo";
    public const string Policy = "CatBack.TikTokAffiliate.Access";
    public const string ConnectionPolicy = "CatBack.TikTokAffiliate.CreatorConnection";
    public const string OrdersPolicy = "CatBack.TikTokAffiliate.CreatorOrders";
    public const string OrderSyncPolicy = "CatBack.TikTokAffiliate.CreatorOrderSync";
    public const string OrderCreatorProperty = "TikTokOrderCreatorOpenId";
}
