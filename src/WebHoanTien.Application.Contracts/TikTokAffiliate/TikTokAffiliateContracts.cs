using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.TikTokAffiliate;

public interface ITikTokAffiliateService : IApplicationService
{
    Task<TikTokAffiliateIntegrationDto> GetIntegrationInfo();
    Task<TikTokCreatorDto?> GetCreatorProfile();
    Task<TikTokCreatorDto> ConnectCreator();
    Task DisconnectCreator();
    Task<TikTokProductDto> GetProduct(string productUrl);
    Task<TikTokGeneratedLinkDto> GenerateAffiliateLink(string productId);
    Task<IReadOnlyList<TikTokGeneratedLinkDto>> GetGeneratedLinks();
    Task DeleteGeneratedLink(string productId);
    Task<IReadOnlyList<TikTokAffiliateOrderDto>> SearchAffiliateOrders();
}

public sealed record TikTokAffiliateIntegrationDto(string Mode, bool IsDemo, string? SampleProductUrl,
    bool IsAvailable = true);
public sealed record TikTokCreatorDto(string CreatorId, string Username, string DisplayName,
    string Market, string Status);
public sealed record TikTokProductDto(string ProductId, string Title, string ShopName, decimal Price,
    string Currency, decimal CommissionRate, string ImageUrl);
public sealed record TikTokGeneratedLinkDto(string ProductId, string ProductTitle, string SharingLink,
    DateTimeOffset CreatedAt, string Status);
public sealed record TikTokAffiliateOrderDto(string OrderId, string Product, decimal OrderAmount,
    decimal AffiliateCommission, string Currency, string Status, DateTimeOffset CreatedAt);
