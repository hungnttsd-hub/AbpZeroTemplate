using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using WebHoanTien.Integrations.Shopee;

namespace WebHoanTien.Affiliates;

public class AffiliateIdResolver : IAffiliateIdResolver, ITransientDependency
{
    private readonly IRepository<UserAffiliateIdOverride, Guid> _overrides;
    private readonly ShopeeAffiliateOptions _shopeeOptions;

    public AffiliateIdResolver(IRepository<UserAffiliateIdOverride, Guid> overrides,
        IOptions<ShopeeAffiliateOptions> shopeeOptions)
    {
        _overrides = overrides;
        _shopeeOptions = shopeeOptions.Value;
    }

    public async Task<ResolvedAffiliateId> ResolveAsync(Guid userId, AffiliatePlatform platform)
    {
        var userOverride = (await _overrides.GetListAsync(x => x.UserId == userId && x.Platform == platform))
            .SingleOrDefault();
        if (userOverride is not null)
            return new ResolvedAffiliateId(userOverride.AffiliateId, userOverride.Id);

        var configuredAffiliateId = platform == AffiliatePlatform.Shopee
            ? _shopeeOptions.AffiliateId
            : string.Empty;
        if (string.IsNullOrWhiteSpace(configuredAffiliateId))
            throw ProviderNotConfigured();

        try
        {
            return new ResolvedAffiliateId(AffiliateIdRules.Normalize(configuredAffiliateId), null);
        }
        catch (ArgumentException)
        {
            throw ProviderNotConfigured();
        }
    }

    private static UserFriendlyException ProviderNotConfigured() => new(
        "Chưa cấu hình Shopee Affiliate ID. Liên hệ quản trị viên để kiểm tra cấu hình affiliate.",
        code: WebHoanTienDomainErrorCodes.ProviderNotConfigured);
}
