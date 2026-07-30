using Volo.Abp.Modularity;

namespace AbpIoTemplateProject;

[DependsOn(
    typeof(AbpIoTemplateProjectApplicationModule),
    typeof(AbpIoTemplateProjectDomainTestModule)
)]
public class AbpIoTemplateProjectApplicationTestModule : AbpModule
{

}
