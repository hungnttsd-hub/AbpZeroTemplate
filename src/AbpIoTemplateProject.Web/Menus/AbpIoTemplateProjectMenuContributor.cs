using System.Threading.Tasks;
using AbpIoTemplateProject.Localization;
using AbpIoTemplateProject.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace AbpIoTemplateProject.Web.Menus;

public class AbpIoTemplateProjectMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<AbpIoTemplateProjectResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                AbpIoTemplateProjectMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );

        context.Menu.Items.Add(new ApplicationMenuItem(
            "Education.Admin",
            "Quản trị IZONE",
            "~/Admin",
            icon: "fas fa-graduation-cap",
            requiredPermissionName: AbpIoTemplateProjectPermissions.Courses.Default,
            order: 1));

        administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);

        return Task.CompletedTask;
    }
}
