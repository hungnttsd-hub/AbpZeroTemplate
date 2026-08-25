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

    public Task<AffiliateProductOffer?> GetProductOfferAsync(string itemId,
        System.Threading.CancellationToken cancellationToken = default) =>
        Task.FromResult<AffiliateProductOffer?>(null);
}
