using AbpIoTemplateProject.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpIoTemplateProject.Permissions;

public class AbpIoTemplateProjectPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var education = context.AddGroup(AbpIoTemplateProjectPermissions.GroupName, L("Permission:Education"));

        DefineCrud(education, AbpIoTemplateProjectPermissions.Courses.Default, L("Permission:Courses"));
        DefineCrud(education, AbpIoTemplateProjectPermissions.Teachers.Default, L("Permission:Teachers"));
        education.AddPermission(AbpIoTemplateProjectPermissions.Classes.Default, L("Permission:Classes"))
            .AddChild(AbpIoTemplateProjectPermissions.Classes.Manage, L("Permission:Manage"));
        education.AddPermission(AbpIoTemplateProjectPermissions.Students.Default, L("Permission:Students"))
            .AddChild(AbpIoTemplateProjectPermissions.Students.Manage, L("Permission:Manage"));
        education.AddPermission(AbpIoTemplateProjectPermissions.Enrollments.Default, L("Permission:Enrollments"))
            .AddChild(AbpIoTemplateProjectPermissions.Enrollments.Manage, L("Permission:Manage"));
        education.AddPermission(AbpIoTemplateProjectPermissions.Leads.Default, L("Permission:Leads"))
            .AddChild(AbpIoTemplateProjectPermissions.Leads.Manage, L("Permission:Manage"));
        education.AddPermission(AbpIoTemplateProjectPermissions.PlacementTests.Default, L("Permission:PlacementTests"))
            .AddChild(AbpIoTemplateProjectPermissions.PlacementTests.Manage, L("Permission:Manage"));
        education.AddPermission(AbpIoTemplateProjectPermissions.Content.Default, L("Permission:Content"))
            .AddChild(AbpIoTemplateProjectPermissions.Content.Manage, L("Permission:Manage"));
    }

    private static void DefineCrud(PermissionGroupDefinition group, string permissionName, LocalizableString displayName)
    {
        var permission = group.AddPermission(permissionName, displayName);
        permission.AddChild(permissionName + ".Create", L("Permission:Create"));
        permission.AddChild(permissionName + ".Update", L("Permission:Update"));
        permission.AddChild(permissionName + ".Delete", L("Permission:Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpIoTemplateProjectResource>(name);
    }
}
