using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Store;

public interface IStorefrontAppService : IApplicationService
{
    Task<HomePageDto> GetHomeAsync();
    Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductListInput input);
    Task<ProductDetailDto> GetProductAsync(string slug);
    Task<List<SearchSuggestionDto>> SearchSuggestionsAsync(string query);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<List<BrandDto>> GetBrandsAsync();
    Task<PagedResultDto<ArticleSummaryDto>> GetArticlesAsync(PagedAndSortedResultRequestDto input);
    Task<ArticleDetailDto> GetArticleAsync(string slug);
    Task<List<StoreLocationDto>> GetStoresAsync();
}
