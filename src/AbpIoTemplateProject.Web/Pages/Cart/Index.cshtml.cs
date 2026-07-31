using System;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Cart;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly ICartAppService _cartAppService;

    public CartDto Cart { get; private set; } = new();

    public IndexModel(ICartAppService cartAppService)
    {
        _cartAppService = cartAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(Guid productId, Guid? variantId, int quantity = 1)
    {
        await _cartAppService.AddAsync(new AddCartItemInput
        {
            CartKey = GetOrCreateCartKey(),
            ProductId = productId,
            ProductVariantId = variantId,
            Quantity = Math.Max(1, quantity)
        });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid itemId, int quantity)
    {
        await _cartAppService.UpdateAsync(new UpdateCartItemInput
        {
            CartKey = GetOrCreateCartKey(),
            ItemId = itemId,
            Quantity = quantity
        });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid itemId)
    {
        await _cartAppService.RemoveAsync(GetOrCreateCartKey(), itemId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        await _cartAppService.ClearAsync(GetOrCreateCartKey());
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPromotionAsync(string? promotionCode)
    {
        await _cartAppService.ApplyPromotionAsync(new ApplyPromotionInput
        {
            CartKey = GetOrCreateCartKey(),
            PromotionCode = promotionCode
        });
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Cart = await _cartAppService.GetAsync(GetOrCreateCartKey());
        ViewData["StoreCartCount"] = Cart.TotalQuantity;
        ViewData["StoreCartTotal"] = Cart.GrandTotal;
    }
}
