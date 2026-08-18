using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class LinksModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    public PagedResultDto<AffiliateTrackingDto> Links { get; private set; } = new();
    public LinksModel(IAffiliateLinkAppService links) => _links = links;
    public async Task OnGetAsync() => Links = await _links.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 50 });
}
