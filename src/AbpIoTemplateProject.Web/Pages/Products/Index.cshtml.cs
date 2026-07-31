using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages.Products;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IStorefrontAppService _storefrontAppService;
    private readonly ICartAppService _cartAppService;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? BrandId { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool InStock { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnSale { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResultDto<ProductListItemDto> Products { get; private set; } = new();
    public List<CategoryDto> Categories { get; private set; } = new();
    public List<BrandDto> Brands { get; private set; } = new();
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Products.TotalCount / (double)StoreConsts.DefaultPageSize));

    public IndexModel(IStorefrontAppService storefrontAppService, ICartAppService cartAppService)
    {
        _storefrontAppService = storefrontAppService;
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        Products = await _storefrontAppService.GetProductsAsync(new ProductListInput
        {
            Filter = Q,
            Category = Category,
            BrandId = BrandId,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            OnlyInStock = InStock,
            OnSale = OnSale || string.Equals(Sort, "promotions", StringComparison.OrdinalIgnoreCase),
            Newest = string.Equals(Sort, "newest", StringComparison.OrdinalIgnoreCase),
            BestSelling = string.Equals(Sort, "best-selling", StringComparison.OrdinalIgnoreCase),
            Sorting = Sort switch
            {
                "price-asc" => "price asc",
                "price-desc" => "price desc",
                "name" => "name asc",
                _ => null
            },
            SkipCount = (PageNumber - 1) * StoreConsts.DefaultPageSize,
            MaxResultCount = StoreConsts.DefaultPageSize
        });
        Categories = await _storefrontAppService.GetCategoriesAsync();
        Brands = await _storefrontAppService.GetBrandsAsync();
        await LoadCartSummaryAsync(_cartAppService);
    }

    public async Task<JsonResult> OnGetSuggestionsAsync(string query)
    {
        return new JsonResult(await _storefrontAppService.SearchSuggestionsAsync(query));
    }
}
