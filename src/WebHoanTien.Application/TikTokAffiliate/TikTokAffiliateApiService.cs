using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.Policy)]
[RemoteService(false)]
public class TikTokAffiliateApiService : WebHoanTienAppService, ITikTokAffiliateService
{
    private readonly ITikTokCreatorConnection _connection;
    private readonly IAffiliateLinkAppService _links;
    private readonly IAffiliateOrderAppService _orders;
    public TikTokAffiliateApiService(ITikTokCreatorConnection connection, IAffiliateLinkAppService links, IAffiliateOrderAppService orders)
    { _connection = connection; _links = links; _orders = orders; }
    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto("Api", false, null, _connection.IsConfigured));
    public Task<TikTokCreatorDto?> GetCreatorProfile() => _connection.GetProfile();
    public Task<TikTokCreatorDto> ConnectCreator() => throw new UserFriendlyException("Creator chung được admin kết nối tại trang quản trị TikTok Creator.");
    public Task DisconnectCreator() => Task.CompletedTask;

    public async Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks() =>
        (await _links.GetListAsync(new AffiliateTrackingListInput { Platform = AffiliatePlatform.TikTok, MaxResultCount = 100 }))
        .Items.Where(x => x.Platform == AffiliatePlatform.TikTok).Select(Map).ToList();

    public async Task DeleteGeneratedLink(string trackingId)
    {
        if (!Guid.TryParse(trackingId, out var id) || (await _links.GetAsync(id)).Platform != AffiliatePlatform.TikTok)
            throw new UserFriendlyException("Không tìm thấy link TikTok thuộc tài khoản.");
        await _links.SetHiddenAsync(new SetAffiliateTrackingHiddenInput { Id = id, IsHidden = true });
    }

    public async Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId) => Map(await _links.CreateAsync(
        new CreateAffiliateLinkInput { Url = AsUrl(productId), TargetType = AffiliateLinkTargetType.Auto }));

    public Task<TikTokProductDto> GetProduct(string productUrl)
    {
        if (!TikTokAffiliateUrl.TryNormalize(AsUrl(productUrl), out _, out var id) || id is null)
            throw new UserFriendlyException("Vui lòng dùng link sản phẩm TikTok Shop có ID.");
        var metadata = TikTokProductOverrides.Find(id);
        return Task.FromResult(new TikTokProductDto(id, metadata?.Title ?? "Sản phẩm TikTok Shop", "TikTok Shop",
            metadata?.Price, "VND", null, metadata?.ImageUrl ?? "", "Metadata", null));
    }

    public async Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders() =>
        (await _orders.GetListAsync(new AffiliateOrderListInput { Platform = AffiliatePlatform.TikTok, MaxResultCount = 100 }))
        .Items.Select(x => new TikTokAffiliateOrderDto(x.ExternalOrderId, x.Items.FirstOrDefault()?.ProductName ?? "Sản phẩm TikTok Shop",
            x.PurchaseAmount, x.ExpectedUserCommission, "VND", x.Status.ToString(), new DateTimeOffset(DateTime.SpecifyKind(x.PurchaseTime, DateTimeKind.Utc)),
            EstimatedCommission: x.ExpectedUserCommission, ActualCommission: x.Status == AffiliateOrderStatus.Settled ? x.PayableUserCommission : null,
            SettledAt: x.SettledAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(x.SettledAt.Value, DateTimeKind.Utc)) : null, Source: "TikTokCreatorApi")).ToList();

    private static string AsUrl(string input) => input.Length is > 0 and <= 30 && input.All(char.IsAsciiDigit)
        ? TikTokProductUrlResolver.Canonical(input) : input;
    private static TikTokGeneratedLinkDto Map(AffiliateTrackingDto x) => new(x.ProductId ?? "", x.ProductName ?? "Sản phẩm TikTok Shop",
        x.RedirectUrl, new DateTimeOffset(DateTime.SpecifyKind(x.CreationTime, DateTimeKind.Utc)), "CREATED", x.Id, x.RedirectUrl);
}
