using System;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages.Articles;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResultDto<ArticleSummaryDto> Articles { get; private set; } = new();
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Articles.TotalCount / 9d));

    public IndexModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        Articles = await _storefrontAppService.GetArticlesAsync(new PagedAndSortedResultRequestDto
        {
            SkipCount = (PageNumber - 1) * 9,
            MaxResultCount = 9
        });
        await LoadCartSummaryAsync(_cartAppService);
    }
}
