using Microsoft.Extensions.Localization;
using WebHoanTien.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Web;

[Dependency(ReplaceServices = true)]
public class WebHoanTienBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<WebHoanTienResource> _localizer;

    public WebHoanTienBrandingProvider(IStringLocalizer<WebHoanTienResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
