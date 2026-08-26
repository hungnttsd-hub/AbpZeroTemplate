using System;
using System.IO;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                var certificateBase64 = configuration["OpenIddict:CertificateBase64"];

                if (string.IsNullOrWhiteSpace(certificatePassword))
                {
                    throw new AbpException("OpenIddict:CertificatePassword must be configured outside Development.");
                }

                if (!string.IsNullOrWhiteSpace(certificateBase64))
                {
                    try
                    {
                        var certificate = new X509Certificate2(
                            Convert.FromBase64String(certificateBase64),
                            certificatePassword,
                            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
                        serverBuilder.AddEncryptionCertificate(certificate);
                        serverBuilder.AddSigningCertificate(certificate);
                    }
                    catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException)
                    {
                        throw new AbpException("OpenIddict:CertificateBase64 không phải PFX Base64 hợp lệ.", exception);
                    }
                }
                else
                {
                    serverBuilder.AddProductionEncryptionAndSigningCertificate(certificatePath, certificatePassword);
                }
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

        context.Services.AddDataProtection()
            .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "CatsBack")
            .PersistKeysToDbContext<WebHoanTienDbContext>();

        ConfigureAuthentication(context, hostingEnvironment, configuration);
        context.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!, name: "postgresql", tags: new[] { "ready" });
        context.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
        });
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
        var cookieExpireDays = Math.Clamp(configuration.GetValue("Authentication:Cookie:ExpireDays", 30), 1, 365);
        context.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(cookieExpireDays);
            options.SlidingExpiration = configuration.GetValue("Authentication:Cookie:SlidingExpiration", true);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        context.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
        {
            options.SignIn.RequireConfirmedEmail = configuration.GetValue("Identity:RequireConfirmedEmail", !environment.IsDevelopment());
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = configuration.GetValue("Identity:Password:RequiredLength", 6);
            options.Password.RequiredUniqueChars = configuration.GetValue("Identity:Password:RequiredUniqueChars", 0);
            options.Password.RequireDigit = configuration.GetValue("Identity:Password:RequireDigit", false);
            options.Password.RequireLowercase = configuration.GetValue("Identity:Password:RequireLowercase", false);
            options.Password.RequireUppercase = configuration.GetValue("Identity:Password:RequireUppercase", false);
            options.Password.RequireNonAlphanumeric = configuration.GetValue("Identity:Password:RequireNonAlphanumeric", false);
        });

        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        var googleCallbackUrl = GetGoogleCallbackUrl(configuration);
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            context.Services.AddAuthentication().AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = true;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.IsEssential = true;
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                if (googleCallbackUrl is not null)
                {
                    options.Events.OnRedirectToAuthorizationEndpoint = redirectContext =>
                    {
                        redirectContext.Response.Redirect(ReplaceOAuthRedirectUri(redirectContext.RedirectUri, googleCallbackUrl.AbsoluteUri));
                        return System.Threading.Tasks.Task.CompletedTask;
                    };
                }
                options.Events.OnRemoteFailure = failureContext =>
                {
                    var logger = failureContext.HttpContext.RequestServices
                        .GetRequiredService<Microsoft.Extensions.Logging.ILogger<WebHoanTienWebModule>>();
                    logger.LogWarning(failureContext.Failure,
                        "Google authentication callback failed. Path: {Path}",
                        failureContext.Request.Path);
                    failureContext.HandleResponse();
                    failureContext.Response.Redirect("/Account/Login?GoogleLoginError=callback");
                    return System.Threading.Tasks.Task.CompletedTask;
                };
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

    private static Uri? GetGoogleCallbackUrl(IConfiguration configuration)
    {
        var configuredUrl = configuration["Authentication:Google:CallbackUrl"];
        if (string.IsNullOrWhiteSpace(configuredUrl)) return null;

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var callbackUrl)
            || !string.Equals(callbackUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(callbackUrl.AbsolutePath, "/signin-google", StringComparison.Ordinal)
            || !string.IsNullOrEmpty(callbackUrl.Query)
            || !string.IsNullOrEmpty(callbackUrl.Fragment))
        {
            throw new AbpException("Authentication:Google:CallbackUrl must be an HTTPS URL ending in /signin-google.");
        }

        return callbackUrl;
    }

    private static string ReplaceOAuthRedirectUri(string authorizationEndpoint, string callbackUrl)
    {
        var endpoint = new Uri(authorizationEndpoint);
        var query = QueryHelpers.ParseQuery(endpoint.Query);
        var replacementQuery = new QueryBuilder();

        foreach (var parameter in query)
        {
            if (string.Equals(parameter.Key, "redirect_uri", StringComparison.OrdinalIgnoreCase))
            {
                replacementQuery.Add(parameter.Key, callbackUrl);
                continue;
            }

            foreach (var value in parameter.Value)
            {
                replacementQuery.Add(parameter.Key, value ?? string.Empty);
            }
        }

        var endpointBuilder = new UriBuilder(endpoint)
        {
            Query = replacementQuery.ToQueryString().Value?.TrimStart('?')
        };
        return endpointBuilder.Uri.AbsoluteUri;
    }

    private static void UseConfiguredGoogleCallbackUrl(IApplicationBuilder app, IConfiguration configuration)
    {
        var callbackUrl = GetGoogleCallbackUrl(configuration);
        if (callbackUrl is null) return;

        var publicHost = callbackUrl.IsDefaultPort
            ? new HostString(callbackUrl.Host)
            : new HostString(callbackUrl.Host, callbackUrl.Port);

        app.Use(async (httpContext, next) =>
        {
            if (string.Equals(httpContext.Request.Path.Value, callbackUrl.AbsolutePath, StringComparison.Ordinal))
            {
                httpContext.Request.Scheme = callbackUrl.Scheme;
                httpContext.Request.Host = publicHost;
            }

            await next();
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
                    bundle.AddFiles("/catback-modal.css");
                    bundle.AddFiles("/admin-payouts.css");
                    bundle.AddFiles("/admin-notifications.css");
                }
            );
            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle => bundle.AddFiles("/catback-modal.js")
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
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "CatsBack API", Version = "v1" });
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

        app.UseForwardedHeaders();
        UseConfiguredGoogleCallbackUrl(app, context.ServiceProvider.GetRequiredService<IConfiguration>());
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
            DashboardTitle = "CatsBack Jobs"
        });

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CatsBack API");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();

        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        var retentionCron = configuration["Affiliate:RetentionCron"] ?? "0 3 * * *";
        RecurringJob.AddOrUpdate<AffiliateRetentionJob>(
            "affiliate-retention-daily",
            job => job.ExecuteAsync(new AffiliateRetentionJobArgs()),
            retentionCron);
    }
}
