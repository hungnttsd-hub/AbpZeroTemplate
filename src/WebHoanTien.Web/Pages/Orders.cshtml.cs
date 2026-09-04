using System;
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
    public const int PageSize = 10;

    private readonly IAffiliateOrderAppService _orders;
    private readonly IAuthorizationService _authorizationService;

    public PagedResultDto<AffiliateOrderDto> Orders { get; private set; } = new();
    public int PendingCount { get; private set; }
    public int ConfirmedCount { get; private set; }
    public decimal ExpectedCashback { get; private set; }
    public bool IsAdminOrderView { get; private set; }
    public bool HasMore => Orders.Items.Count < Orders.TotalCount;
    public string ReturnUrl => string.IsNullOrWhiteSpace(Status) ? "/Orders" : $"/Orders?status={Uri.EscapeDataString(Status)}";
    public string MoreUrl => string.IsNullOrWhiteSpace(NormalizedStatus)
        ? "/Orders?handler=More"
        : $"/Orders?handler=More&status={Uri.EscapeDataString(NormalizedStatus)}";

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public OrdersModel(IAffiliateOrderAppService orders, IAuthorizationService authorizationService)
    {
        _orders = orders;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await LoadPageAsync(0);
        var summary = await _orders.GetSummaryAsync();
        PendingCount = summary.PendingCount;
        ConfirmedCount = summary.ConfirmedCount;
        ExpectedCashback = summary.ExpectedCashback;
    }

    public async Task<IActionResult> OnGetMoreAsync(int skip = 0)
    {
        skip = Math.Max(0, skip);
        await LoadPageAsync(skip);
        ViewData.Model = this;
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Has-More"] = (skip + Orders.Items.Count < Orders.TotalCount).ToString().ToLowerInvariant();
        Response.Headers["X-Total-Count"] = Orders.TotalCount.ToString();

        return new PartialViewResult
        {
            ViewName = "/Pages/Shared/_OrderCards.cshtml",
            ViewData = ViewData
        };
    }

    public string FilterClass(string? value) => string.Equals(Status, value, StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(Status) && string.IsNullOrWhiteSpace(value) ? "is-active" : string.Empty;

    private string? NormalizedStatus => Status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "pending",
        "confirmed" => "confirmed",
        "cancelled" => "cancelled",
        _ => null
    };

    private async Task LoadPageAsync(int skip)
    {
        IsAdminOrderView = (await _authorizationService.AuthorizeAsync(User, WebHoanTienPermissions.Admin.Orders)).Succeeded;
        Orders = await _orders.GetListAsync(new AffiliateOrderListInput
        {
            SkipCount = skip,
            MaxResultCount = PageSize,
            StatusGroup = NormalizedStatus
        });
    }
}
