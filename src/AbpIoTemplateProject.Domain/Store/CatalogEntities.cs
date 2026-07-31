using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace AbpIoTemplateProject.Store;

public class Category : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    protected Category()
    {
    }

    public Category(Guid id, string name, string slug, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        SetName(name);
        SetSlug(slug);
        IsActive = true;
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string? imageUrl,
        Guid? parentId,
        bool isFeatured,
        bool isActive,
        int displayOrder)
    {
        if (parentId == Id)
        {
            throw new BusinessException("Store:CategoryCannotBeItsOwnParent");
        }

        SetName(name);
        SetSlug(slug);
        Description = description?.Trim();
        ImageUrl = imageUrl?.Trim();
        ParentId = parentId;
        IsFeatured = isFeatured;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    private void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
    }

    private void SetSlug(string slug)
    {
        Slug = Check.NotNullOrWhiteSpace(slug, nameof(slug), StoreConsts.MaxSlugLength).ToLowerInvariant();
    }
}

public class Brand : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsActive { get; private set; }

    protected Brand()
    {
    }

    public Brand(Guid id, string name, string slug, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Update(name, slug, null, null, false, true);
    }

    public void Update(string name, string slug, string? description, string? logoUrl, bool isFeatured, bool isActive)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Slug = Check.NotNullOrWhiteSpace(slug, nameof(slug), StoreConsts.MaxSlugLength).ToLowerInvariant();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        IsFeatured = isFeatured;
        IsActive = isActive;
    }
}

public class Supplier : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    protected Supplier()
    {
    }

    public Supplier(Guid id, string code, string name, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), StoreConsts.MaxCodeLength).ToUpperInvariant();
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        IsActive = true;
    }

    public void Update(string name, string? contactName, string? phone, string? email, string? address, bool isActive)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        ContactName = contactName?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        IsActive = isActive;
    }
}

public class Product : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductImage> _images = new();

    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public ProductType Type { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string Unit { get; private set; } = "Sản phẩm";
    public string? ShortDescription { get; private set; }
    public string? Description { get; private set; }
    public string? Specifications { get; private set; }
    public string? UsageInstructions { get; private set; }
    public decimal? SalePrice { get; private set; }
    public decimal? ListPrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal Weight { get; private set; }
    public string? Warranty { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsNew { get; private set; }
    public bool IsBestSeller { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsVisible { get; private set; }
    public bool AllowBackorder { get; private set; }
    public int MinimumOrderQuantity { get; private set; }
    public int MaximumOrderQuantity { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public IReadOnlyCollection<ProductVariant> Variants => _variants;
    public IReadOnlyCollection<ProductImage> Images => _images;

    protected Product()
    {
    }

    public Product(
        Guid id,
        string code,
        string sku,
        string name,
        string slug,
        ProductType type,
        Guid categoryId,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), StoreConsts.MaxCodeLength).ToUpperInvariant();
        Sku = Check.NotNullOrWhiteSpace(sku, nameof(sku), StoreConsts.MaxCodeLength).ToUpperInvariant();
        Type = type;
        CategoryId = categoryId;
        SetNameAndSlug(name, slug);
        IsActive = true;
        IsVisible = true;
        MinimumOrderQuantity = 1;
        MaximumOrderQuantity = 99;
    }

    public void UpdateDetails(
        string name,
        string slug,
        Guid categoryId,
        Guid? brandId,
        Guid? supplierId,
        string unit,
        string? shortDescription,
        string? description,
        string? specifications,
        string? usageInstructions,
        string? thumbnailUrl,
        decimal weight,
        string? warranty)
    {
        SetNameAndSlug(name, slug);
        CategoryId = categoryId;
        BrandId = brandId;
        SupplierId = supplierId;
        Unit = Check.NotNullOrWhiteSpace(unit, nameof(unit), 64);
        ShortDescription = shortDescription?.Trim();
        Description = description?.Trim();
        Specifications = specifications?.Trim();
        UsageInstructions = usageInstructions?.Trim();
        ThumbnailUrl = thumbnailUrl?.Trim();
        Weight = Check.Range(weight, nameof(weight), 0, decimal.MaxValue);
        Warranty = warranty?.Trim();
    }

    public void ChangePrice(decimal? salePrice, decimal? listPrice, decimal? costPrice, decimal taxRate)
    {
        if (salePrice < 0 || listPrice < 0 || costPrice < 0)
        {
            throw new BusinessException("Store:PriceCannotBeNegative");
        }

        SalePrice = salePrice;
        ListPrice = listPrice;
        CostPrice = costPrice;
        TaxRate = Check.Range(taxRate, nameof(taxRate), 0, 100);
    }

    public void ConfigureSales(
        bool isFeatured,
        bool isNew,
        bool isBestSeller,
        bool isActive,
        bool isVisible,
        bool allowBackorder,
        int minimumOrderQuantity,
        int maximumOrderQuantity)
    {
        if (minimumOrderQuantity < 1 || maximumOrderQuantity < minimumOrderQuantity)
        {
            throw new BusinessException("Store:InvalidOrderQuantityRange");
        }

        IsFeatured = isFeatured;
        IsNew = isNew;
        IsBestSeller = isBestSeller;
        IsActive = isActive;
        IsVisible = isVisible;
        AllowBackorder = allowBackorder;
        MinimumOrderQuantity = minimumOrderQuantity;
        MaximumOrderQuantity = maximumOrderQuantity;
    }

    public void SetSeo(string? metaTitle, string? metaDescription, string? metaKeywords, string? canonicalUrl)
    {
        MetaTitle = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
        MetaKeywords = metaKeywords?.Trim();
        CanonicalUrl = canonicalUrl?.Trim();
    }

    public ProductVariant AddVariant(
        Guid id,
        string name,
        string sku,
        string optionSummary,
        decimal? salePrice,
        decimal? listPrice,
        string? imageUrl,
        decimal? weight)
    {
        if (Type != ProductType.Variant)
        {
            throw new BusinessException("Store:SimpleProductCannotHaveVariants");
        }

        var variant = new ProductVariant(
            id,
            Id,
            name,
            sku,
            optionSummary,
            salePrice,
            listPrice,
            imageUrl,
            weight,
            TenantId);
        _variants.Add(variant);
        return variant;
    }

    public void AddImage(Guid id, string url, string altText, int displayOrder, bool isPrimary)
    {
        if (isPrimary)
        {
            foreach (var existingImage in _images)
            {
                existingImage.UnmarkPrimary();
            }
        }

        _images.Add(new ProductImage(id, Id, url, altText, displayOrder, isPrimary, TenantId));
    }

    private void SetNameAndSlug(string name, string slug)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Slug = Check.NotNullOrWhiteSpace(slug, nameof(slug), StoreConsts.MaxSlugLength).ToLowerInvariant();
    }
}

public class ProductVariant : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string OptionSummary { get; private set; } = string.Empty;
    public decimal? SalePrice { get; private set; }
    public decimal? ListPrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal? Weight { get; private set; }
    public bool IsActive { get; private set; }

    protected ProductVariant()
    {
    }

    internal ProductVariant(
        Guid id,
        Guid productId,
        string name,
        string sku,
        string optionSummary,
        decimal? salePrice,
        decimal? listPrice,
        string? imageUrl,
        decimal? weight,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Sku = Check.NotNullOrWhiteSpace(sku, nameof(sku), StoreConsts.MaxCodeLength).ToUpperInvariant();
        OptionSummary = Check.NotNullOrWhiteSpace(optionSummary, nameof(optionSummary), StoreConsts.MaxNameLength);
        SalePrice = salePrice;
        ListPrice = listPrice;
        ImageUrl = imageUrl;
        Weight = weight;
        IsActive = true;
    }

    public void UpdatePrice(decimal? salePrice, decimal? listPrice)
    {
        if (salePrice < 0 || listPrice < 0)
        {
            throw new BusinessException("Store:PriceCannotBeNegative");
        }

        SalePrice = salePrice;
        ListPrice = listPrice;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}

public class ProductImage : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    protected ProductImage()
    {
    }

    internal ProductImage(
        Guid id,
        Guid productId,
        string url,
        string altText,
        int displayOrder,
        bool isPrimary,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        Url = Check.NotNullOrWhiteSpace(url, nameof(url), StoreConsts.MaxUrlLength);
        AltText = Check.NotNullOrWhiteSpace(altText, nameof(altText), StoreConsts.MaxNameLength);
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    internal void UnmarkPrimary()
    {
        IsPrimary = false;
    }
}
