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
using WebHoanTien.Affiliates;

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
        context.Services.Configure<ShopeeAffiliateOptions>(configuration.GetSection(ShopeeAffiliateOptions.SectionName));
        context.Services.Configure<ShopeeOpenPlatformOptions>(configuration.GetSection(ShopeeOpenPlatformOptions.SectionName));
        context.Services.AddHttpClient("ShopeeAffiliate", client => client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Shopee:TimeoutSeconds", 15)));
        context.Services.AddHttpClient<IShopeeAmsPermissionChecker, ShopeeAmsPermissionChecker>(client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Shopee:OpenPlatform:TimeoutSeconds", 15), 1, 120)));
        context.Services.AddHttpClient("AffiliateRedirectResolver", client => client.Timeout = TimeSpan.FromSeconds(8))
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
        if (string.Equals(configuration["Affiliate:ProviderMode"], "Mock", StringComparison.OrdinalIgnoreCase))
            context.Services.AddTransient<IAffiliateProvider, MockShopeeAffiliateProvider>();
        else
            context.Services.AddTransient<IAffiliateProvider, ShopeeAffiliateProvider>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<WebHoanTienApplicationModule>();
        });
    }
}
