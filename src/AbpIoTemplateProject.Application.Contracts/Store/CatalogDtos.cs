using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Store;

public class ProductListInput : PagedAndSortedResultRequestDto
{
    [MaxLength(256)]
    public string? Filter { get; set; }

    public string? Category { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyInStock { get; set; }
    public bool OnSale { get; set; }
    public bool Featured { get; set; }
    public bool Newest { get; set; }
    public bool BestSelling { get; set; }

    public ProductListInput()
    {
        MaxResultCount = StoreConsts.DefaultPageSize;
    }
}

public class CategoryDto : EntityDto<Guid>
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public int ProductCount { get; set; }
}

public class BrandDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsFeatured { get; set; }
}

public class SupplierDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ProductListItemDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? HoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsInStock { get; set; }
    public bool HasVariants { get; set; }
    public int DiscountPercent =>
        SalePrice.HasValue && ListPrice.HasValue && ListPrice > 0 && SalePrice < ListPrice
            ? (int)Math.Round((ListPrice.Value - SalePrice.Value) / ListPrice.Value * 100m)
            : 0;
}

public class ProductVariantDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string OptionSummary { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Weight { get; set; }
    public bool IsActive { get; set; }
    public bool IsInStock { get; set; }
    public int MaximumPurchasableQuantity { get; set; }
}

public class ProductImageDto : EntityDto<Guid>
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductDetailDto : ProductListItemDto
{
    public ProductType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public string? UsageInstructions { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Weight { get; set; }
    public string? Warranty { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public int MaximumOrderQuantity { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductListItemDto> RelatedProducts { get; set; } = new();
}

public class SearchSuggestionDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public bool IsInStock { get; set; }
}

public class BannerDto : EntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DesktopImageUrl { get; set; } = string.Empty;
    public string MobileImageUrl { get; set; } = string.Empty;
    public string? ButtonText { get; set; }
    public string? TargetUrl { get; set; }
}

public class ArticleSummaryDto : EntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishTime { get; set; }
    public bool IsFeatured { get; set; }
}

public class ArticleCategoryDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class ArticleDetailDto : ArticleSummaryDto
{
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public List<ArticleSummaryDto> RelatedArticles { get; set; } = new();
}

public class StoreLocationDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string? MapUrl { get; set; }
    public string? ImageUrl { get; set; }
}

public class HomePageDto
{
    public List<BannerDto> Banners { get; set; } = new();
    public List<CategoryDto> FeaturedCategories { get; set; } = new();
    public List<ProductListItemDto> FeaturedProducts { get; set; } = new();
    public List<ProductListItemDto> BestSellers { get; set; } = new();
    public List<ProductListItemDto> NewProducts { get; set; } = new();
    public List<ProductListItemDto> PromotionProducts { get; set; } = new();
    public List<ArticleSummaryDto> Articles { get; set; } = new();
    public List<StoreLocationDto> Stores { get; set; } = new();
}
