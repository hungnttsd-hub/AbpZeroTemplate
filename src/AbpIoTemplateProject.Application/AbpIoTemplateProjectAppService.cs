using System;
using System.Collections.Generic;
using System.Text;
using AbpIoTemplateProject.Localization;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject;

/* Inherit your application services from this class.
 */
public abstract class AbpIoTemplateProjectAppService : ApplicationService
{
    protected AbpIoTemplateProjectAppService()
    {
        LocalizationResource = typeof(AbpIoTemplateProjectResource);
    }
}
