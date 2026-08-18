using WebHoanTien.Samples;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Applications;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<WebHoanTienEntityFrameworkCoreTestModule>
{

}
