using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Store;

[AllowAnonymous]
public class CartAppService : AbpIoTemplateProjectAppService, ICartAppService
{
    private readonly IRepository<ShoppingCart, Guid> _cartRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<InventoryItem, Guid> _inventoryRepository;
    private readonly IRepository<Promotion, Guid> _promotionRepository;
    private readonly IRepository<ShippingMethod, Guid> _shippingMethodRepository;

    public CartAppService(
        IRepository<ShoppingCart, Guid> cartRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<InventoryItem, Guid> inventoryRepository,
        IRepository<Promotion, Guid> promotionRepository,
        IRepository<ShippingMethod, Guid> shippingMethodRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _promotionRepository = promotionRepository;
        _shippingMethodRepository = shippingMethodRepository;
    }

    public async Task<CartDto> GetAsync(string cartKey)
    {
        ValidateCartKey(cartKey);
        var cart = await FindCartAsync(cartKey);
        return cart is null ? EmptyCart(cartKey) : await BuildCartDtoAsync(cart);
    }

    public async Task<CartDto> AddAsync(AddCartItemInput input)
    {
        ValidateCartKey(input.CartKey);
        var product = await GetProductAsync(input.ProductId);
        var selection = ResolveSelection(product, input.ProductVariantId);
        var cart = await FindCartAsync(input.CartKey) ?? new ShoppingCart(
            GuidGenerator.Create(),
            input.CartKey,
            CurrentUser.Id,
            CurrentTenant.Id);

        if (CurrentUser.Id.HasValue && !cart.UserId.HasValue)
        {
            cart.AssignToUser(CurrentUser.Id.Value);
        }

        var existingQuantity = cart.Items
            .Where(x => x.ProductId == input.ProductId && x.ProductVariantId == input.ProductVariantId)
            .Sum(x => x.Quantity);
        var maximum = await GetMaximumPurchasableQuantityAsync(product, input.ProductVariantId);
        var desiredQuantity = existingQuantity + input.Quantity;
        if (desiredQuantity < product.MinimumOrderQuantity || desiredQuantity > maximum)
        {
            throw new UserFriendlyException(L["Store:InvalidCartQuantity", maximum]);
        }

        cart.AddOrUpdateItem(
            GuidGenerator.Create(),
            product.Id,
            selection.Variant?.Id,
            input.Quantity,
            selection.Price,
            product.Name,
            selection.Variant?.Sku ?? product.Sku,
            selection.Variant?.OptionSummary,
            selection.Variant?.ImageUrl ?? product.ThumbnailUrl);

        if (await _cartRepository.AnyAsync(x => x.Id == cart.Id))
        {
            await _cartRepository.UpdateAsync(cart, autoSave: true);
        }
        else
        {
            await _cartRepository.InsertAsync(cart, autoSave: true);
        }

        return await BuildCartDtoAsync(cart);
    }

    public async Task<CartDto> UpdateAsync(UpdateCartItemInput input)
    {
        ValidateCartKey(input.CartKey);
        var cart = await GetCartAsync(input.CartKey);
        var item = cart.Items.FirstOrDefault(x => x.Id == input.ItemId)
                   ?? throw new UserFriendlyException(L["Store:CartItemNotFound"]);
        var product = await GetProductAsync(item.ProductId);
        var maximum = await GetMaximumPurchasableQuantityAsync(product, item.ProductVariantId);
        if (input.Quantity < product.MinimumOrderQuantity || input.Quantity > maximum)
        {
            throw new UserFriendlyException(L["Store:InvalidCartQuantity", maximum]);
        }

        cart.UpdateItem(input.ItemId, input.Quantity);
        await _cartRepository.UpdateAsync(cart, autoSave: true);
        return await BuildCartDtoAsync(cart);
    }

    public async Task<CartDto> RemoveAsync(string cartKey, Guid itemId)
    {
        ValidateCartKey(cartKey);
        var cart = await GetCartAsync(cartKey);
        cart.RemoveItem(itemId);
        await _cartRepository.UpdateAsync(cart, autoSave: true);
        return await BuildCartDtoAsync(cart);
    }

    public async Task<CartDto> ClearAsync(string cartKey)
    {
        ValidateCartKey(cartKey);
        var cart = await FindCartAsync(cartKey);
        if (cart is null)
        {
            return EmptyCart(cartKey);
        }

        cart.Clear();
        await _cartRepository.UpdateAsync(cart, autoSave: true);
        return await BuildCartDtoAsync(cart);
    }

    public async Task<CartDto> ApplyPromotionAsync(ApplyPromotionInput input)
    {
        ValidateCartKey(input.CartKey);
        var cart = await GetCartAsync(input.CartKey);
        if (input.PromotionCode.IsNullOrWhiteSpace())
        {
            cart.ApplyPromotion(null);
        }
        else
        {
            var code = input.PromotionCode!.Trim().ToUpperInvariant();
            var promotion = await _promotionRepository.FindAsync(x => x.Code == code && x.IsActive);
            if (promotion is null)
            {
                throw new UserFriendlyException(L["Store:PromotionNotFound"]);
            }

            var subtotal = cart.Items.Sum(x => x.UnitPrice * x.Quantity);
            promotion.CalculateDiscount(subtotal, Clock.Now);
            cart.ApplyPromotion(code);
        }

        await _cartRepository.UpdateAsync(cart, autoSave: true);
        return await BuildCartDtoAsync(cart);
    }

    private async Task<ShoppingCart?> FindCartAsync(string cartKey)
    {
        var query = await _cartRepository.WithDetailsAsync(x => x.Items);
        return await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.CartKey == cartKey && !x.IsConverted));
    }

    private async Task<ShoppingCart> GetCartAsync(string cartKey)
    {
        return await FindCartAsync(cartKey)
               ?? throw new UserFriendlyException(L["Store:CartNotFound"]);
    }

    private async Task<Product> GetProductAsync(Guid productId)
    {
        var query = await _productRepository.WithDetailsAsync(x => x.Variants, x => x.Images);
        var product = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.Id == productId && x.IsActive && x.IsVisible));
        return product ?? throw new UserFriendlyException(L["Store:ProductNotFound"]);
    }

    private static (ProductVariant? Variant, decimal Price) ResolveSelection(Product product, Guid? variantId)
    {
        ProductVariant? variant = null;
        if (product.Type == ProductType.Variant)
        {
            if (!variantId.HasValue)
            {
                throw new UserFriendlyException("Vui lòng chọn phiên bản sản phẩm.");
            }

            variant = product.Variants.FirstOrDefault(x => x.Id == variantId.Value && x.IsActive)
                      ?? throw new UserFriendlyException("Phiên bản sản phẩm không tồn tại hoặc đã ngừng bán.");
        }
        else if (variantId.HasValue)
        {
            throw new UserFriendlyException("Sản phẩm này không có phiên bản.");
        }

        var price = variant?.SalePrice ?? product.SalePrice ?? variant?.ListPrice ?? product.ListPrice;
        if (!price.HasValue)
        {
            throw new UserFriendlyException("Sản phẩm chưa được cấu hình giá bán.");
        }

        return (variant, price.Value);
    }

    private async Task<int> GetMaximumPurchasableQuantityAsync(Product product, Guid? variantId)
    {
        if (product.AllowBackorder)
        {
            return product.MaximumOrderQuantity;
        }

        var inventory = await _inventoryRepository.GetListAsync(x =>
            x.ProductId == product.Id && x.ProductVariantId == variantId);
        var available = inventory.Sum(x => x.AvailableQuantity);
        if (available < product.MinimumOrderQuantity)
        {
            throw new UserFriendlyException(L["Store:OutOfStock"]);
        }

        return Math.Min(product.MaximumOrderQuantity, available);
    }

    private async Task<CartDto> BuildCartDtoAsync(ShoppingCart cart)
    {
        var dto = new CartDto
        {
            Id = cart.Id,
            CartKey = cart.CartKey,
            PromotionCode = cart.PromotionCode
        };

        if (cart.Items.Count == 0)
        {
            return dto;
        }

        var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.WithDetailsAsync(x => x.Variants);
        var products = await AsyncExecuter.ToListAsync(productQuery.Where(x => productIds.Contains(x.Id)));
        var productMap = products.ToDictionary(x => x.Id);
        var inventory = await _inventoryRepository.GetListAsync(x => productIds.Contains(x.ProductId));

        foreach (var item in cart.Items.OrderBy(x => x.CreationTime))
        {
            productMap.TryGetValue(item.ProductId, out var product);
            var variant = item.ProductVariantId.HasValue
                ? product?.Variants.FirstOrDefault(x => x.Id == item.ProductVariantId.Value)
                : null;
            var available = inventory
                .Where(x => x.ProductId == item.ProductId && x.ProductVariantId == item.ProductVariantId)
                .Sum(x => x.AvailableQuantity);
            var maximum = product is null
                ? 0
                : product.AllowBackorder
                    ? product.MaximumOrderQuantity
                    : Math.Min(product.MaximumOrderQuantity, available);
            var currentPrice = variant?.SalePrice ?? product?.SalePrice ?? variant?.ListPrice ?? product?.ListPrice ?? item.UnitPrice;

            dto.Items.Add(new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                ProductName = product?.Name ?? item.ProductName,
                Slug = product?.Slug ?? string.Empty,
                Sku = variant?.Sku ?? product?.Sku ?? item.Sku,
                OptionSummary = variant?.OptionSummary ?? item.OptionSummary,
                ImageUrl = variant?.ImageUrl ?? product?.ThumbnailUrl ?? item.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = currentPrice,
                LineTotal = currentPrice * item.Quantity,
                IsAvailable = product is { IsActive: true, IsVisible: true } &&
                              (product.AllowBackorder || available >= item.Quantity),
                MaximumPurchasableQuantity = maximum
            });
        }

        dto.Subtotal = dto.Items.Sum(x => x.LineTotal);
        if (!cart.PromotionCode.IsNullOrWhiteSpace())
        {
            var promotion = await _promotionRepository.FindAsync(x =>
                x.Code == cart.PromotionCode && x.IsActive);
            if (promotion is not null)
            {
                try
                {
                    dto.DiscountAmount = promotion.CalculateDiscount(dto.Subtotal, Clock.Now);
                }
                catch (BusinessException)
                {
                    dto.DiscountAmount = 0;
                }
            }
        }

        var shippingMethod = (await _shippingMethodRepository.GetListAsync(x => x.IsActive))
            .OrderBy(x => x.Fee)
            .FirstOrDefault();
        dto.EstimatedShippingFee = shippingMethod?.Fee ?? 0;
        dto.GrandTotal = dto.Subtotal + dto.EstimatedShippingFee - dto.DiscountAmount;
        dto.TotalQuantity = dto.Items.Sum(x => x.Quantity);
        return dto;
    }

    private static CartDto EmptyCart(string cartKey)
    {
        return new CartDto { CartKey = cartKey };
    }

    private static void ValidateCartKey(string cartKey)
    {
        Check.NotNullOrWhiteSpace(cartKey, nameof(cartKey), StoreConsts.MaxCodeLength);
    }
}
