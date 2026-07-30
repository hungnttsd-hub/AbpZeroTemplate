using Volo.Abp.Modularity;

namespace AbpIoTemplateProject;

/* Inherit from this class for your domain layer tests. */
public abstract class AbpIoTemplateProjectDomainTestBase<TStartupModule> : AbpIoTemplateProjectTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
