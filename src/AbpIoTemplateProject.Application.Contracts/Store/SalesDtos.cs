using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Store;

public class AddCartItemInput
{
    [Required]
    public string CartKey { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemInput
{
    [Required]
    public string CartKey { get; set; } = string.Empty;
    public Guid ItemId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }
}

public class CartKeyInput
{
    [Required]
    public string CartKey { get; set; } = string.Empty;
}

public class ApplyPromotionInput : CartKeyInput
{
    [MaxLength(StoreConsts.MaxCodeLength)]
    public string? PromotionCode { get; set; }
}

public class CartItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? OptionSummary { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsAvailable { get; set; }
    public int MaximumPurchasableQuantity { get; set; }
}

public class CartDto
{
    public Guid? Id { get; set; }
    public string CartKey { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = new();
    public string? PromotionCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal EstimatedShippingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public int TotalQuantity { get; set; }
}

public class ShippingMethodDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Fee { get; set; }
    public int EstimatedDays { get; set; }
}

public class CheckoutInput
{
    [Required]
    public string CartKey { get; set; } = string.Empty;

    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required, MaxLength(StoreConsts.MaxNameLength)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, MaxLength(StoreConsts.MaxPhoneLength)]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(StoreConsts.MaxEmailLength)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string District { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Ward { get; set; } = string.Empty;

    [Required, MaxLength(StoreConsts.MaxAddressLength)]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(StoreConsts.MaxNoteLength)]
    public string? DeliveryNote { get; set; }

    public Guid ShippingMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}

public class TrackOrderInput
{
    [Required, MaxLength(StoreConsts.MaxCodeLength)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, MaxLength(StoreConsts.MaxEmailLength)]
    public string Verification { get; set; } = string.Empty;
}

public class OrderItemDto : EntityDto<Guid>
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? OptionSummary { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderHistoryDto
{
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime CreationTime { get; set; }
}

public class OrderDto : EntityDto<Guid>
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string ShippingMethodName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public string? CancellationReason { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PromotionCode { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentRedirectUrl { get; set; }
    public string? PaymentInstructions { get; set; }
    public DateTime CreationTime { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderHistoryDto> History { get; set; } = new();
}

public class PaymentGatewayRequest
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}

public class PaymentGatewayResult
{
    public string Reference { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public string? Instructions { get; set; }
}

public interface IStorePaymentGateway
{
    bool CanHandle(PaymentMethod method);
    Task<PaymentGatewayResult> InitializeAsync(PaymentGatewayRequest request);
}

public class CustomerAddressDto : EntityDto<Guid>
{
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
