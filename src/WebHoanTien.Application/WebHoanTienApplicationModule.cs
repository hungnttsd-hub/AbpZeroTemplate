using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using WebHoanTien.Integrations;
using WebHoanTien.Integrations.Shopee;
using WebHoanTien.Integrations.RioHub;
using System.Linq;
using WebHoanTien.Affiliates;
using WebHoanTien.Admin;
using WebHoanTien.TikTokAffiliate;
using Microsoft.AspNetCore.Authorization;

namespace WebHoanTien;

[DependsOn(
    typeof(WebHoanTienDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(WebHoanTienApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class WebHoanTienApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddOptions<RioHubOptions>()
            .Bind(configuration.GetSection(RioHubOptions.SectionName))
            .PostConfigure(options =>
            {
                // Respect configuration precedence, including appsettings.secrets.json.
                // Keep the original Docker environment variable as a fallback.
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    options.ApiKey = Environment.GetEnvironmentVariable("RIOHUB_API_KEY") ?? string.Empty;
            })
            .Validate(options => RioHubOptions.IsAllowedBaseUrl(options.BaseUrl) &&
                options.FallbackBaseUrls is not null && options.FallbackBaseUrls.All(RioHubOptions.IsAllowedBaseUrl),
                "RioHub chỉ chấp nhận HTTPS /api/v1 trên ba tên miền đã xác minh.")
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 120 &&
                options.MaxRateLimitRetries is >= 0 and <= 5 && options.MaxRetryAfterSeconds is >= 0 and <= 300,
                "Cấu hình timeout/retry RioHub không hợp lệ.")
            .Validate(options => !options.ApiKey.Any(char.IsControl), "RioHub API key chứa ký tự không hợp lệ.")
            .Validate(options => options.InitialSyncLookbackDays is >= 1 and <= 3650 &&
                (!options.InitialSyncFromUnix.HasValue || options.InitialSyncFromUnix is >= 0 and <= 253402300799),
                "RioHub: khoảng đồng bộ ban đầu không hợp lệ.")
            .Validate(options => !options.SyncEnabled || options.Enabled && !string.IsNullOrWhiteSpace(options.ApiKey) &&
                System.Text.RegularExpressions.Regex.IsMatch(options.CreatorUsername, @"\A[A-Za-z0-9._]{1,100}\z"),
                "Bật sync RioHub cần Enabled, API key và CreatorUsername hợp lệ.");
        context.Services.AddHttpClient(RioHubAffiliateClient.HttpClientName, (provider, client) =>
            {
                var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RioHubOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .RedactLoggedHeaders(new[] { "X-Riohub-Api-Key" })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                MaxResponseHeadersLength = 32
            });
        context.Services.AddSingleton<IRioHubAffiliateClient, RioHubAffiliateClient>();
        Configure<AuthorizationOptions>(options => options.AddPolicy(TikTokAffiliateAccess.Policy,
            policy => policy.RequireAuthenticatedUser().AddRequirements(new TikTokAffiliateAccessRequirement())));
        context.Services.AddScoped<IAuthorizationHandler, TikTokAffiliateAuthorizationHandler>();
        context.Services.AddMemoryCache();
        // Customer workspace always uses RioHub; a legacy Mock setting must never select fixtures.
        context.Services.AddTransient<ITikTokAffiliateService, TikTokAffiliateRioHubService>();
        context.Services.Configure<ShopeeAffiliateOptions>(configuration.GetSection(ShopeeAffiliateOptions.SectionName));
        context.Services.AddHttpClient("ShopeeProductData", client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Shopee:ProductDataTimeoutSeconds", 10), 1, 120)));
        context.Services.AddHttpClient("ShopeeShopMetadata", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Shopee:ShopMetadataTimeoutSeconds", 4), 1, 15));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; CatsBack-ShopMetadata/1.0)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        var redirectTimeoutSeconds = Math.Clamp(configuration.GetValue("Affiliate:RedirectTimeoutSeconds", 8), 1, 120);
        context.Services.AddHttpClient("AffiliateRedirectResolver", client =>
                client.Timeout = TimeSpan.FromSeconds(redirectTimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                MaxResponseHeadersLength = 32,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var addresses = await AffiliateNetworkSafety.ResolvePublicAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                    Exception? lastError = null;
                    foreach (var address in addresses)
                    {
                        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch (Exception exception)
                        {
                            lastError = exception;
                            socket.Dispose();
                        }
                    }
                    throw new HttpRequestException("Không thể kết nối tới địa chỉ Shopee đã kiểm tra.", lastError);
                }
            });
        context.Services.AddTransient<ShopeeAffiliateLinkBuilder>();
        context.Services.AddTransient<ShopeeShopMetadataProvider>();
        context.Services.AddTransient<ShopeeReportImporter>();
        context.Services.AddTransient<IAdminShopeeReportImportAppService, ShopeeReportImportAppService>();
        context.Services.AddTransient<IShopeeAutomationImportAppService, ShopeeAutomationImportAppService>();
        context.Services.AddTransient<IAdminShopeeSettlementImportAppService, ShopeeSettlementImportAppService>();
        context.Services.AddTransient<IShopeeAutomationSettlementImportAppService, ShopeeAutomationSettlementImportAppService>();
        context.Services.AddTransient<IAdminShopeeSettlementApprovalAppService, AdminShopeeSettlementApprovalAppService>();
        if (string.Equals(configuration["Affiliate:ProviderMode"], "Mock", StringComparison.OrdinalIgnoreCase))
            context.Services.AddTransient<IAffiliateProvider, MockShopeeAffiliateProvider>();
        else
            context.Services.AddTransient<IAffiliateProvider, ShopeeAddLiveTagProductDataProvider>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<WebHoanTienApplicationModule>();
        });
    }
}
