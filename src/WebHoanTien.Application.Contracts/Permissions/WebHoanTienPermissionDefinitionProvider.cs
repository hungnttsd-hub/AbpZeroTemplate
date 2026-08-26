using WebHoanTien.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace WebHoanTien.Permissions;

public class WebHoanTienPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(WebHoanTienPermissions.GroupName, L("Permission:Affiliate"));
        var admin = group.AddPermission(WebHoanTienPermissions.Admin.Default, L("Permission:Admin"));
        admin.AddChild(WebHoanTienPermissions.Admin.Settings, L("Permission:Settings"));
        admin.AddChild(WebHoanTienPermissions.Admin.CommissionRules, L("Permission:CommissionRules"));
        admin.AddChild(WebHoanTienPermissions.Admin.Orders, L("Permission:Orders"));
        admin.AddChild(WebHoanTienPermissions.Admin.Sync, L("Permission:Sync"));
        admin.AddChild(WebHoanTienPermissions.Admin.ManualMatch, L("Permission:ManualMatch"));
        admin.AddChild(WebHoanTienPermissions.Admin.Payouts, L("Permission:Payouts"));
        admin.AddChild(WebHoanTienPermissions.Admin.Notifications, L("Permission:Notifications"));
    }

    private static LocalizableString L(string name) => LocalizableString.Create<WebHoanTienResource>(name);
}
