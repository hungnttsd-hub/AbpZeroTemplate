using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AbpIoTemplateProject.Pages;

public class Index_Tests : AbpIoTemplateProjectWebTestBase
{
    [Fact]
    public async Task Storefront_Home_Page_Should_Render()
    {
        var response = await GetResponseAsStringAsync("/");

        response.ShouldContain("<html lang=\"vi\"");
        response.ShouldContain("store-home-hero");
        response.ShouldContain("AquaHome");
    }
}
