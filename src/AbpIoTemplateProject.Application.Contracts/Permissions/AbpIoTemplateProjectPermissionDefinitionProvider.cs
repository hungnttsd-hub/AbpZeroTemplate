using AbpIoTemplateProject.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpIoTemplateProject.Permissions;

public class AbpIoTemplateProjectPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var store = context.AddGroup(AbpIoTemplateProjectPermissions.GroupName, L("Permission:Store"));

        var products = store.AddPermission(AbpIoTemplateProjectPermissions.Products.Default, L("Permission:Products"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.View, L("Permission:View"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.Create, L("Permission:Create"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.Update, L("Permission:Update"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.Delete, L("Permission:Delete"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.ManageImages, L("Permission:ManageImages"));
        products.AddChild(AbpIoTemplateProjectPermissions.Products.ManagePrice, L("Permission:ManagePrice"));

        store.AddPermission(AbpIoTemplateProjectPermissions.Categories.Default, L("Permission:Categories"));
        store.AddPermission(AbpIoTemplateProjectPermissions.Brands.Default, L("Permission:Brands"));
        store.AddPermission(AbpIoTemplateProjectPermissions.Suppliers.Default, L("Permission:Suppliers"));

        var inventory = store.AddPermission(AbpIoTemplateProjectPermissions.Inventory.Default, L("Permission:Inventory"));
        inventory.AddChild(AbpIoTemplateProjectPermissions.Inventory.View, L("Permission:View"));
        inventory.AddChild(AbpIoTemplateProjectPermissions.Inventory.Receive, L("Permission:Receive"));
        inventory.AddChild(AbpIoTemplateProjectPermissions.Inventory.Issue, L("Permission:Issue"));
        inventory.AddChild(AbpIoTemplateProjectPermissions.Inventory.Adjust, L("Permission:Adjust"));
        inventory.AddChild(AbpIoTemplateProjectPermissions.Inventory.Transfer, L("Permission:Transfer"));

        var customers = store.AddPermission(AbpIoTemplateProjectPermissions.Customers.Default, L("Permission:Customers"));
        customers.AddChild(AbpIoTemplateProjectPermissions.Customers.View, L("Permission:View"));
        customers.AddChild(AbpIoTemplateProjectPermissions.Customers.Update, L("Permission:Update"));

        var orders = store.AddPermission(AbpIoTemplateProjectPermissions.Orders.Default, L("Permission:Orders"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.View, L("Permission:View"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Confirm, L("Permission:Confirm"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Prepare, L("Permission:Prepare"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Ship, L("Permission:Ship"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Complete, L("Permission:Complete"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Cancel, L("Permission:Cancel"));
        orders.AddChild(AbpIoTemplateProjectPermissions.Orders.Return, L("Permission:Return"));

        var payments = store.AddPermission(AbpIoTemplateProjectPermissions.Payments.Default, L("Permission:Payments"));
        payments.AddChild(AbpIoTemplateProjectPermissions.Payments.View, L("Permission:View"));
        payments.AddChild(AbpIoTemplateProjectPermissions.Payments.Confirm, L("Permission:ConfirmPayment"));
        payments.AddChild(AbpIoTemplateProjectPermissions.Payments.Refund, L("Permission:Refund"));

        store.AddPermission(AbpIoTemplateProjectPermissions.Promotions.Default, L("Permission:Promotions"));
        store.AddPermission(AbpIoTemplateProjectPermissions.Banners.Default, L("Permission:Banners"));
        store.AddPermission(AbpIoTemplateProjectPermissions.Articles.Default, L("Permission:Articles"));
        store.AddPermission(AbpIoTemplateProjectPermissions.Settings.Default, L("Permission:Settings"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpIoTemplateProjectResource>(name);
    }
}
