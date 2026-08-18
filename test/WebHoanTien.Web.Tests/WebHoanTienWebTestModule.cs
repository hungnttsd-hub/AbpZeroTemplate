using System.Collections.Generic;
using System.Globalization;
using Hangfire;
using Hangfire.PostgreSql;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using WebHoanTien.EntityFrameworkCore;
using WebHoanTien.Localization;
using WebHoanTien.Web;
using WebHoanTien.Web.Menus;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Validation.Localization;
using Xunit;

namespace WebHoanTien;

[DependsOn(
    typeof(AbpAspNetCoreTestBaseModule),
    typeof(WebHoanTienWebModule),
    typeof(WebHoanTienApplicationTestModule),
    typeof(WebHoanTienEntityFrameworkCoreTestModule)
)]
public class WebHoanTienWebTestModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<IMvcBuilder>(builder =>
        {
            builder.PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(typeof(WebHoanTienWebModule).Assembly));
        });

        context.Services.GetPreConfigureActions<OpenIddictServerBuilder>().Clear();
        PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
        {
            options.AddDevelopmentEncryptionAndSigningCertificate = true;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(
            options => options.UseNpgsqlConnection(WebHoanTienEntityFrameworkCoreFixture.ConnectionString),
            new PostgreSqlStorageOptions { SchemaName = WebHoanTienConsts.HangfireDbSchema }));

        ConfigureLocalizationServices(context.Services);
        ConfigureNavigationServices(context.Services);
    }

    private static void ConfigureLocalizationServices(IServiceCollection services)
    {
        var cultures = new List<CultureInfo> { new CultureInfo("vi"), new CultureInfo("en") };
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("vi");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        services.Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<WebHoanTienResource>()
                .AddBaseTypes(
                    typeof(AbpValidationResource),
                    typeof(AbpUiResource)
                );
        });
    }

    private static void ConfigureNavigationServices(IServiceCollection services)
    {
        services.Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new WebHoanTienMenuContributor());
        });
    }
}

[CollectionDefinition(WebHoanTienTestConsts.CollectionDefinitionName)]
public class WebHoanTienWebCollection : ICollectionFixture<WebHoanTienEntityFrameworkCoreFixture>
{
}
