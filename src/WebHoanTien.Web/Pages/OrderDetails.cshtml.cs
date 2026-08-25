using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class OrderDetailsModel : PageModel
{
    private readonly IAffiliateOrderAppService _orders;
    private readonly IAuthorizationService _authorizationService;

    public AffiliateOrderDto Order { get; private set; } = null!;
    public string ReturnUrl { get; private set; } = "/Orders";
    public bool IsAdminOrderView { get; private set; }

    public OrderDetailsModel(IAffiliateOrderAppService orders, IAuthorizationService authorizationService)
    {
        _orders = orders;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync(Guid id, string? returnUrl)
    {
        IsAdminOrderView = (await _authorizationService.AuthorizeAsync(User, WebHoanTienPermissions.Admin.Orders)).Succeeded;
        Order = await _orders.GetAsync(id);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) ReturnUrl = returnUrl;
    }
}
