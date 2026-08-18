using Volo.Abp.Modularity;

namespace WebHoanTien;

public abstract class WebHoanTienApplicationTestBase<TStartupModule> : WebHoanTienTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
