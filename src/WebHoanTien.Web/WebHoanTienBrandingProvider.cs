using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Web;

[Dependency(ReplaceServices = true)]
public class WebHoanTienBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "CatsBack";
}
