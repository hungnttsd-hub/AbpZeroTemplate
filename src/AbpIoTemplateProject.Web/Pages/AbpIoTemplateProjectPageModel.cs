using AbpIoTemplateProject.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace AbpIoTemplateProject.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class AbpIoTemplateProjectPageModel : AbpPageModel
{
    protected AbpIoTemplateProjectPageModel()
    {
        LocalizationResourceType = typeof(AbpIoTemplateProjectResource);
    }
}
