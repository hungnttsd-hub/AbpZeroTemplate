using AbpIoTemplateProject.Samples;
using Xunit;

namespace AbpIoTemplateProject.EntityFrameworkCore.Applications;

[Collection(AbpIoTemplateProjectTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AbpIoTemplateProjectEntityFrameworkCoreTestModule>
{

}
