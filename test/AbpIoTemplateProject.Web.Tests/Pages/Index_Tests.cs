using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AbpIoTemplateProject.Pages;

public class Index_Tests : AbpIoTemplateProjectWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
