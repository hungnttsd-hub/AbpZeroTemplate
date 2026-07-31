using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Promotions.Default)]
public class ContentModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public List<PromotionDto> Promotions { get; private set; } = new();
    public List<BannerDto> Banners { get; private set; } = new();
    public List<ArticleCategoryDto> ArticleCategories { get; private set; } = new();
    public PagedResultDto<ArticleSummaryDto> Articles { get; private set; } = new();
    public DateTime DefaultStart => DateTime.Today;
    public DateTime DefaultEnd => DateTime.Today.AddMonths(1);
    public ContentModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { await LoadAsync(); }
    public async Task<IActionResult> OnPostPromotionAsync(SavePromotionInput input) { await _admin.SavePromotionAsync(input); return RedirectToPage(); }
    public async Task<IActionResult> OnPostBannerAsync(SaveBannerInput input) { await _admin.SaveBannerAsync(input); return RedirectToPage(); }
    public async Task<IActionResult> OnPostArticleAsync(SaveArticleInput input) { await _admin.SaveArticleAsync(input); return RedirectToPage(); }
    private async Task LoadAsync() { Promotions = await _admin.GetPromotionsAsync(); Banners = await _admin.GetBannersAsync(); ArticleCategories = await _admin.GetArticleCategoriesAsync(); Articles = await _admin.GetArticlesAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 60 }); }
}
