using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AbpIoTemplateProject.Pages;

public class Index_Tests : AbpIoTemplateProjectWebTestBase
{
    [Fact]
    public async Task Education_Home_Page_Should_Render()
    {
        var response = await GetResponseAsStringAsync("/");

        response.ShouldContain("<html lang=\"vi\"");
        response.ShouldContain("iz-hero");
        response.ShouldContain("IZONE");
    }
}
