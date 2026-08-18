using Volo.Abp.Modularity;

namespace WebHoanTien;

/* Inherit from this class for your domain layer tests. */
public abstract class WebHoanTienDomainTestBase<TStartupModule> : WebHoanTienTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
