using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Store;

public class StoreAdminDashboardDto
{
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int CustomerCount { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public class SaveCategoryInput
{
    public Guid? Id { get; set; }
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxSlugLength)]
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class SaveBrandInput
{
    public Guid? Id { get; set; }
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxSlugLength)]
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveSupplierInput
{
    public Guid? Id { get; set; }
    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveProductInput
{
    public Guid? Id { get; set; }
    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string Sku { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxSlugLength)]
    public string Slug { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? SupplierId { get; set; }
    public string Unit { get; set; } = "Sản phẩm";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public string? UsageInstructions { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Weight { get; set; }
    public string? Warranty { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool AllowBackorder { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public int MaximumOrderQuantity { get; set; } = 99;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class SaveProductVariantInput
{
    public Guid ProductId { get; set; }
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string Sku { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string OptionSummary { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Weight { get; set; }
}

public class SaveProductImageInput
{
    public Guid ProductId { get; set; }
    [Required, MaxLength(StoreConsts.MaxUrlLength)]
    public string Url { get; set; } = string.Empty;
    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class InventoryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int LowStockThreshold { get; set; }
}

public class AdminCustomerDto : EntityDto<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class AdminPaymentDto : EntityDto<Guid>
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreationTime { get; set; }
}

public class ConfirmPaymentInput
{
    public Guid PaymentId { get; set; }
    [MaxLength(StoreConsts.MaxCodeLength)]
    public string? ReferenceNumber { get; set; }
}

public class AdjustInventoryInput
{
    public Guid InventoryItemId { get; set; }
    public int QuantityDelta { get; set; }
    [MaxLength(StoreConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class ChangeOrderStatusInput
{
    public Guid OrderId { get; set; }
    public OrderStatus TargetStatus { get; set; }
    public string? TrackingCode { get; set; }
    public string? Note { get; set; }
}

public class SavePromotionInput
{
    public Guid? Id { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PromotionDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBannerInput
{
    public Guid? Id { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string DesktopImageUrl { get; set; } = string.Empty;
    [Required] public string MobileImageUrl { get; set; } = string.Empty;
    public string? ButtonText { get; set; }
    public string? TargetUrl { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveArticleInput
{
    public Guid? Id { get; set; }
    public Guid ArticleCategoryId { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Slug { get; set; } = string.Empty;
    [Required] public string Summary { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    [Required] public string AuthorName { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool Publish { get; set; }
}
