using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Store;

public interface IStoreAdminAppService : IApplicationService
{
    Task<StoreAdminDashboardDto> GetDashboardAsync();
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto> SaveCategoryAsync(SaveCategoryInput input);
    Task<List<BrandDto>> GetBrandsAsync();
    Task<BrandDto> SaveBrandAsync(SaveBrandInput input);
    Task<List<SupplierDto>> GetSuppliersAsync();
    Task<SupplierDto> SaveSupplierAsync(SaveSupplierInput input);
    Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(ProductListInput input);
    Task<Guid> SaveProductAsync(SaveProductInput input);
    Task<Guid> AddProductVariantAsync(SaveProductVariantInput input);
    Task<Guid> AddProductImageAsync(SaveProductImageInput input);
    Task<List<InventoryItemDto>> GetInventoryAsync();
    Task AdjustInventoryAsync(AdjustInventoryInput input);
    Task<List<AdminCustomerDto>> GetCustomersAsync();
    Task<List<AdminPaymentDto>> GetPaymentsAsync();
    Task ConfirmPaymentAsync(ConfirmPaymentInput input);
    Task<PagedResultDto<OrderDto>> GetOrdersAsync(PagedAndSortedResultRequestDto input);
    Task ChangeOrderStatusAsync(ChangeOrderStatusInput input);
    Task<List<PromotionDto>> GetPromotionsAsync();
    Task<Guid> SavePromotionAsync(SavePromotionInput input);
    Task<List<BannerDto>> GetBannersAsync();
    Task<Guid> SaveBannerAsync(SaveBannerInput input);
    Task<List<ArticleCategoryDto>> GetArticleCategoriesAsync();
    Task<PagedResultDto<ArticleSummaryDto>> GetArticlesAsync(PagedAndSortedResultRequestDto input);
    Task<Guid> SaveArticleAsync(SaveArticleInput input);
}
