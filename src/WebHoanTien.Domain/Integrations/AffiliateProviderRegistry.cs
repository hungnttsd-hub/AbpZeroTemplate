using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Integrations;

public class AffiliateProviderRegistry : IAffiliateProviderRegistry, ITransientDependency
{
    private readonly IReadOnlyDictionary<AffiliatePlatform, IAffiliateProvider> _providers;

    public AffiliateProviderRegistry(IEnumerable<IAffiliateProvider> providers)
    {
        _providers = providers.GroupBy(x => x.Platform).ToDictionary(x => x.Key, x => x.Last());
    }

    public IAffiliateProvider Get(AffiliatePlatform platform)
    {
        if (_providers.TryGetValue(platform, out var provider)) return provider;
        throw new BusinessException(WebHoanTienDomainErrorCodes.ProviderNotConfigured)
            .WithData("Platform", platform);
    }
}
