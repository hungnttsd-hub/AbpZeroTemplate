using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.Policy)]
[RemoteService(false)]
public class TikTokAffiliateApiService : WebHoanTienAppService, ITikTokAffiliateService
{
    // The future adapter maps verified Creator API responses into these same DTOs.
    // No mock fallback, token exchange or outbound API calls in this review phase.
    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto("Api", false, null, IsAvailable: false));
    public Task<TikTokCreatorDto?> GetCreatorProfile() => Task.FromResult<TikTokCreatorDto?>(null);
    public Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks() =>
        Task.FromResult<IReadOnlyList<TikTokGeneratedLinkDto>>(Array.Empty<TikTokGeneratedLinkDto>());
    public Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders() =>
        Task.FromResult<IReadOnlyList<TikTokAffiliateOrderDto>>(Array.Empty<TikTokAffiliateOrderDto>());
    public Task<TikTokCreatorDto> ConnectCreator() => throw Unavailable();
    public Task<TikTokProductDto> GetProduct(string productUrl) => throw Unavailable();
    public Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId) => throw Unavailable();
    private static UserFriendlyException Unavailable() => new(
        "TikTok Creator API chưa được triển khai. Kết nối thật sẽ khả dụng sau khi hoàn tất quyền truy cập Creator và tích hợp API.");
}
