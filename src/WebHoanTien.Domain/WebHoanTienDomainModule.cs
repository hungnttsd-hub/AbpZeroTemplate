using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using WebHoanTien.IdentityExtensions;
using System;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.SettingManagement;
using Volo.Abp.Timing;

namespace WebHoanTien;

[DependsOn(
    typeof(WebHoanTienDomainSharedModule),
    typeof(AbpAuditLoggingDomainModule),
    typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpEmailingModule)
)]
public class WebHoanTienDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<Microsoft.AspNetCore.Identity.IUserValidator<Volo.Abp.Identity.IdentityUser>, CatBackUserValidator>();
        Configure<AbpClockOptions>(options => options.Kind = DateTimeKind.Utc);
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Languages.Add(new LanguageInfo("vi", "vi", "Tiếng Việt"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
        });
    }
}
