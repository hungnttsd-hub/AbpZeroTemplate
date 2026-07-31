using System;
using System.Linq;
using AbpIoTemplateProject.Store;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AbpIoTemplateProject.StoreTests;

public class StoreDomainTests
{
    [Fact]
    public void Inventory_Should_Reserve_Release_And_Complete_Sale()
    {
        var inventory = new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10, 2);
        inventory.Reserve(4);
        inventory.AvailableQuantity.ShouldBe(6);
        inventory.Release(1);
        inventory.ReservedQuantity.ShouldBe(3);
        inventory.CompleteSale(3);
        inventory.OnHandQuantity.ShouldBe(7);
        inventory.ReservedQuantity.ShouldBe(0);
    }

    [Fact]
    public void Inventory_Should_Not_Go_Below_Reserved_Quantity()
    {
        var inventory = new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10, 2);
        inventory.Reserve(7);
        Should.Throw<BusinessException>(() => inventory.Adjust(-4));
    }

    [Fact]
    public void Promotion_Should_Validate_Period_Minimum_And_Maximum_Discount()
    {
        var now = DateTime.UtcNow;
        var promotion = new Promotion(
            Guid.NewGuid(), "SAVE20", "Save 20", PromotionType.Percentage, 20, 100, 30, now.AddDays(-1), now.AddDays(1));
        promotion.CalculateDiscount(200, now).ShouldBe(30);
        Should.Throw<BusinessException>(() => promotion.CalculateDiscount(50, now));
        Should.Throw<BusinessException>(() => promotion.CalculateDiscount(200, now.AddDays(2)));
    }

    [Fact]
    public void Category_Should_Not_Be_Its_Own_Parent()
    {
        var category = new Category(Guid.NewGuid(), "Plants", "plants");
        Should.Throw<BusinessException>(() => category.Update("Plants", "plants", null, null, category.Id, false, true, 0));
    }

    [Fact]
    public void Simple_Product_Should_Not_Accept_Variants()
    {
        var product = CreateProduct(ProductType.Simple);
        Should.Throw<BusinessException>(() => product.AddVariant(
            Guid.NewGuid(), "Large", "SKU-L", "Size: L", 100, 120, null, null));
    }

    [Fact]
    public void Cart_Should_Merge_The_Same_Product_And_Clear_Promotion()
    {
        var cart = new ShoppingCart(Guid.NewGuid(), "cart-key");
        var productId = Guid.NewGuid();
        cart.AddOrUpdateItem(Guid.NewGuid(), productId, null, 1, 100, "Product", "SKU", null, null);
        cart.AddOrUpdateItem(Guid.NewGuid(), productId, null, 2, 100, "Product", "SKU", null, null);
        cart.Items.Single().Quantity.ShouldBe(3);
        cart.ApplyPromotion(" save10 ");
        cart.PromotionCode.ShouldBe("SAVE10");
        cart.Clear();
        cart.Items.ShouldBeEmpty();
        cart.PromotionCode.ShouldBeNull();
    }

    [Fact]
    public void Order_Should_Enforce_Status_Transitions()
    {
        var order = CreateOrder();
        Should.Throw<BusinessException>(() => order.StartPreparing(Guid.NewGuid()));
        order.Confirm(Guid.NewGuid());
        order.StartPreparing(Guid.NewGuid());
        order.MarkReadyToShip(Guid.NewGuid());
        order.MarkAsShipped(Guid.NewGuid(), "TRACK-1");
        order.Complete(Guid.NewGuid());
        order.Status.ShouldBe(OrderStatus.Completed);
        order.StatusHistory.Count.ShouldBe(5);
    }

    [Fact]
    public void Completed_Order_Should_Not_Be_Cancelled()
    {
        var order = CreateOrder();
        order.Confirm(Guid.NewGuid());
        order.StartPreparing(Guid.NewGuid());
        order.MarkReadyToShip(Guid.NewGuid());
        order.MarkAsShipped(Guid.NewGuid(), "TRACK-2");
        order.Complete(Guid.NewGuid());
        Should.Throw<BusinessException>(() => order.Cancel(Guid.NewGuid(), "No longer allowed"));
    }

    private static Product CreateProduct(ProductType type)
    {
        return new Product(Guid.NewGuid(), "P001", "SKU001", "Product", "product", type, Guid.NewGuid());
    }

    private static Order CreateOrder()
    {
        var order = new Order(
            Guid.NewGuid(), "AQ-TEST", "test-key", Guid.NewGuid(), null, "Customer", "0901000000",
            "customer@example.com", "HCM", "District 7", "Tan Phong", "1 Main Street", null,
            Guid.NewGuid(), "Standard", PaymentMethod.CashOnDelivery);
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), null, "Product", "SKU", null, null, 2, 100, 10);
        order.CalculateTotals(10, 20, "SAVE");
        order.GrandTotal.ShouldBe(230);
        return order;
    }
}
