using WebHoanTien.Samples;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Domains;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<WebHoanTienEntityFrameworkCoreTestModule>
{

}
