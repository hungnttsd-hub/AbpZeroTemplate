using Volo.Abp.Modularity;

namespace AbpIoTemplateProject;

[DependsOn(
    typeof(AbpIoTemplateProjectDomainModule),
    typeof(AbpIoTemplateProjectTestBaseModule)
)]
public class AbpIoTemplateProjectDomainTestModule : AbpModule
{

}
