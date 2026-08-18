using Volo.Abp.Modularity;

namespace WebHoanTien;

[DependsOn(
    typeof(WebHoanTienDomainModule),
    typeof(WebHoanTienTestBaseModule)
)]
public class WebHoanTienDomainTestModule : AbpModule
{

}
