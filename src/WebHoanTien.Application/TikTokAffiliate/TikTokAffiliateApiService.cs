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
    private readonly ITikTokCreatorConnection _connection;
    public TikTokAffiliateApiService(ITikTokCreatorConnection connection) => _connection = connection;
    // Only authorization/profile are implemented. Product/link/orders remain unavailable.
    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto("Api", false, null, IsAvailable: false));
    public Task<TikTokCreatorDto?> GetCreatorProfile() => _connection.GetProfile();
    public Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks() =>
        Task.FromResult<IReadOnlyList<TikTokGeneratedLinkDto>>(Array.Empty<TikTokGeneratedLinkDto>());
    public Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders() =>
        Task.FromResult<IReadOnlyList<TikTokAffiliateOrderDto>>(Array.Empty<TikTokAffiliateOrderDto>());
    public Task<TikTokCreatorDto> ConnectCreator() => throw Unavailable();
    public Task DisconnectCreator() => _connection.Disconnect();
    public Task DeleteGeneratedLink(string productId) => throw Unavailable();
    public Task<TikTokProductDto> GetProduct(string productUrl) => throw Unavailable();
    public Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId) => throw Unavailable();
    private static UserFriendlyException Unavailable() => new(
        "TikTok Creator API chưa được triển khai. Kết nối thật sẽ khả dụng sau khi hoàn tất quyền truy cập Creator và tích hợp API.");
}
