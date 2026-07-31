using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace AbpIoTemplateProject.Store;

public class Warehouse : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }

    protected Warehouse()
    {
    }

    public Warehouse(Guid id, string code, string name, string address, Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), StoreConsts.MaxCodeLength).ToUpperInvariant();
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), StoreConsts.MaxNameLength);
        Address = Check.NotNullOrWhiteSpace(address, nameof(address), StoreConsts.MaxAddressLength);
        IsActive = true;
    }
}

public class InventoryItem : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public int OnHandQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; }
    public int AvailableQuantity => OnHandQuantity - ReservedQuantity;

    protected InventoryItem()
    {
    }

    public InventoryItem(
        Guid id,
        Guid warehouseId,
        Guid productId,
        Guid? productVariantId,
        int initialQuantity,
        int lowStockThreshold,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        ProductId = productId;
        ProductVariantId = productVariantId;
        OnHandQuantity = Check.Range(initialQuantity, nameof(initialQuantity), 0, int.MaxValue);
        LowStockThreshold = Check.Range(lowStockThreshold, nameof(lowStockThreshold), 0, int.MaxValue);
    }

    public (int Before, int After) Adjust(int quantityDelta)
    {
        var before = OnHandQuantity;
        var after = before + quantityDelta;
        if (after < ReservedQuantity)
        {
            throw new BusinessException("Store:InventoryBelowReservedQuantity");
        }

        OnHandQuantity = after;
        return (before, after);
    }

    public void Reserve(int quantity)
    {
        if (quantity < 1 || AvailableQuantity < quantity)
        {
            throw new BusinessException("Store:InsufficientInventory");
        }

        ReservedQuantity += quantity;
    }

    public void Release(int quantity)
    {
        if (quantity < 1 || ReservedQuantity < quantity)
        {
            throw new BusinessException("Store:InvalidInventoryRelease");
        }

        ReservedQuantity -= quantity;
    }

    public void CompleteSale(int quantity)
    {
        if (quantity < 1 || ReservedQuantity < quantity || OnHandQuantity < quantity)
        {
            throw new BusinessException("Store:InvalidInventorySale");
        }

        ReservedQuantity -= quantity;
        OnHandQuantity -= quantity;
    }
}

public class InventoryTransaction : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public InventoryTransactionType Type { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityChanged { get; private set; }
    public int QuantityAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Note { get; private set; }

    protected InventoryTransaction()
    {
    }

    public InventoryTransaction(
        Guid id,
        Guid inventoryItemId,
        InventoryTransactionType type,
        int quantityBefore,
        int quantityChanged,
        int quantityAfter,
        string? referenceType,
        string? referenceNumber,
        string? note,
        Guid? tenantId = null) : base(id)
    {
        TenantId = tenantId;
        InventoryItemId = inventoryItemId;
        Type = type;
        QuantityBefore = quantityBefore;
        QuantityChanged = quantityChanged;
        QuantityAfter = quantityAfter;
        ReferenceType = referenceType?.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        Note = note?.Trim();
    }
}
