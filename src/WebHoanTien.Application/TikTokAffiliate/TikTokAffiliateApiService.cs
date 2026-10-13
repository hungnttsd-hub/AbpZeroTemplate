using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(TikTokAffiliateAccess.Policy)]
[RemoteService(false)]
public class TikTokAffiliateApiService : WebHoanTienAppService, ITikTokAffiliateService
{
    private readonly ITikTokCreatorConnection _connection;
    private readonly IAffiliateLinkAppService _links;
    private readonly IAffiliateOrderAppService _orders;
    private readonly ITikTokCreatorApi _api;
    public TikTokAffiliateApiService(ITikTokCreatorConnection connection, IAffiliateLinkAppService links,
        IAffiliateOrderAppService orders, ITikTokCreatorApi api)
    { _connection = connection; _links = links; _orders = orders; _api = api; }
    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto(_connection.IsConfigured));
    public Task<TikTokCreatorDto?> GetCreatorProfile() => _connection.GetProfile();

    public async Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks() =>
        (await _links.GetListAsync(new AffiliateTrackingListInput { Platform = AffiliatePlatform.TikTok, MaxResultCount = 100 }))
        .Items.Where(x => x.Platform == AffiliatePlatform.TikTok && x.ProviderSource == TikTokLinkAttribution.Source).Select(Map).ToList();

    public async Task DeleteGeneratedLink(string trackingId)
    {
        if (!Guid.TryParse(trackingId, out var id))
            throw new UserFriendlyException("Không tìm thấy link TikTok thuộc tài khoản.");
        var link = await _links.GetAsync(id);
        if (link.Platform != AffiliatePlatform.TikTok || link.ProviderSource != TikTokLinkAttribution.Source)
            throw new UserFriendlyException("Không tìm thấy link TikTok thuộc tài khoản.");
        await _links.SetHiddenAsync(new SetAffiliateTrackingHiddenInput { Id = id, IsHidden = true });
    }

    public async Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId)
    {
        var url = AsUrl(productId);
        if (!TikTokAffiliateUrl.TryNormalizeProductInput(url, out _, out _))
            throw new UserFriendlyException("Vui lòng nhập link hoặc ID sản phẩm TikTok Shop hợp lệ.");
        return Map(await _links.CreateAsync(new CreateAffiliateLinkInput { Url = url, TargetType = AffiliateLinkTargetType.Product }));
    }

    public async Task<TikTokProductDto> GetProduct(string productUrl)
    {
        if (!TikTokAffiliateUrl.TryNormalizeProductInput(AsUrl(productUrl), out _, out var id) || id is null)
            throw new UserFriendlyException("Vui lòng dùng link sản phẩm TikTok Shop có ID.");
        var product = (await _api.GetProductsAsync(new[] { id })).FirstOrDefault();
        if (product is null) throw new UserFriendlyException("TikTok chưa trả thông tin open collaboration của sản phẩm này.");
        return new TikTokProductDto(id, product.Title ?? "Sản phẩm TikTok Shop", product.ShopName ?? "TikTok Shop",
            product.MinimumPrice, "VND", null, product.ImageUrl ?? "", "TikTokCreatorApi", product.MinimumCommission,
            product.MaximumPrice, product.MaximumCommission);
    }

    public Task<PagedResultDto<AffiliateOrderDto>> SearchAffiliateOrders(AffiliateOrderListInput input) =>
        _orders.GetListAsync(new AffiliateOrderListInput
        {
            Platform = AffiliatePlatform.TikTok, Status = input.Status, StatusGroup = input.StatusGroup,
            From = input.From, To = input.To, SkipCount = input.SkipCount, MaxResultCount = input.MaxResultCount
        });

    private static string AsUrl(string? input)
    {
        var value = input?.Trim() ?? string.Empty;
        return value.Length is > 0 and <= 30 && value.All(char.IsAsciiDigit)
            ? TikTokProductUrlResolver.Canonical(value) : value;
    }
    private static TikTokGeneratedLinkDto Map(AffiliateTrackingDto x) => new(x.ProductId ?? "", x.ProductName ?? "Sản phẩm TikTok Shop",
        x.RedirectUrl, new DateTimeOffset(DateTime.SpecifyKind(x.CreationTime, DateTimeKind.Utc)), "CREATED", x.Id, x.RedirectUrl,
        new TikTokProductDto(x.ProductId ?? "", x.ProductName ?? "Sản phẩm TikTok Shop", "TikTok Shop",
            x.ProductPrice, "VND", null, x.ImageUrl ?? "", "TikTokCreatorApi", x.CreatorCommissionAmount,
            x.ProductPriceMaximum, x.CreatorCommissionMaximum, x.EstimatedCommission, x.EstimatedCommissionMaximum));
}
