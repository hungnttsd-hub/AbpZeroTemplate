using Microsoft.Extensions.Configuration;
using Volo.Abp.Emailing;
using Volo.Abp.Identity.Settings;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace WebHoanTien.Settings;

public class WebHoanTienSettingDefinitionProvider : SettingDefinitionProvider
{
    private readonly IConfiguration _configuration;
    public WebHoanTienSettingDefinitionProvider(IConfiguration configuration) => _configuration = configuration;

    public override void Define(ISettingDefinitionContext context)
    {
        SetDefault(context, LocalizationSettingNames.DefaultLanguage, "vi");
        SetDefault(context, EmailSettingNames.DefaultFromAddress, _configuration["Smtp:FromAddress"]);
        SetDefault(context, EmailSettingNames.DefaultFromDisplayName, _configuration["Smtp:FromName"] ?? "CatsBack");
        SetDefault(context, IdentitySettingNames.Password.RequiredLength, _configuration["Identity:Password:RequiredLength"] ?? "6");
        SetDefault(context, IdentitySettingNames.Password.RequiredUniqueChars, _configuration["Identity:Password:RequiredUniqueChars"] ?? "0");
        SetDefault(context, IdentitySettingNames.Password.RequireDigit, _configuration["Identity:Password:RequireDigit"] ?? "false");
        SetDefault(context, IdentitySettingNames.Password.RequireLowercase, _configuration["Identity:Password:RequireLowercase"] ?? "false");
        SetDefault(context, IdentitySettingNames.Password.RequireUppercase, _configuration["Identity:Password:RequireUppercase"] ?? "false");
        SetDefault(context, IdentitySettingNames.Password.RequireNonAlphanumeric, _configuration["Identity:Password:RequireNonAlphanumeric"] ?? "false");
        SetDefault(context, EmailSettingNames.Smtp.Host, _configuration["Smtp:Host"]);
        SetDefault(context, EmailSettingNames.Smtp.Port, _configuration["Smtp:Port"] ?? "587");
        SetDefault(context, EmailSettingNames.Smtp.UserName, _configuration["Smtp:Username"]);
        SetDefault(context, EmailSettingNames.Smtp.Password, _configuration["Smtp:Password"]);
        SetDefault(context, EmailSettingNames.Smtp.EnableSsl, _configuration["Smtp:EnableSsl"] ?? "true");
        SetDefault(context, EmailSettingNames.Smtp.UseDefaultCredentials, "false");
    }

    private static void SetDefault(ISettingDefinitionContext context, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && context.GetOrNull(name) is { } definition) definition.DefaultValue = value;
    }
}
