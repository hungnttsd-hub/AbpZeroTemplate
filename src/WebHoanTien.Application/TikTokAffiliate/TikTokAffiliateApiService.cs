using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
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
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly TikTokProductPreviewEnricher _preview;
    public TikTokAffiliateApiService(ITikTokCreatorConnection connection, IAffiliateLinkAppService links,
        IAffiliateOrderAppService orders, ITikTokCreatorApi api, IRepository<AffiliateTracking, Guid> trackings,
        TikTokProductPreviewEnricher preview)
    { _connection = connection; _links = links; _orders = orders; _api = api; _trackings = trackings; _preview = preview; }
    public Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo() => Task.FromResult(
        new TikTokAffiliateIntegrationDto(_connection.IsConfigured));
    public Task<TikTokCreatorDto?> GetCreatorProfile() => _connection.GetProfile();

    public async Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks()
    {
        if (CurrentTenant.Id.HasValue) throw new UserFriendlyException("Trang TikTok này dùng Creator của hệ thống CatBack.");
        var creatorOpenId = await _api.GetCreatorOpenIdAsync();
        // Creator library includes every CatBack owner and hidden link, without the personal list's 100-row limit.
        var links = (await _trackings.GetListAsync(x => x.Platform == AffiliatePlatform.TikTok))
            .Where(x => x.GetProperty<string>(TikTokLinkAttribution.SourceProperty) == TikTokLinkAttribution.Source
                && x.GetProperty<string>(TikTokLinkAttribution.CreatorProperty) == creatorOpenId)
            .OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id).ToList();
        var refreshed = await _preview.EnrichAsync(links);
        if (refreshed.Count > 0) await _trackings.UpdateManyAsync(refreshed, autoSave: true);
        return links.Select(MapCreatorLink).ToList();
    }

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
            x.ProductPriceMaximum, x.CreatorCommissionMaximum, x.EstimatedCommission, x.EstimatedCommissionMaximum), !x.IsHidden);

    private TikTokGeneratedLinkDto MapCreatorLink(AffiliateTracking x)
    {
        var ownsLink = x.UserId == CurrentUser.GetId();
        var sharingLink = x.Status == AffiliateTrackingStatus.Active ? "/go/" + x.TrackingToken : string.Empty;
        var status = x.IsHidden ? "HIDDEN" : x.Status == AffiliateTrackingStatus.Active ? "CREATED"
            : x.Status == AffiliateTrackingStatus.Failed ? "FAILED" : "DISABLED";
        return new(x.ProductId ?? "", x.ProductName ?? "Sản phẩm TikTok Shop", sharingLink,
            new DateTimeOffset(DateTime.SpecifyKind(x.CreationTime, DateTimeKind.Utc)), status, x.Id, sharingLink,
            new TikTokProductDto(x.ProductId ?? "", x.ProductName ?? "Sản phẩm TikTok Shop", "TikTok Shop",
                x.GetProperty<decimal?>(TikTokProductPreview.PriceMinimum), "VND", null, x.ImageUrl ?? "", "TikTokCreatorApi",
                x.GetProperty<decimal?>(TikTokProductPreview.CommissionMinimum), x.GetProperty<decimal?>(TikTokProductPreview.PriceMaximum),
                x.GetProperty<decimal?>(TikTokProductPreview.CommissionMaximum), ownsLink ? x.EstimatedCommission : null,
                ownsLink ? x.GetProperty<decimal?>(TikTokProductPreview.CashbackMaximum) : null), ownsLink && !x.IsHidden);
    }
}
