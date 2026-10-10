namespace WebHoanTien.Affiliates;

public sealed record TikTokProductOverride(string ProductId, string Title, string ImageUrl,
    decimal Price, decimal CommissionAmount);

public static class TikTokProductOverrides
{
    public const string SunSetProductId = "1732226714601031550";
    public const string ProductUrlFallbackProperty = "TikTokProductUrlFallback";
    private static readonly TikTokProductOverride SunSet = new(
        SunSetProductId,
        "Cây Sun Set Thủy Sinh – gắn sẵn giá thể _Không Cần CO₂",
        "/products/tiktok/tiktok-product-1732226714601031550.png",
        36100m, 3600m);

    // Overrides describe a product; order commissions still come from RioHub reconciliation.
    public static TikTokProductOverride? Find(string? productId) =>
        productId == SunSet.ProductId ? SunSet : null;
}
