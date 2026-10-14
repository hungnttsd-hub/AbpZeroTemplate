using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.TikTokAffiliate;

public interface ITikTokAffiliateService : IApplicationService
{
    Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo();
    Task<TikTokCreatorDto?> GetCreatorProfile();
    Task<TikTokProductDto> GetProduct(string productUrl);
    Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId);
    Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks();
    Task DeleteGeneratedLink(string productId);
    Task<PagedResultDto<AffiliateOrderDto>> SearchAffiliateOrders(AffiliateOrderListInput input);
}

public sealed record TikTokAffiliateIntegrationDto(bool IsAvailable);
public sealed record TikTokCreatorDto(string CreatorId, string Username, string DisplayName,
    string Market, string Status);
public sealed record TikTokProductDto(string ProductId, string Title, string ShopName, decimal? Price,
    string Currency, decimal? CommissionRate, string ImageUrl, string? CommissionSource = null,
    decimal? CommissionAmount = null, decimal? PriceMaximum = null, decimal? CommissionMaximum = null,
    decimal? EstimatedCashback = null, decimal? EstimatedCashbackMaximum = null);
public sealed record TikTokGeneratedLinkDto(string ProductId, string ProductTitle, string SharingLink,
    DateTimeOffset CreatedAt, string Status, Guid? TrackingId = null, string? DirectLink = null,
    TikTokProductDto? Product = null, bool CanHide = false);
