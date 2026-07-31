using Volo.Abp.Settings;
using Volo.Abp.Localization;

namespace AbpIoTemplateProject.Settings;

public class AbpIoTemplateProjectSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        var defaultLanguage = context.GetOrNull(LocalizationSettingNames.DefaultLanguage);
        if (defaultLanguage is not null)
        {
            defaultLanguage.DefaultValue = "vi";
        }
    }
}
