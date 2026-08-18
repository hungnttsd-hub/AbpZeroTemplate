using System.IO;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebHoanTien.EntityFrameworkCore;
using WebHoanTien.Localization;
using WebHoanTien.Web.Menus;
using WebHoanTien.Web.Operations;
using WebHoanTien.Web.IdentityExtensions;
using WebHoanTien.Operations;
using WebHoanTien.Affiliates;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity.Web;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.Web;
using Volo.Abp.Security.Claims;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.Swashbuckle;
using Volo.Abp.OpenIddict;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.UI;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace WebHoanTien.Web;

[DependsOn(
    typeof(WebHoanTienHttpApiModule),
    typeof(WebHoanTienApplicationModule),
    typeof(WebHoanTienEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpIdentityWebModule),
    typeof(AbpSettingManagementWebModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpBackgroundJobsHangfireModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
    )]
public class WebHoanTienWebModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(WebHoanTienResource),
                typeof(WebHoanTienDomainModule).Assembly,
                typeof(WebHoanTienDomainSharedModule).Assembly,
                typeof(WebHoanTienApplicationModule).Assembly,
                typeof(WebHoanTienApplicationContractsModule).Assembly,
                typeof(WebHoanTienWebModule).Assembly
            );
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("WebHoanTien");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                var certificatePath = configuration["OpenIddict:CertificatePath"] ?? "openiddict.pfx";
                var certificatePassword = configuration["OpenIddict:CertificatePassword"];

                if (string.IsNullOrWhiteSpace(certificatePassword))
                {
                    throw new AbpException("OpenIddict:CertificatePassword must be configured outside Development.");
                }

                serverBuilder.AddProductionEncryptionAndSigningCertificate(certificatePath, certificatePassword);
            });
        }
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new AbpException("ConnectionStrings:Default chưa được cấu hình.");
        context.Services.AddHangfire(config => config.UsePostgreSqlStorage(
            options => options.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions { SchemaName = WebHoanTienConsts.HangfireDbSchema }));

        ConfigureAuthentication(context, hostingEnvironment, configuration);
        context.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!, name: "postgresql", tags: new[] { "ready" });
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureAutoMapper();
        ConfigureVirtualFileSystem(hostingEnvironment);
        ConfigureNavigationServices();
        ConfigureAutoApiControllers();
        ConfigureSwaggerServices(context.Services);
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IWebHostEnvironment environment, IConfiguration configuration)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
        {
            options.SignIn.RequireConfirmedEmail = configuration.GetValue("Identity:RequireConfirmedEmail", !environment.IsDevelopment());
            options.User.RequireUniqueEmail = true;
        });

        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            context.Services.AddAuthentication().AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = true;
                options.Events.OnCreatingTicket = ticketContext =>
                {
                    if (ticketContext.User.TryGetProperty("picture", out var picture) && picture.GetString() is { Length: > 0 } avatar)
                        ticketContext.Identity?.AddClaim(new Claim("google_avatar", avatar));
                    return System.Threading.Tasks.Task.CompletedTask;
                };
            });
        }
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureAutoMapper()
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<WebHoanTienWebModule>();
        });
    }

    private void ConfigureVirtualFileSystem(IWebHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<WebHoanTienDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}WebHoanTien.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<WebHoanTienDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}WebHoanTien.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<WebHoanTienApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}WebHoanTien.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<WebHoanTienApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}WebHoanTien.Application"));
                options.FileSets.ReplaceEmbeddedByPhysical<WebHoanTienWebModule>(hostingEnvironment.ContentRootPath);
            });
        }
    }

    private void ConfigureNavigationServices()
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new WebHoanTienMenuContributor());
        });
    }

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(WebHoanTienApplicationModule).Assembly);
        });
    }

    private void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "WebHoanTien API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        app.UseUnitOfWork();
        app.UseMiddleware<LegalConsentMiddleware>();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new AdminHangfireDashboardAuthorizationFilter() },
            DashboardTitle = "webHoanTien Jobs"
        });

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebHoanTien API");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();

        RecurringJob.AddOrUpdate<AffiliateSyncJob>(
            "affiliate-conversion-hourly",
            job => job.ExecuteAsync(new AffiliateSyncJobArgs { Platform = AffiliatePlatform.Shopee, Kind = AffiliateSyncKind.Conversion }),
            Cron.Hourly);
        RecurringJob.AddOrUpdate<AffiliateSyncJob>(
            "affiliate-reconciliation-daily",
            job => job.ExecuteAsync(new AffiliateSyncJobArgs { Platform = AffiliatePlatform.Shopee, Kind = AffiliateSyncKind.Reconciliation }),
            Cron.Daily(2));
        RecurringJob.AddOrUpdate<AffiliateRetentionJob>(
            "affiliate-retention-daily",
            job => job.ExecuteAsync(new AffiliateRetentionJobArgs()),
            Cron.Daily(3));
    }
}
