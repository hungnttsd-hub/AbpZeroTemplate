using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

// Infrastructure boundary; all token handling remains in the Web adapter.
public interface ITikTokCreatorApi
{
    Task<string> GetCreatorOpenIdAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TikTokCreatorProduct>> GetProductsAsync(IReadOnlyCollection<string> productIds, CancellationToken ct = default);
    Task<TikTokSharingLink> GenerateLinkAsync(string productId, CancellationToken ct = default);
    Task<TikTokCreatorOrderPage> SearchOrdersAsync(long fromUnix, long toUnix, string? pageToken, CancellationToken ct = default);
}

public sealed record TikTokSharingLink(string SharingLink, string? DeepLink, string? OneLink, string CreatorOpenId);
public sealed record TikTokCreatorOrderPage(JsonElement[] Orders, string? NextPageToken, string CreatorOpenId);
public sealed record TikTokCreatorProduct(string ProductId, string CreatorOpenId, string? Title, string? ShopName,
    string? ImageUrl, decimal? MinimumPrice, decimal? MaximumPrice,
    decimal? MinimumCommission, decimal? MaximumCommission);
