using Microsoft.Extensions.Localization;
using AbpIoTemplateProject.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AbpIoTemplateProject.Web;

[Dependency(ReplaceServices = true)]
public class AbpIoTemplateProjectBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AbpIoTemplateProjectResource> _localizer;

    public AbpIoTemplateProjectBrandingProvider(IStringLocalizer<AbpIoTemplateProjectResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
