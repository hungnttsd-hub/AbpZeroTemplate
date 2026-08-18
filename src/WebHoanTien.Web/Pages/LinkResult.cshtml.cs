using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class LinkResultModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    public AffiliateTrackingDto Tracking { get; private set; } = null!;
    public LinkResultModel(IAffiliateLinkAppService links) => _links = links;
    public async Task<IActionResult> OnGetAsync(Guid id) { Tracking = await _links.GetAsync(id); return Page(); }
}
