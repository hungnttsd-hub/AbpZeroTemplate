using AbpIoTemplateProject.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AbpIoTemplateProjectController : AbpControllerBase
{
    protected AbpIoTemplateProjectController()
    {
        LocalizationResource = typeof(AbpIoTemplateProjectResource);
    }
}
