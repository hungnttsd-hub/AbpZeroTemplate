using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class OrdersModel : PageModel
{
    private readonly IAffiliateOrderAppService _orders;
    private readonly IAuthorizationService _authorizationService;

    public PagedResultDto<AffiliateOrderDto> Orders { get; private set; } = new();
    public IReadOnlyList<AffiliateOrderDto> AllOrders { get; private set; } = Array.Empty<AffiliateOrderDto>();
    public int PendingCount { get; private set; }
    public int ConfirmedCount { get; private set; }
    public decimal ExpectedCashback { get; private set; }
    public bool IsAdminOrderView { get; private set; }
    public string ReturnUrl => string.IsNullOrWhiteSpace(Status) ? "/Orders" : $"/Orders?status={Uri.EscapeDataString(Status)}";

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public OrdersModel(IAffiliateOrderAppService orders, IAuthorizationService authorizationService)
    {
        _orders = orders;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        IsAdminOrderView = (await _authorizationService.AuthorizeAsync(User, WebHoanTienPermissions.Admin.Orders)).Succeeded;
        var result = await _orders.GetListAsync(new AffiliateOrderListInput { MaxResultCount = 250 });
        AllOrders = result.Items;
        PendingCount = AllOrders.Count(order => CustomerOrderUi.IsPending(order.Status));
        ConfirmedCount = AllOrders.Count(order => CustomerOrderUi.IsConfirmed(order.Status));
        ExpectedCashback = AllOrders.Where(order => !CustomerOrderUi.IsCancelled(order.Status))
            .Sum(CustomerOrderUi.DisplayCommission);

        var normalizedStatus = Status?.Trim().ToLowerInvariant();
        var filtered = normalizedStatus switch
        {
            "pending" => AllOrders.Where(order => CustomerOrderUi.IsPending(order.Status)),
            "confirmed" => AllOrders.Where(order => CustomerOrderUi.IsConfirmed(order.Status)),
            "cancelled" => AllOrders.Where(order => CustomerOrderUi.IsCancelled(order.Status)),
            _ => AllOrders
        };
        var items = filtered.ToList();
        Orders = new PagedResultDto<AffiliateOrderDto>(items.Count, items);
    }

    public string FilterClass(string? value) => string.Equals(Status, value, StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(Status) && string.IsNullOrWhiteSpace(value) ? "is-active" : string.Empty;
}
