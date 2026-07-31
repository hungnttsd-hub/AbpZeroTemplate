using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Store;

public class OrderAppService : AbpIoTemplateProjectAppService, IOrderAppService
{
    private readonly IRepository<ShoppingCart, Guid> _cartRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<InventoryItem, Guid> _inventoryRepository;
    private readonly IRepository<InventoryTransaction, Guid> _inventoryTransactionRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<ShippingMethod, Guid> _shippingMethodRepository;
    private readonly IRepository<Promotion, Guid> _promotionRepository;
    private readonly IRepository<PromotionUsage, Guid> _promotionUsageRepository;
    private readonly IEnumerable<IStorePaymentGateway> _paymentGateways;

    public OrderAppService(
        IRepository<ShoppingCart, Guid> cartRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<InventoryItem, Guid> inventoryRepository,
        IRepository<InventoryTransaction, Guid> inventoryTransactionRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<ShippingMethod, Guid> shippingMethodRepository,
        IRepository<Promotion, Guid> promotionRepository,
        IRepository<PromotionUsage, Guid> promotionUsageRepository,
        IEnumerable<IStorePaymentGateway> paymentGateways)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _inventoryTransactionRepository = inventoryTransactionRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _promotionRepository = promotionRepository;
        _promotionUsageRepository = promotionUsageRepository;
        _paymentGateways = paymentGateways;
    }

    [AllowAnonymous]
    public async Task<List<ShippingMethodDto>> GetShippingMethodsAsync()
    {
        return (await _shippingMethodRepository.GetListAsync(x => x.IsActive))
            .OrderBy(x => x.Fee)
            .Select(x => new ShippingMethodDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                Fee = x.Fee,
                EstimatedDays = x.EstimatedDays
            }).ToList();
    }

    [AllowAnonymous]
    public async Task<OrderDto> CheckoutAsync(CheckoutInput input)
    {
        var idempotencyKey = Check.NotNullOrWhiteSpace(
            input.IdempotencyKey,
            nameof(input.IdempotencyKey),
            StoreConsts.MaxCodeLength);
        var existing = await _orderRepository.FindAsync(x => x.IdempotencyKey == idempotencyKey);
        if (existing is not null)
        {
            return await GetOrderDtoAsync(existing.Id);
        }

        var cartQuery = await _cartRepository.WithDetailsAsync(x => x.Items);
        var cart = await AsyncExecuter.FirstOrDefaultAsync(
            cartQuery.Where(x => x.CartKey == input.CartKey && !x.IsConverted));
        if (cart is null || cart.Items.Count == 0)
        {
            throw new UserFriendlyException(L["Store:CartIsEmpty"]);
        }

        var shippingMethod = await _shippingMethodRepository.FindAsync(x =>
            x.Id == input.ShippingMethodId && x.IsActive);
        if (shippingMethod is null)
        {
            throw new UserFriendlyException(L["Store:ShippingMethodNotFound"]);
        }

        var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.WithDetailsAsync(x => x.Variants);
        var products = await AsyncExecuter.ToListAsync(
            productQuery.Where(x => productIds.Contains(x.Id) && x.IsActive && x.IsVisible));
        if (products.Count != productIds.Count)
        {
            throw new UserFriendlyException(L["Store:CartContainsUnavailableProduct"]);
        }

        var productMap = products.ToDictionary(x => x.Id);
        var inventories = await _inventoryRepository.GetListAsync(x => productIds.Contains(x.ProductId));
        var validatedItems = new List<ValidatedOrderItem>();
        foreach (var cartItem in cart.Items)
        {
            var product = productMap[cartItem.ProductId];
            var variant = cartItem.ProductVariantId.HasValue
                ? product.Variants.FirstOrDefault(x => x.Id == cartItem.ProductVariantId.Value && x.IsActive)
                : null;
            if (product.Type == ProductType.Variant && variant is null)
            {
                throw new UserFriendlyException(L["Store:CartContainsUnavailableProduct"]);
            }

            var price = variant?.SalePrice ?? product.SalePrice ?? variant?.ListPrice ?? product.ListPrice;
            if (!price.HasValue)
            {
                throw new UserFriendlyException(L["Store:ProductPriceMissing"]);
            }

            var matchingInventory = inventories
                .Where(x => x.ProductId == product.Id && x.ProductVariantId == cartItem.ProductVariantId)
                .OrderByDescending(x => x.AvailableQuantity)
                .ToList();
            var available = matchingInventory.Sum(x => x.AvailableQuantity);
            if ((!product.AllowBackorder && available < cartItem.Quantity) ||
                cartItem.Quantity < product.MinimumOrderQuantity ||
                cartItem.Quantity > product.MaximumOrderQuantity)
            {
                throw new UserFriendlyException(L["Store:InsufficientInventoryForProduct", product.Name]);
            }

            validatedItems.Add(new ValidatedOrderItem(product, variant, cartItem.Quantity, price.Value, matchingInventory));
        }

        var customer = await FindOrCreateCustomerAsync(input);
        var order = new Order(
            GuidGenerator.Create(),
            CreateOrderNumber(),
            idempotencyKey,
            customer.Id,
            CurrentUser.Id,
            input.FullName.Trim(),
            NormalizePhone(input.Phone),
            input.Email.Trim().ToLowerInvariant(),
            input.Province.Trim(),
            input.District.Trim(),
            input.Ward.Trim(),
            input.AddressLine.Trim(),
            input.DeliveryNote?.Trim(),
            shippingMethod.Id,
            shippingMethod.Name,
            input.PaymentMethod,
            CurrentTenant.Id);

        foreach (var item in validatedItems)
        {
            order.AddItem(
                GuidGenerator.Create(),
                item.Product.Id,
                item.Variant?.Id,
                item.Product.Name,
                item.Variant?.Sku ?? item.Product.Sku,
                item.Variant?.OptionSummary,
                item.Variant?.ImageUrl ?? item.Product.ThumbnailUrl,
                item.Quantity,
                item.UnitPrice,
                item.Product.TaxRate);
        }

        var subtotal = validatedItems.Sum(x => x.UnitPrice * x.Quantity);
        var (promotion, discount) = await ResolvePromotionAsync(cart.PromotionCode, subtotal, customer.Id);
        order.CalculateTotals(discount, shippingMethod.Fee, promotion?.Code);

        foreach (var item in validatedItems.Where(x => !x.Product.AllowBackorder))
        {
            var remaining = item.Quantity;
            foreach (var inventory in item.Inventories)
            {
                if (remaining == 0)
                {
                    break;
                }

                var reserved = Math.Min(remaining, inventory.AvailableQuantity);
                if (reserved == 0)
                {
                    continue;
                }

                var before = inventory.AvailableQuantity;
                inventory.Reserve(reserved);
                await _inventoryRepository.UpdateAsync(inventory);
                await _inventoryTransactionRepository.InsertAsync(new InventoryTransaction(
                    GuidGenerator.Create(),
                    inventory.Id,
                    InventoryTransactionType.Reserve,
                    before,
                    -reserved,
                    inventory.AvailableQuantity,
                    "Order",
                    order.OrderNumber,
                    $"Giữ tồn cho đơn {order.OrderNumber}",
                    CurrentTenant.Id));
                remaining -= reserved;
            }
        }

        await _orderRepository.InsertAsync(order);
        var payment = new Payment(
            GuidGenerator.Create(),
            order.Id,
            input.PaymentMethod,
            order.GrandTotal,
            CurrentTenant.Id);
        if (input.PaymentMethod != PaymentMethod.CashOnDelivery)
        {
            var gateway = _paymentGateways.FirstOrDefault(x => x.CanHandle(input.PaymentMethod))
                          ?? throw new UserFriendlyException(L["Store:PaymentGatewayNotConfigured"]);
            var gatewayResult = await gateway.InitializeAsync(new PaymentGatewayRequest
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Method = input.PaymentMethod,
                Amount = order.GrandTotal,
                CustomerEmail = order.Email
            });
            payment.SetPendingReference(gatewayResult.Reference);
        }
        await _paymentRepository.InsertAsync(payment);

        if (promotion is not null)
        {
            promotion.RecordUsage();
            await _promotionRepository.UpdateAsync(promotion);
            await _promotionUsageRepository.InsertAsync(new PromotionUsage(
                GuidGenerator.Create(),
                promotion.Id,
                order.Id,
                customer.Id,
                discount,
                CurrentTenant.Id));
        }

        cart.MarkConverted();
        await _cartRepository.UpdateAsync(cart, autoSave: true);
        return await GetOrderDtoAsync(order.Id);
    }

    [AllowAnonymous]
    public async Task<OrderDto> TrackAsync(TrackOrderInput input)
    {
        var orderNumber = Check.NotNullOrWhiteSpace(input.OrderNumber, nameof(input.OrderNumber)).Trim().ToUpperInvariant();
        var verification = Check.NotNullOrWhiteSpace(input.Verification, nameof(input.Verification)).Trim();
        var order = await _orderRepository.FindAsync(x => x.OrderNumber == orderNumber);
        if (order is null ||
            (!string.Equals(order.Email, verification, StringComparison.OrdinalIgnoreCase) &&
             NormalizePhone(order.Phone) != NormalizePhone(verification)))
        {
            throw new UserFriendlyException(L["Store:OrderTrackingInformationInvalid"]);
        }

        return await GetOrderDtoAsync(order.Id);
    }

    [Authorize]
    public async Task<List<OrderDto>> GetMyOrdersAsync()
    {
        var userId = CurrentUser.Id
                     ?? throw new UserFriendlyException(L["Store:AuthenticationRequired"]);
        var orders = (await _orderRepository.GetListAsync(x => x.UserId == userId))
            .OrderByDescending(x => x.CreationTime)
            .ToList();
        var result = new List<OrderDto>();
        foreach (var order in orders)
        {
            result.Add(await GetOrderDtoAsync(order.Id));
        }

        return result;
    }

    private async Task<Customer> FindOrCreateCustomerAsync(CheckoutInput input)
    {
        var normalizedPhone = NormalizePhone(input.Phone);
        var normalizedEmail = input.Email.Trim().ToLowerInvariant();
        Customer? customer = null;
        if (CurrentUser.Id.HasValue)
        {
            customer = await _customerRepository.FindAsync(x => x.UserId == CurrentUser.Id);
        }

        customer ??= await _customerRepository.FindAsync(x =>
            x.Phone == normalizedPhone || x.Email == normalizedEmail);
        if (customer is null)
        {
            customer = new Customer(
                GuidGenerator.Create(),
                input.FullName.Trim(),
                normalizedPhone,
                normalizedEmail,
                CurrentUser.Id,
                CurrentTenant.Id);
            customer.AddAddress(
                GuidGenerator.Create(),
                input.FullName.Trim(),
                normalizedPhone,
                input.Province.Trim(),
                input.District.Trim(),
                input.Ward.Trim(),
                input.AddressLine.Trim(),
                true);
            await _customerRepository.InsertAsync(customer);
        }
        else
        {
            customer.Update(input.FullName.Trim(), normalizedPhone, normalizedEmail);
            await _customerRepository.UpdateAsync(customer);
        }

        return customer;
    }

    private async Task<(Promotion? Promotion, decimal Discount)> ResolvePromotionAsync(
        string? promotionCode,
        decimal subtotal,
        Guid customerId)
    {
        Promotion? promotion;
        if (promotionCode.IsNullOrWhiteSpace())
        {
            promotion = (await _promotionRepository.GetListAsync(x => x.IsActive && x.IsAutomatic))
                .Where(x => x.StartTime <= Clock.Now && x.EndTime >= Clock.Now && x.MinimumOrderAmount <= subtotal)
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();
        }
        else
        {
            promotion = await _promotionRepository.FindAsync(x =>
                x.Code == promotionCode && x.IsActive);
            if (promotion is null)
            {
                throw new UserFriendlyException(L["Store:PromotionNotFound"]);
            }
        }

        if (promotion is null)
        {
            return (null, 0);
        }

        if (promotion.PerCustomerLimit.HasValue)
        {
            var usageCount = await _promotionUsageRepository.CountAsync(x =>
                x.PromotionId == promotion.Id && x.CustomerId == customerId);
            if (usageCount >= promotion.PerCustomerLimit.Value)
            {
                throw new UserFriendlyException(L["Store:PromotionCustomerLimitReached"]);
            }
        }

        return (promotion, promotion.CalculateDiscount(subtotal, Clock.Now));
    }

    private async Task<OrderDto> GetOrderDtoAsync(Guid id)
    {
        var query = await _orderRepository.WithDetailsAsync(x => x.Items, x => x.StatusHistory);
        var order = await AsyncExecuter.FirstAsync(query.Where(x => x.Id == id));
        var payment = await _paymentRepository.FindAsync(x => x.OrderId == order.Id);
        PaymentGatewayResult? gatewayResult = null;
        if (payment is not null && order.PaymentMethod != PaymentMethod.CashOnDelivery)
        {
            var gateway = _paymentGateways.FirstOrDefault(x => x.CanHandle(order.PaymentMethod));
            if (gateway is not null)
            {
                gatewayResult = await gateway.InitializeAsync(new PaymentGatewayRequest
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Method = order.PaymentMethod,
                    Amount = order.GrandTotal,
                    CustomerEmail = order.Email
                });
            }
        }

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            Email = order.Email,
            FullAddress = $"{order.AddressLine}, {order.Ward}, {order.District}, {order.Province}",
            ShippingMethodName = order.ShippingMethodName,
            TrackingCode = order.TrackingCode,
            CancellationReason = order.CancellationReason,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            ShippingFee = order.ShippingFee,
            TaxAmount = order.TaxAmount,
            GrandTotal = order.GrandTotal,
            PromotionCode = order.PromotionCode,
            PaymentReference = payment?.ReferenceNumber,
            PaymentRedirectUrl = gatewayResult?.RedirectUrl,
            PaymentInstructions = gatewayResult?.Instructions,
            CreationTime = order.CreationTime,
            Items = order.Items.Select(x => new OrderItemDto
            {
                Id = x.Id,
                ProductName = x.ProductName,
                Sku = x.Sku,
                OptionSummary = x.OptionSummary,
                ImageUrl = x.ImageUrl,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.UnitPrice * x.Quantity
            }).ToList(),
            History = order.StatusHistory.OrderBy(x => x.CreationTime).Select(x => new OrderHistoryDto
            {
                FromStatus = x.FromStatus,
                ToStatus = x.ToStatus,
                Note = x.Note,
                CreationTime = x.CreationTime
            }).ToList()
        };
    }

    private string CreateOrderNumber()
    {
        return $"AQ{Clock.Now:yyMMddHHmm}{GuidGenerator.Create():N}".ToUpperInvariant();
    }

    private static string NormalizePhone(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private sealed record ValidatedOrderItem(
        Product Product,
        ProductVariant? Variant,
        int Quantity,
        decimal UnitPrice,
        List<InventoryItem> Inventories);
}
