using Volo.Abp.Modularity;

namespace WebHoanTien;

[DependsOn(
    typeof(WebHoanTienApplicationModule),
    typeof(WebHoanTienDomainTestModule)
)]
public class WebHoanTienApplicationTestModule : AbpModule
{

}
