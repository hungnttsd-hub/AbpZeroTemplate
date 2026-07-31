using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace AbpIoTemplateProject.Store;

public class ShoppingCart : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    private readonly List<ShoppingCartItem> _items = new();

    public Guid? TenantId { get; private set; }
    public string CartKey { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public string? PromotionCode { get; private set; }
    public bool IsConverted { get; private set; }
    public IReadOnlyCollection<ShoppingCartItem> Items => _items;

    protected ShoppingCart()
    {
    }

    public ShoppingCart(Guid id, string cartKey, Guid? userId = null, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        CartKey = Check.NotNullOrWhiteSpace(cartKey, nameof(cartKey), StoreConsts.MaxCodeLength);
        UserId = userId;
    }

    public ShoppingCartItem AddOrUpdateItem(
        Guid itemId,
        Guid productId,
        Guid? variantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string sku,
        string? optionSummary,
        string? imageUrl)
    {
        EnsureMutable();
        var existing = _items.FirstOrDefault(x => x.ProductId == productId && x.ProductVariantId == variantId);
        if (existing is not null)
        {
            existing.SetQuantity(existing.Quantity + quantity);
            existing.RefreshSnapshot(unitPrice, productName, sku, optionSummary, imageUrl);
            return existing;
        }

        var item = new ShoppingCartItem(
            itemId,
            Id,
            productId,
            variantId,
            quantity,
            unitPrice,
            productName,
            sku,
            optionSummary,
            imageUrl,
            TenantId);
        _items.Add(item);
        return item;
    }

    public void UpdateItem(Guid itemId, int quantity)
    {
        EnsureMutable();
        var item = _items.FirstOrDefault(x => x.Id == itemId)
                   ?? throw new BusinessException("Store:CartItemNotFound");
        item.SetQuantity(quantity);
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureMutable();
        _items.RemoveAll(x => x.Id == itemId);
    }

    public void Clear()
    {
        EnsureMutable();
        _items.Clear();
        PromotionCode = null;
    }

    public void ApplyPromotion(string? code)
    {
        EnsureMutable();
        PromotionCode = code?.Trim().ToUpperInvariant();
    }

    public void AssignToUser(Guid userId)
    {
        UserId = userId;
    }

    public void MarkConverted()
    {
        EnsureMutable();
        IsConverted = true;
    }

    private void EnsureMutable()
    {
        if (IsConverted)
        {
            throw new BusinessException("Store:CartAlreadyConverted");
        }
    }
}

public class ShoppingCartItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ShoppingCartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? OptionSummary { get; private set; }
    public string? ImageUrl { get; private set; }

    protected ShoppingCartItem()
    {
    }

    internal ShoppingCartItem(
        Guid id,
        Guid shoppingCartId,
        Guid productId,
        Guid? productVariantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string sku,
        string? optionSummary,
        string? imageUrl,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        ShoppingCartId = shoppingCartId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        SetQuantity(quantity);
        RefreshSnapshot(unitPrice, productName, sku, optionSummary, imageUrl);
    }

    internal void SetQuantity(int quantity)
    {
        Quantity = Check.Range(quantity, nameof(quantity), 1, 999);
    }

    internal void RefreshSnapshot(
        decimal unitPrice,
        string productName,
        string sku,
        string? optionSummary,
        string? imageUrl)
    {
        UnitPrice = Check.Range(unitPrice, nameof(unitPrice), 0, decimal.MaxValue);
        ProductName = productName;
        Sku = sku;
        OptionSummary = optionSummary;
        ImageUrl = imageUrl;
    }
}

public class Customer : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    private readonly List<CustomerAddress> _addresses = new();

    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses;

    protected Customer()
    {
    }

    public Customer(
        Guid id,
        string fullName,
        string phone,
        string email,
        Guid? userId = null,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Update(fullName, phone, email);
    }

    public void Update(string fullName, string phone, string email)
    {
        FullName = Check.NotNullOrWhiteSpace(fullName, nameof(fullName), StoreConsts.MaxNameLength);
        Phone = Check.NotNullOrWhiteSpace(phone, nameof(phone), StoreConsts.MaxPhoneLength);
        Email = Check.NotNullOrWhiteSpace(email, nameof(email), StoreConsts.MaxEmailLength);
    }

    public void AddAddress(
        Guid id,
        string recipientName,
        string phone,
        string province,
        string district,
        string ward,
        string addressLine,
        bool isDefault)
    {
        if (isDefault)
        {
            foreach (var address in _addresses)
            {
                address.UnmarkDefault();
            }
        }

        _addresses.Add(new CustomerAddress(
            id,
            Id,
            recipientName,
            phone,
            province,
            district,
            ward,
            addressLine,
            isDefault,
            TenantId));
    }
}

public class CustomerAddress : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string Ward { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    protected CustomerAddress()
    {
    }

    internal CustomerAddress(
        Guid id,
        Guid customerId,
        string recipientName,
        string phone,
        string province,
        string district,
        string ward,
        string addressLine,
        bool isDefault,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        RecipientName = recipientName;
        Phone = phone;
        Province = province;
        District = district;
        Ward = ward;
        AddressLine = addressLine;
        IsDefault = isDefault;
    }

    internal void UnmarkDefault()
    {
        IsDefault = false;
    }
}

public class Order : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderStatusHistory> _statusHistory = new();

    public Guid? TenantId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string Ward { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public string? DeliveryNote { get; private set; }
    public Guid ShippingMethodId { get; private set; }
    public string ShippingMethodName { get; private set; } = string.Empty;
    public string? TrackingCode { get; private set; }
    public string? CancellationReason { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string? PromotionCode { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    protected Order()
    {
    }

    public Order(
        Guid id,
        string orderNumber,
        string idempotencyKey,
        Guid customerId,
        Guid? userId,
        string customerName,
        string phone,
        string email,
        string province,
        string district,
        string ward,
        string addressLine,
        string? deliveryNote,
        Guid shippingMethodId,
        string shippingMethodName,
        PaymentMethod paymentMethod,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        OrderNumber = Check.NotNullOrWhiteSpace(orderNumber, nameof(orderNumber), StoreConsts.MaxCodeLength);
        IdempotencyKey = Check.NotNullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey), StoreConsts.MaxCodeLength);
        CustomerId = customerId;
        UserId = userId;
        CustomerName = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), StoreConsts.MaxNameLength);
        Phone = Check.NotNullOrWhiteSpace(phone, nameof(phone), StoreConsts.MaxPhoneLength);
        Email = Check.NotNullOrWhiteSpace(email, nameof(email), StoreConsts.MaxEmailLength);
        Province = province;
        District = district;
        Ward = ward;
        AddressLine = addressLine;
        DeliveryNote = deliveryNote;
        ShippingMethodId = shippingMethodId;
        ShippingMethodName = shippingMethodName;
        PaymentMethod = paymentMethod;
        Status = OrderStatus.Pending;
        PaymentStatus = paymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Unpaid : PaymentStatus.Pending;
    }

    public void AddItem(
        Guid id,
        Guid productId,
        Guid? productVariantId,
        string productName,
        string sku,
        string? optionSummary,
        string? imageUrl,
        int quantity,
        decimal unitPrice,
        decimal taxRate)
    {
        EnsurePending();
        _items.Add(new OrderItem(
            id,
            Id,
            productId,
            productVariantId,
            productName,
            sku,
            optionSummary,
            imageUrl,
            quantity,
            unitPrice,
            taxRate,
            TenantId));
    }

    public void CalculateTotals(decimal discountAmount, decimal shippingFee, string? promotionCode)
    {
        EnsurePending();
        Subtotal = _items.Sum(x => x.UnitPrice * x.Quantity);
        TaxAmount = _items.Sum(x => x.UnitPrice * x.Quantity * x.TaxRate / 100m);
        DiscountAmount = Check.Range(discountAmount, nameof(discountAmount), 0, Subtotal);
        ShippingFee = Check.Range(shippingFee, nameof(shippingFee), 0, decimal.MaxValue);
        GrandTotal = Subtotal + TaxAmount + ShippingFee - DiscountAmount;
        PromotionCode = promotionCode;
    }

    public void Confirm(Guid historyId, string? note = null)
    {
        TransitionTo(historyId, OrderStatus.Confirmed, new[] { OrderStatus.Pending }, note);
    }

    public void StartPreparing(Guid historyId, string? note = null)
    {
        TransitionTo(historyId, OrderStatus.Preparing, new[] { OrderStatus.Confirmed }, note);
    }

    public void MarkReadyToShip(Guid historyId, string? note = null)
    {
        TransitionTo(historyId, OrderStatus.ReadyToShip, new[] { OrderStatus.Preparing }, note);
    }

    public void MarkAsShipped(Guid historyId, string trackingCode, string? note = null)
    {
        TrackingCode = Check.NotNullOrWhiteSpace(trackingCode, nameof(trackingCode), StoreConsts.MaxCodeLength);
        TransitionTo(historyId, OrderStatus.Shipping, new[] { OrderStatus.ReadyToShip }, note);
    }

    public void Complete(Guid historyId, string? note = null)
    {
        TransitionTo(historyId, OrderStatus.Completed, new[] { OrderStatus.Shipping }, note);
    }

    public void Cancel(Guid historyId, string reason)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Returned)
        {
            throw new BusinessException("Store:OrderCannotBeCancelled");
        }

        CancellationReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), StoreConsts.MaxNoteLength);
        TransitionTo(
            historyId,
            OrderStatus.Cancelled,
            new[]
            {
                OrderStatus.Pending,
                OrderStatus.Confirmed,
                OrderStatus.Preparing,
                OrderStatus.ReadyToShip
            },
            reason);
    }

    public void MarkPayment(PaymentStatus paymentStatus)
    {
        PaymentStatus = paymentStatus;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new BusinessException("Store:OrderIsNotPending");
        }
    }

    private void TransitionTo(Guid historyId, OrderStatus target, OrderStatus[] allowedSources, string? note)
    {
        if (!allowedSources.Contains(Status))
        {
            throw new BusinessException("Store:InvalidOrderStatusTransition")
                .WithData("CurrentStatus", Status)
                .WithData("TargetStatus", target);
        }

        var previous = Status;
        Status = target;
        _statusHistory.Add(new OrderStatusHistory(historyId, Id, previous, target, note, TenantId));
    }
}

public class OrderItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? OptionSummary { get; private set; }
    public string? ImageUrl { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }

    protected OrderItem()
    {
    }

    internal OrderItem(
        Guid id,
        Guid orderId,
        Guid productId,
        Guid? productVariantId,
        string productName,
        string sku,
        string? optionSummary,
        string? imageUrl,
        int quantity,
        decimal unitPrice,
        decimal taxRate,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        OrderId = orderId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductName = productName;
        Sku = sku;
        OptionSummary = optionSummary;
        ImageUrl = imageUrl;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
    }
}

public class OrderStatusHistory : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Note { get; private set; }

    protected OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(
        Guid id,
        Guid orderId,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        string? note,
        Guid? tenantId) : base(id)
    {
        TenantId = tenantId;
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
    }
}

public class Payment : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string? ReferenceNumber { get; private set; }

    protected Payment()
    {
    }

    public Payment(Guid id, Guid orderId, PaymentMethod method, decimal amount, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        OrderId = orderId;
        Method = method;
        Amount = amount;
        Status = method == PaymentMethod.CashOnDelivery ? PaymentStatus.Unpaid : PaymentStatus.Pending;
    }

    public void Confirm(string? referenceNumber)
    {
        Status = PaymentStatus.Paid;
        ReferenceNumber = referenceNumber?.Trim();
    }

    public void SetPendingReference(string? referenceNumber)
    {
        if (Status == PaymentStatus.Paid)
        {
            throw new BusinessException("Store:PaidPaymentCannotBeChanged");
        }

        ReferenceNumber = referenceNumber?.Trim();
    }
}

public class ShippingMethod : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Fee { get; private set; }
    public int EstimatedDays { get; private set; }
    public bool IsActive { get; private set; }

    protected ShippingMethod()
    {
    }

    public ShippingMethod(Guid id, string code, string name, decimal fee, int estimatedDays, Guid? tenantId = null)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Fee = fee;
        EstimatedDays = estimatedDays;
        IsActive = true;
    }
}

public class Promotion : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinimumOrderAmount { get; private set; }
    public decimal? MaximumDiscountAmount { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public int? PerCustomerLimit { get; private set; }
    public bool IsAutomatic { get; private set; }
    public bool CanCombine { get; private set; }
    public bool IsActive { get; private set; }

    protected Promotion()
    {
    }

    public Promotion(
        Guid id,
        string code,
        string name,
        PromotionType type,
        decimal value,
        decimal minimumOrderAmount,
        decimal? maximumDiscountAmount,
        DateTime startTime,
        DateTime endTime,
        Guid? tenantId = null) : base(id)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("Store:InvalidPromotionPeriod");
        }

        TenantId = tenantId;
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), StoreConsts.MaxCodeLength).ToUpperInvariant();
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Type = type;
        Value = value;
        MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscountAmount = maximumDiscountAmount;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
    }

    public void ConfigureLimits(int? usageLimit, int? perCustomerLimit, bool isAutomatic, bool canCombine, bool isActive)
    {
        UsageLimit = usageLimit;
        PerCustomerLimit = perCustomerLimit;
        IsAutomatic = isAutomatic;
        CanCombine = canCombine;
        IsActive = isActive;
    }

    public void Update(
        string name,
        PromotionType type,
        decimal value,
        decimal minimumOrderAmount,
        decimal? maximumDiscountAmount,
        DateTime startTime,
        DateTime endTime)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("Store:InvalidPromotionPeriod");
        }

        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Type = type;
        Value = Check.Range(value, nameof(value), 0, decimal.MaxValue);
        MinimumOrderAmount = Check.Range(minimumOrderAmount, nameof(minimumOrderAmount), 0, decimal.MaxValue);
        MaximumDiscountAmount = maximumDiscountAmount;
        StartTime = startTime;
        EndTime = endTime;
    }

    public decimal CalculateDiscount(decimal subtotal, DateTime now)
    {
        if (!IsActive || now < StartTime || now > EndTime)
        {
            throw new BusinessException("Store:PromotionExpired");
        }

        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
        {
            throw new BusinessException("Store:PromotionUsageLimitReached");
        }

        if (subtotal < MinimumOrderAmount)
        {
            throw new BusinessException("Store:PromotionMinimumOrderNotMet");
        }

        var discount = Type == PromotionType.Percentage
            ? subtotal * Value / 100m
            : Value;

        if (MaximumDiscountAmount.HasValue)
        {
            discount = Math.Min(discount, MaximumDiscountAmount.Value);
        }

        return Math.Min(discount, subtotal);
    }

    public void RecordUsage()
    {
        UsageCount++;
    }
}

public class PromotionUsage : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid PromotionId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public decimal DiscountAmount { get; private set; }

    protected PromotionUsage()
    {
    }

    public PromotionUsage(
        Guid id,
        Guid promotionId,
        Guid orderId,
        Guid? customerId,
        decimal discountAmount,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        PromotionId = promotionId;
        OrderId = orderId;
        CustomerId = customerId;
        DiscountAmount = discountAmount;
    }
}
