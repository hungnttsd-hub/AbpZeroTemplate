using AbpIoTemplateProject.Store;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace AbpIoTemplateProject.EntityFrameworkCore;

public static class StoreDbContextModelCreatingExtensions
{
    public static void ConfigureStore(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
        var prefix = AbpIoTemplateProjectConsts.DbTablePrefix + "Store";
        var schema = AbpIoTemplateProjectConsts.DbSchema;

        builder.Entity<Category>(b =>
        {
            b.ToTable(prefix + "Categories", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(StoreConsts.MaxSlugLength);
            b.Property(x => x.ImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ParentId, x.DisplayOrder });
            b.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Brand>(b =>
        {
            b.ToTable(prefix + "Brands", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(StoreConsts.MaxSlugLength);
            b.Property(x => x.LogoUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        });

        builder.Entity<Supplier>(b =>
        {
            b.ToTable(prefix + "Suppliers", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Phone).HasMaxLength(StoreConsts.MaxPhoneLength);
            b.Property(x => x.Email).HasMaxLength(StoreConsts.MaxEmailLength);
            b.Property(x => x.Address).HasMaxLength(StoreConsts.MaxAddressLength);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<Product>(b =>
        {
            b.ToTable(prefix + "Products", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(StoreConsts.MaxSlugLength);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(64);
            b.Property(x => x.ShortDescription).HasMaxLength(1024);
            b.Property(x => x.Warranty).HasMaxLength(256);
            b.Property(x => x.ThumbnailUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.CanonicalUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            ConfigureMoney(b.Property(x => x.SalePrice));
            ConfigureMoney(b.Property(x => x.ListPrice));
            ConfigureMoney(b.Property(x => x.CostPrice));
            b.Property(x => x.TaxRate).HasPrecision(5, 2);
            b.Property(x => x.Weight).HasPrecision(18, 3);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CategoryId, x.IsActive, x.IsVisible });
            b.HasIndex(x => new { x.TenantId, x.BrandId });
            b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Brand>().WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.Variants).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Images).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(x => x.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ProductVariant>(b =>
        {
            b.ToTable(prefix + "ProductVariants", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.OptionSummary).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.ImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            ConfigureMoney(b.Property(x => x.SalePrice));
            ConfigureMoney(b.Property(x => x.ListPrice));
            b.Property(x => x.Weight).HasPrecision(18, 3);
            b.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
        });

        builder.Entity<ProductImage>(b =>
        {
            b.ToTable(prefix + "ProductImages", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Url).IsRequired().HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.AltText).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        });

        builder.Entity<Warehouse>(b =>
        {
            b.ToTable(prefix + "Warehouses", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Address).IsRequired().HasMaxLength(StoreConsts.MaxAddressLength);
            b.Property(x => x.Phone).HasMaxLength(StoreConsts.MaxPhoneLength);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<InventoryItem>(b =>
        {
            b.ToTable(prefix + "InventoryItems", schema);
            b.ConfigureByConvention();
            b.Ignore(x => x.AvailableQuantity);
            b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId, x.ProductVariantId }).IsUnique();
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InventoryTransaction>(b =>
        {
            b.ToTable(prefix + "InventoryTransactions", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ReferenceType).HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.ReferenceNumber).HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Note).HasMaxLength(StoreConsts.MaxNoteLength);
            b.HasIndex(x => new { x.InventoryItemId, x.CreationTime });
            b.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShoppingCart>(b =>
        {
            b.ToTable(prefix + "ShoppingCarts", schema);
            b.ConfigureByConvention();
            b.Property(x => x.CartKey).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.PromotionCode).HasMaxLength(StoreConsts.MaxCodeLength);
            b.HasIndex(x => new { x.TenantId, x.CartKey }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.UserId, x.IsConverted });
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ShoppingCartId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ShoppingCartItem>(b =>
        {
            b.ToTable(prefix + "ShoppingCartItems", schema);
            b.ConfigureByConvention();
            ConfigureMoney(b.Property(x => x.UnitPrice));
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.OptionSummary).HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.ImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.HasIndex(x => new { x.ShoppingCartId, x.ProductId, x.ProductVariantId }).IsUnique();
        });

        builder.Entity<Customer>(b =>
        {
            b.ToTable(prefix + "Customers", schema);
            b.ConfigureByConvention();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(StoreConsts.MaxPhoneLength);
            b.Property(x => x.Email).IsRequired().HasMaxLength(StoreConsts.MaxEmailLength);
            b.HasIndex(x => new { x.TenantId, x.UserId });
            b.HasIndex(x => new { x.TenantId, x.Phone });
            b.HasMany(x => x.Addresses).WithOne().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<CustomerAddress>(b =>
        {
            b.ToTable(prefix + "CustomerAddresses", schema);
            b.ConfigureByConvention();
            b.Property(x => x.RecipientName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(StoreConsts.MaxPhoneLength);
            b.Property(x => x.Province).IsRequired().HasMaxLength(128);
            b.Property(x => x.District).IsRequired().HasMaxLength(128);
            b.Property(x => x.Ward).IsRequired().HasMaxLength(128);
            b.Property(x => x.AddressLine).IsRequired().HasMaxLength(StoreConsts.MaxAddressLength);
        });

        builder.Entity<Order>(b =>
        {
            b.ToTable(prefix + "Orders", schema);
            b.ConfigureByConvention();
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(StoreConsts.MaxPhoneLength);
            b.Property(x => x.Email).IsRequired().HasMaxLength(StoreConsts.MaxEmailLength);
            b.Property(x => x.AddressLine).IsRequired().HasMaxLength(StoreConsts.MaxAddressLength);
            b.Property(x => x.DeliveryNote).HasMaxLength(StoreConsts.MaxNoteLength);
            b.Property(x => x.TrackingCode).HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.CancellationReason).HasMaxLength(StoreConsts.MaxNoteLength);
            b.Property(x => x.PromotionCode).HasMaxLength(StoreConsts.MaxCodeLength);
            ConfigureMoney(b.Property(x => x.Subtotal));
            ConfigureMoney(b.Property(x => x.DiscountAmount));
            ConfigureMoney(b.Property(x => x.ShippingFee));
            ConfigureMoney(b.Property(x => x.TaxAmount));
            ConfigureMoney(b.Property(x => x.GrandTotal));
            b.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.UserId, x.CreationTime });
            b.HasIndex(x => new { x.TenantId, x.Status, x.CreationTime });
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ShippingMethod>().WithMany().HasForeignKey(x => x.ShippingMethodId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(x => x.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<OrderItem>(b =>
        {
            b.ToTable(prefix + "OrderItems", schema);
            b.ConfigureByConvention();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.OptionSummary).HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.ImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            ConfigureMoney(b.Property(x => x.UnitPrice));
            b.Property(x => x.TaxRate).HasPrecision(5, 2);
        });

        builder.Entity<OrderStatusHistory>(b =>
        {
            b.ToTable(prefix + "OrderStatusHistories", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Note).HasMaxLength(StoreConsts.MaxNoteLength);
            b.HasIndex(x => new { x.OrderId, x.CreationTime });
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable(prefix + "Payments", schema);
            b.ConfigureByConvention();
            ConfigureMoney(b.Property(x => x.Amount));
            b.Property(x => x.ReferenceNumber).HasMaxLength(StoreConsts.MaxCodeLength);
            b.HasIndex(x => new { x.TenantId, x.OrderId });
            b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShippingMethod>(b =>
        {
            b.ToTable(prefix + "ShippingMethods", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            ConfigureMoney(b.Property(x => x.Fee));
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<Promotion>(b =>
        {
            b.ToTable(prefix + "Promotions", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(StoreConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            ConfigureMoney(b.Property(x => x.Value));
            ConfigureMoney(b.Property(x => x.MinimumOrderAmount));
            ConfigureMoney(b.Property(x => x.MaximumDiscountAmount));
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive, x.StartTime, x.EndTime });
        });

        builder.Entity<PromotionUsage>(b =>
        {
            b.ToTable(prefix + "PromotionUsages", schema);
            b.ConfigureByConvention();
            ConfigureMoney(b.Property(x => x.DiscountAmount));
            b.HasIndex(x => new { x.PromotionId, x.CustomerId });
            b.HasOne<Promotion>().WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Banner>(b =>
        {
            b.ToTable(prefix + "Banners", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.DesktopImageUrl).IsRequired().HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.MobileImageUrl).IsRequired().HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.TargetUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.HasIndex(x => new { x.TenantId, x.IsActive, x.DisplayOrder });
        });

        builder.Entity<StoreLocation>(b =>
        {
            b.ToTable(prefix + "Locations", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Address).IsRequired().HasMaxLength(StoreConsts.MaxAddressLength);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(StoreConsts.MaxPhoneLength);
            b.Property(x => x.MapUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.ImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
        });

        builder.Entity<ArticleCategory>(b =>
        {
            b.ToTable(prefix + "ArticleCategories", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(StoreConsts.MaxSlugLength);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        });

        builder.Entity<Article>(b =>
        {
            b.ToTable(prefix + "Articles", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(StoreConsts.MaxSlugLength);
            b.Property(x => x.Summary).IsRequired().HasMaxLength(StoreConsts.MaxAddressLength);
            b.Property(x => x.FeaturedImageUrl).HasMaxLength(StoreConsts.MaxUrlLength);
            b.Property(x => x.AuthorName).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status, x.PublishTime });
            b.HasOne<ArticleCategory>().WithMany().HasForeignKey(x => x.ArticleCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HomePageSection>(b =>
        {
            b.ToTable(prefix + "HomePageSections", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.HasIndex(x => new { x.TenantId, x.IsVisible, x.DisplayOrder });
        });

        builder.Entity<SiteSetting>(b =>
        {
            b.ToTable(prefix + "SiteSettings", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Key).IsRequired().HasMaxLength(StoreConsts.MaxNameLength);
            b.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        });
    }

    private static void ConfigureMoney<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<T> propertyBuilder)
    {
        propertyBuilder.HasPrecision(18, 2);
    }
}
