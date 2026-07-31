using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AbpIoTemplateProject.Web.Pages.Checkout;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly ICartAppService _cartAppService;
    private readonly IOrderAppService _orderAppService;

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public CartDto Cart { get; private set; } = new();
    public List<ShippingMethodDto> ShippingMethods { get; private set; } = new();

    public IndexModel(ICartAppService cartAppService, IOrderAppService orderAppService)
    {
        _cartAppService = cartAppService;
        _orderAppService = orderAppService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Cart.Items.Count == 0)
        {
            return RedirectToPage("/Cart/Index");
        }

        Input.CartKey = GetOrCreateCartKey();
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        Input.FullName = CurrentUser.Name ?? string.Empty;
        Input.Email = CurrentUser.Email ?? string.Empty;
        Input.PaymentMethod = PaymentMethod.CashOnDelivery;
        Input.ShippingMethodId = ShippingMethods.Count > 0 ? ShippingMethods[0].Id : Guid.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.CartKey = GetOrCreateCartKey();
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        try
        {
            var order = await _orderAppService.CheckoutAsync(Input);
            return RedirectToPage("/Checkout/Success", new
            {
                orderNumber = order.OrderNumber,
                verification = order.Email
            });
        }
        catch (UserFriendlyException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync();
            return Page();
        }
    }

    private async Task LoadAsync()
    {
        Cart = await _cartAppService.GetAsync(GetOrCreateCartKey());
        ShippingMethods = await _orderAppService.GetShippingMethodsAsync();
        ViewData["StoreCartCount"] = Cart.TotalQuantity;
        ViewData["StoreCartTotal"] = Cart.GrandTotal;
    }
}
