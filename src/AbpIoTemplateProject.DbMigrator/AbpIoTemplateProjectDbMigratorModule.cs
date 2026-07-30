using AbpIoTemplateProject.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpIoTemplateProject.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpIoTemplateProjectEntityFrameworkCoreModule),
    typeof(AbpIoTemplateProjectApplicationContractsModule)
    )]
public class AbpIoTemplateProjectDbMigratorModule : AbpModule
{
}
