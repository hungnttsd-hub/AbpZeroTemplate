using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.Policy)]
[RemoteService(false)]
public class TikTokAffiliateMockService : WebHoanTienAppService, ITikTokAffiliateService
{
    private const string ProductId = "1736327643619493458";
    private static readonly TikTokCreatorDto Creator = new("demo_creator_vn_001", "catback_demo",
        "CatBack Demo Creator", "VN", "CONNECTED");
    private static readonly TikTokProductDto Product = new(ProductId, "Crocs Classic Clog",
        "Demo Official Store", 1290000m, "VND", 0.08m, "/demo/tiktok-product-01.jpg");
    private static readonly IReadOnlyList<TikTokAffiliateOrderDto> Orders = Array.AsReadOnly(new[]
    {
        new TikTokAffiliateOrderDto("TK100001", "Crocs Classic", 1290000m, 103200m, "VND", "Pending",
            new DateTimeOffset(2026, 10, 5, 15, 20, 0, TimeSpan.FromHours(7))),
        new TikTokAffiliateOrderDto("TK100002", "Nike Air Max", 2190000m, 175200m, "VND", "Settled",
            new DateTimeOffset(2026, 10, 4, 10, 15, 0, TimeSpan.FromHours(7))),
        new TikTokAffiliateOrderDto("TK100003", "New Balance 530", 1890000m, 151200m, "VND", "Cancelled",
            new DateTimeOffset(2026, 10, 3, 9, 40, 0, TimeSpan.FromHours(7)))
    });
    private readonly TikTokAffiliateDemoStore _store;

    public TikTokAffiliateMockService(TikTokAffiliateDemoStore store) => _store = store;

    private TikTokAffiliateDemoStore.DemoState State =>
        _store.Get(CurrentTenant.Id, CurrentUser.Id ?? throw new UserFriendlyException("Vui lòng đăng nhập CatBack."));

    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto("Mock", true, $"https://shop.tiktok.com/vn/pdp/{ProductId}"));

    public Task<TikTokCreatorDto?> GetCreatorProfile()
    {
        var state = State;
        lock (state.Sync) return Task.FromResult(state.Connected ? Creator : null);
    }

    public async Task<TikTokCreatorDto> ConnectCreator()
    {
        await Task.Delay(450);
        var state = State;
        lock (state.Sync) state.Connected = true;
        return Creator;
    }

    public async Task<TikTokProductDto> GetProduct(string productUrl)
    {
        var state = State;
        lock (state.Sync) RequireConnected(state);
        if (string.IsNullOrWhiteSpace(productUrl) || productUrl.Length > 2048 ||
            !Uri.TryCreate(productUrl.Trim(), UriKind.Absolute, out var url) ||
            url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo) ||
            !url.IdnHost.Equals("shop.tiktok.com", StringComparison.OrdinalIgnoreCase) ||
            !(url.AbsolutePath.TrimEnd('/') == $"/vn/pdp/{ProductId}" ||
              url.AbsolutePath.TrimEnd('/') == $"/view/product/{ProductId}"))
            throw new UserFriendlyException("Review Demo chỉ hỗ trợ sản phẩm mẫu. Chọn ‘Use demo product’ để dùng URL hợp lệ.");
        await Task.Delay(450);
        lock (state.Sync) state.ProductChecked = true;
        return Product;
    }

    public async Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId)
    {
        var state = State;
        lock (state.Sync)
        {
            RequireConnected(state);
            if (productId != ProductId || !state.ProductChecked)
                throw new UserFriendlyException("Hãy kiểm tra sản phẩm trước khi tạo affiliate link.");
        }
        await Task.Delay(900);
        lock (state.Sync)
        {
            // Repeated clicks are idempotent for this single-product review fixture.
            var existing = state.Links.FirstOrDefault(link => link.ProductId == productId);
            if (existing is not null) return existing;
            var link = new TikTokGeneratedLinkDto(ProductId, Product.Title,
                $"https://shop.tiktok.com/view/product/{ProductId}?affiliate_id=demo",
                DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)), "Generated");
            state.Links.Insert(0, link);
            return link;
        }
    }

    public Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks()
    {
        var state = State;
        lock (state.Sync) return Task.FromResult<IReadOnlyList<TikTokGeneratedLinkDto>>(state.Links.ToArray());
    }

    public Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders()
    {
        var state = State;
        lock (state.Sync) return Task.FromResult(state.Connected ? Orders : (IReadOnlyList<TikTokAffiliateOrderDto>)Array.Empty<TikTokAffiliateOrderDto>());
    }

    private static void RequireConnected(TikTokAffiliateDemoStore.DemoState state)
    {
        if (!state.Connected) throw new UserFriendlyException("Hãy kết nối TikTok Creator trước.");
    }
}
