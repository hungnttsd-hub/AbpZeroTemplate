using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;

namespace WebHoanTien.EntityFrameworkCore;

[DependsOn(typeof(WebHoanTienApplicationTestModule), typeof(WebHoanTienEntityFrameworkCoreModule))]
public class WebHoanTienEntityFrameworkCoreTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(x => { x.SaveStaticFeaturesToDatabase = false; x.IsDynamicFeatureStoreEnabled = false; });
        Configure<PermissionManagementOptions>(x => { x.SaveStaticPermissionsToDatabase = false; x.IsDynamicPermissionStoreEnabled = false; });
        Configure<SettingManagementOptions>(x => { x.SaveStaticSettingsToDatabase = false; x.IsDynamicSettingStoreEnabled = false; });
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();
        context.Services.Configure<AbpDbContextOptions>(options => options.Configure(config =>
            config.DbContextOptions.UseNpgsql(WebHoanTienEntityFrameworkCoreFixture.ConnectionString)));
        context.Services.AddTransient<IAffiliateProvider, TestAffiliateProvider>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
        {
            using var scope = context.ServiceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
        });
    }
}

public class TestAffiliateProvider : IAffiliateProvider
{
    public AffiliatePlatform Platform => AffiliatePlatform.Shopee;

    public Task<AffiliateShortLinkResult> GenerateShortLinkAsync(string originUrl, string trackingToken,
        System.Threading.CancellationToken cancellationToken = default) =>
        Task.FromResult(new AffiliateShortLinkResult("https://s.shopee.vn/test"));

    public Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId,
        System.Threading.CancellationToken cancellationToken = default) =>
        Task.FromResult<AffiliateProductOffer?>(null);

    public Task<AffiliateConversionPage> GetConversionsAsync(AffiliateConversionQuery query,
        System.Threading.CancellationToken cancellationToken = default) =>
        Task.FromResult(CreatePage(query));

    public Task<AffiliateConversionPage> GetValidatedConversionsAsync(AffiliateConversionQuery query,
        System.Threading.CancellationToken cancellationToken = default) =>
        Task.FromResult(CreatePage(query));

    private static AffiliateConversionPage CreatePage(AffiliateConversionQuery query)
    {
        var purchaseTime = query.From.AddMinutes(1);
        var item = new NormalizedAffiliateOrderItem("TEST-ITEM", "MODEL", "Test product", 500_000m, 1,
            100_000m, 0m, false, "PENDING");
        var order = new NormalizedAffiliateOrder("TEST-ORDER", AffiliateOrderStatus.Pending, "Marketplace",
            500_000m, 100_000m, new[] { item });
        var conversion = new NormalizedAffiliateConversion("TEST-CONVERSION", null, purchaseTime, null,
            AffiliateConversionStatus.Pending, 100_000m, 100_000m, CommissionSource.NetCommission,
            new[] { order });
        return new AffiliateConversionPage(new[] { conversion }, null, "{\"fixture\":true}");
    }
}
