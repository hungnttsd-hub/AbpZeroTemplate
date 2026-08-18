using System;
using System.Collections.Generic;
using System.Text;
using WebHoanTien.Localization;
using Volo.Abp.Application.Services;

namespace WebHoanTien;

/* Inherit your application services from this class.
 */
public abstract class WebHoanTienAppService : ApplicationService
{
    protected WebHoanTienAppService()
    {
        LocalizationResource = typeof(WebHoanTienResource);
    }
}
