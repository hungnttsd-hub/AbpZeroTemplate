using AbpIoTemplateProject.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpIoTemplateProject.Permissions;

public class AbpIoTemplateProjectPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AbpIoTemplateProjectPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AbpIoTemplateProjectPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpIoTemplateProjectResource>(name);
    }
}
