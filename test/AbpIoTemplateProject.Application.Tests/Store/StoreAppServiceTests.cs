using System;
using System.Linq;
using System.Threading.Tasks;
using AbpIoTemplateProject.Store;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace AbpIoTemplateProject.StoreTests;

public abstract class StoreAppServiceTests<TStartupModule> : AbpIoTemplateProjectApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IStorefrontAppService _storefront;
    private readonly ICartAppService _cart;
    private readonly IOrderAppService _orders;
    private readonly IStoreAdminAppService _admin;

    protected StoreAppServiceTests()
    {
        _storefront = GetRequiredService<IStorefrontAppService>();
        _cart = GetRequiredService<ICartAppService>();
        _orders = GetRequiredService<IOrderAppService>();
        _admin = GetRequiredService<IStoreAdminAppService>();
    }

    [Fact]
    public async Task Catalog_Should_Search_Filter_And_Page()
    {
        var result = await _storefront.GetProductsAsync(new ProductListInput
        {
            Filter = "AQ-",
            MaxResultCount = 5
        });
        result.TotalCount.ShouldBeGreaterThan(5);
        result.Items.Count.ShouldBe(5);
        result.Items.ShouldAllBe(x => x.IsInStock);
    }

    [Fact]
    public async Task Product_Detail_Should_Contain_Related_Products()
    {
        var list = await _storefront.GetProductsAsync(new ProductListInput { MaxResultCount = 1 });
        var detail = await _storefront.GetProductAsync(list.Items[0].Slug);
        detail.Id.ShouldBe(list.Items[0].Id);
        detail.Images.ShouldNotBeEmpty();
        detail.RelatedProducts.ShouldAllBe(x => x.Id != detail.Id);
    }

    [Fact]
    public async Task Cart_Should_Reject_Quantity_Above_Inventory()
    {
        var list = await _storefront.GetProductsAsync(new ProductListInput { MaxResultCount = 60 });
        var product = list.Items.First(x => !x.HasVariants);
        await Should.ThrowAsync<UserFriendlyException>(() => _cart.AddAsync(new AddCartItemInput
        {
            CartKey = $"test-{Guid.NewGuid():N}",
            ProductId = product.Id,
            Quantity = 999
        }));
    }

    [Fact]
    public async Task Checkout_Should_Be_Idempotent_And_Trackable()
    {
        var list = await _storefront.GetProductsAsync(new ProductListInput { MaxResultCount = 60 });
        var product = list.Items.First(x => !x.HasVariants);
        var cartKey = $"test-{Guid.NewGuid():N}";
        await _cart.AddAsync(new AddCartItemInput { CartKey = cartKey, ProductId = product.Id, Quantity = 1 });
        var shipping = (await _orders.GetShippingMethodsAsync()).First();
        var input = new CheckoutInput
        {
            CartKey = cartKey,
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            FullName = "Integration Test",
            Phone = "0901234567",
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Province = "TP.HCM",
            District = "Quận 7",
            Ward = "Tân Phong",
            AddressLine = "1 Test Street",
            ShippingMethodId = shipping.Id,
            PaymentMethod = PaymentMethod.CashOnDelivery
        };
        var first = await _orders.CheckoutAsync(input);
        var second = await _orders.CheckoutAsync(input);
        second.Id.ShouldBe(first.Id);
        var tracked = await _orders.TrackAsync(new TrackOrderInput
        {
            OrderNumber = first.OrderNumber,
            Verification = input.Phone
        });
        tracked.Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task Admin_Dashboard_And_Inventory_Should_Reflect_Seeded_Data()
    {
        var dashboard = await _admin.GetDashboardAsync();
        var inventory = await _admin.GetInventoryAsync();
        dashboard.ProductCount.ShouldBe(50);
        dashboard.CustomerCount.ShouldBeGreaterThanOrEqualTo(10);
        inventory.ShouldNotBeEmpty();
        inventory.ShouldAllBe(x => x.OnHandQuantity >= x.ReservedQuantity);
    }
}
