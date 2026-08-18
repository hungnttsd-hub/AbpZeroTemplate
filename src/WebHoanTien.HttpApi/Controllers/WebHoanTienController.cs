using WebHoanTien.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace WebHoanTien.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class WebHoanTienController : AbpControllerBase
{
    protected WebHoanTienController()
    {
        LocalizationResource = typeof(WebHoanTienResource);
    }
}
