using WebHoanTien.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace WebHoanTien.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(WebHoanTienEntityFrameworkCoreModule),
    typeof(WebHoanTienApplicationContractsModule)
    )]
public class WebHoanTienDbMigratorModule : AbpModule
{
}
