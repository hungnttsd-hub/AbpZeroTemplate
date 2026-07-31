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

        context.Menu.AddItem(
            new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreAdmin,
                    "Cửa hàng",
                    icon: "fas fa-store",
                    order: 1)
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreProducts,
                    "Sản phẩm",
                    "/admin/store/products",
                    icon: "fas fa-box",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Products.View))
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreInventory,
                    "Tồn kho",
                    "/admin/store/inventory",
                    icon: "fas fa-warehouse",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Inventory.View))
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreOrders,
                    "Đơn hàng",
                    "/admin/store/orders",
                    icon: "fas fa-receipt",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Orders.View))
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreCustomers,
                    "Khách hàng",
                    "/admin/store/customers",
                    icon: "fas fa-users",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Customers.View))
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StorePayments,
                    "Thanh toán",
                    "/admin/store/payments",
                    icon: "fas fa-credit-card",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Payments.View))
                .AddItem(new ApplicationMenuItem(
                    AbpIoTemplateProjectMenus.StoreContent,
                    "Nội dung & ưu đãi",
                    "/admin/store/content",
                    icon: "fas fa-bullhorn",
                    requiredPermissionName: AbpIoTemplateProjectPermissions.Promotions.Default))
        );

        administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);

        return Task.CompletedTask;
    }
}
