using AbpIoTemplateProject.Samples;
using Xunit;

namespace AbpIoTemplateProject.EntityFrameworkCore.Domains;

[Collection(AbpIoTemplateProjectTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AbpIoTemplateProjectEntityFrameworkCoreTestModule>
{

}
