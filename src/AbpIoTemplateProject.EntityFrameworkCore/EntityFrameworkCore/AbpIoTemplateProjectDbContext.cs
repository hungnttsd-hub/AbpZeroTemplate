using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using AbpIoTemplateProject.Store;

namespace AbpIoTemplateProject.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class AbpIoTemplateProjectDbContext :
    AbpDbContext<AbpIoTemplateProjectDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    public DbSet<Category> StoreCategories { get; set; }
    public DbSet<Brand> StoreBrands { get; set; }
    public DbSet<Supplier> StoreSuppliers { get; set; }
    public DbSet<Product> StoreProducts { get; set; }
    public DbSet<ProductVariant> StoreProductVariants { get; set; }
    public DbSet<ProductImage> StoreProductImages { get; set; }
    public DbSet<Warehouse> StoreWarehouses { get; set; }
    public DbSet<InventoryItem> StoreInventoryItems { get; set; }
    public DbSet<InventoryTransaction> StoreInventoryTransactions { get; set; }
    public DbSet<ShoppingCart> StoreShoppingCarts { get; set; }
    public DbSet<ShoppingCartItem> StoreShoppingCartItems { get; set; }
    public DbSet<Customer> StoreCustomers { get; set; }
    public DbSet<CustomerAddress> StoreCustomerAddresses { get; set; }
    public DbSet<Order> StoreOrders { get; set; }
    public DbSet<OrderItem> StoreOrderItems { get; set; }
    public DbSet<OrderStatusHistory> StoreOrderStatusHistories { get; set; }
    public DbSet<Payment> StorePayments { get; set; }
    public DbSet<ShippingMethod> StoreShippingMethods { get; set; }
    public DbSet<Promotion> StorePromotions { get; set; }
    public DbSet<PromotionUsage> StorePromotionUsages { get; set; }
    public DbSet<Banner> StoreBanners { get; set; }
    public DbSet<StoreLocation> StoreLocations { get; set; }
    public DbSet<ArticleCategory> StoreArticleCategories { get; set; }
    public DbSet<Article> StoreArticles { get; set; }
    public DbSet<HomePageSection> StoreHomePageSections { get; set; }
    public DbSet<SiteSetting> StoreSiteSettings { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public AbpIoTemplateProjectDbContext(DbContextOptions<AbpIoTemplateProjectDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        builder.ConfigureStore();
    }
}
