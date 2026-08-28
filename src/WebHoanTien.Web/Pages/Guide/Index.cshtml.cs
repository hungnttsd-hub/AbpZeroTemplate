using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHoanTien.Web.Pages.Guide;

[AllowAnonymous]
public class IndexModel : PageModel
{
    public IReadOnlyList<GuideVideoDefinition> Videos => GuideVideoCatalog.All;
}
