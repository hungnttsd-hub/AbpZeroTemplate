using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Security.Claims;
using WebHoanTien.TikTokAffiliate;
using WebHoanTien.Localization;
using WebHoanTien.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace WebHoanTien.Web.Menus;

public class WebHoanTienMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.Main)
        {
            return;
        }

        var l = context.GetLocalizer<WebHoanTienResource>();
        context.Menu.Items.Insert(0, new ApplicationMenuItem(
            WebHoanTienMenus.Home, l["Menu:Home"], "~/", "fas fa-home", 0));

        context.Menu.Items.Add(new ApplicationMenuItem(
            "Customer.Wallet", l["Menu:Wallet"], "~/Wallet", "fas fa-wallet", 10));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Customer.Orders", l["Menu:MyOrders"], "~/Orders", "fas fa-receipt", 20));
        var authorization = context.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = context.ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>().Principal;
        if ((await authorization.AuthorizeAsync(principal, TikTokAffiliateAccess.Policy)).Succeeded)
            context.Menu.Items.Add(new ApplicationMenuItem(
                "Customer.TikTokAffiliate", "Tiếp thị liên kết TikTok", "~/tiktok-affiliate", "fab fa-tiktok", 25));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Customer.Account", l["Menu:Account"], "~/Account/Profile", "fas fa-user", 30));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.Notifications", l["Menu:Notifications"], "~/Admin/Notifications", "fas fa-bell", 44,
            requiredPermissionName: WebHoanTienPermissions.Admin.Notifications));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.Settlements", l["Menu:Settlements"], "~/Admin/Settlements", "fas fa-file-invoice-dollar", 45,
            requiredPermissionName: WebHoanTienPermissions.Admin.Orders));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.Payouts", l["Menu:Payouts"], "~/Admin/Payouts", "fas fa-money-check-alt", 46,
            requiredPermissionName: WebHoanTienPermissions.Admin.Payouts));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.Wallets", l["Menu:UserWallets"], "~/Admin/Wallets", "fas fa-wallet", 47,
            requiredPermissionName: WebHoanTienPermissions.Admin.Payouts));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.EmailSettings", l["Menu:EmailSettings"], "~/Admin/Settings", "fas fa-envelope-open-text", 47,
            requiredPermissionName: WebHoanTienPermissions.Admin.Settings));
        context.Menu.Items.Add(new ApplicationMenuItem(
            "Affiliate.Admin", l["Menu:AffiliateAdmin"], "~/Admin/Affiliates", "fas fa-chart-line", 50,
            requiredPermissionName: WebHoanTienPermissions.Admin.Default));

        var administration = context.Menu.GetAdministration();
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 1);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 2);
    }
}
