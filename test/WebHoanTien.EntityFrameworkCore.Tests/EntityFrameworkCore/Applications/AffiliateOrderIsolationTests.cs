using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Applications;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class AffiliateOrderIsolationTests : WebHoanTienEntityFrameworkCoreTestBase
{
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IAffiliateOrderAppService _service;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public AffiliateOrderIsolationTests()
    {
        _users = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _trackings = GetRequiredService<IRepository<AffiliateTracking, Guid>>();
        _conversions = GetRequiredService<IRepository<AffiliateConversion, Guid>>();
        _orders = GetRequiredService<IRepository<AffiliateOrder, Guid>>();
        _items = GetRequiredService<IRepository<AffiliateOrderItem, Guid>>();
        _attributions = GetRequiredService<IRepository<AffiliateOrderItemAttribution, Guid>>();
        _service = GetRequiredService<IAffiliateOrderAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Customer_Can_Only_List_And_Open_Own_Orders()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await _users.InsertAsync(new IdentityUser(userA, $"a-{userA:N}", $"a-{userA:N}@test.local"));
            await _users.InsertAsync(new IdentityUser(userB, $"b-{userB:N}", $"b-{userB:N}@test.local"));
            await AddOrderAsync(userA, orderA, "ORDER-A");
            await AddOrderAsync(userB, orderB, "ORDER-B");
        });

        using (_principalAccessor.Change(CreatePrincipal(userA)))
        {
            var result = await _service.GetListAsync(new AffiliateOrderListInput());
            result.Items.ShouldContain(x => x.Id == orderA);
            result.Items.ShouldNotContain(x => x.Id == orderB);
            (await _service.GetAsync(orderA)).Id.ShouldBe(orderA);
            await Should.ThrowAsync<AbpAuthorizationException>(() => _service.GetAsync(orderB));
        }
    }

    [Fact]
    public async Task Shared_Order_Should_Show_Each_User_Only_Their_Attributed_Items()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await _users.InsertAsync(new IdentityUser(userA, $"a-{userA:N}", $"a-{userA:N}@test.local"));
            await _users.InsertAsync(new IdentityUser(userB, $"b-{userB:N}", $"b-{userB:N}@test.local"));
            await AddSharedOrderAsync(userA, userB, orderId);
        });

        using (_principalAccessor.Change(CreatePrincipal(userA)))
        {
            var order = await _service.GetAsync(orderId);
            order.Items.Count.ShouldBe(1);
            order.Items[0].ProductName.ShouldBe("Item A");
            order.ExpectedUserCommission.ShouldBe(40_000m);
            order.Items[0].Attributions.ShouldBeEmpty();
            order.Recipients.ShouldBeEmpty();
        }
        using (_principalAccessor.Change(CreatePrincipal(userB)))
        {
            var order = await _service.GetAsync(orderId);
            order.Items.Count.ShouldBe(1);
            order.Items[0].ProductName.ShouldBe("Item B");
            order.ExpectedUserCommission.ShouldBe(50_000m);
            order.Items[0].Attributions.ShouldBeEmpty();
            order.Recipients.ShouldBeEmpty();
        }
    }

    private async Task AddOrderAsync(Guid userId, Guid orderId, string externalOrderId)
    {
        var trackingId = Guid.NewGuid();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await _trackings.InsertAsync(new AffiliateTracking(trackingId, userId, AffiliatePlatform.Shopee, token,
            $"https://shopee.vn/product/1/{Math.Abs(orderId.GetHashCode())}",
            $"https://shopee.vn/product/1/{Math.Abs(orderId.GetHashCode())}"));

        var conversionId = Guid.NewGuid();
        var conversion = new AffiliateConversion(conversionId, AffiliatePlatform.Shopee, $"CONV-{conversionId:N}", DateTime.UtcNow);
        conversion.MapTo(trackingId, userId, token);
        conversion.ApplyCommission(100_000m, 100_000m, CommissionSource.NetCommission, 70m);
        await _conversions.InsertAsync(conversion);

        var order = new AffiliateOrder(orderId, conversionId, externalOrderId);
        order.Update(AffiliateOrderStatus.Pending, "Marketplace", 500_000m, 100_000m, 70_000m);
        await _orders.InsertAsync(order);
        var itemId = Guid.NewGuid();
        var item = new AffiliateOrderItem(itemId, orderId, $"ITEM-{orderId:N}", null);
        item.Update("Test item", 500_000m, 1, 100_000m, 100_000m, 70_000m,
            0m, false, "Pending");
        await _items.InsertAsync(item);
        var attribution = new AffiliateOrderItemAttribution(Guid.NewGuid(), itemId, token);
        attribution.UpdateSource(500_000m, 1, 100_000m, 100_000m, 0m, false, "Pending");
        attribution.Match(trackingId, userId, 70m, 70_000m);
        await _attributions.InsertAsync(attribution);
    }

    private async Task AddSharedOrderAsync(Guid userA, Guid userB, Guid orderId)
    {
        var tokenA = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        var tokenB = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        var trackingA = new AffiliateTracking(Guid.NewGuid(), userA, AffiliatePlatform.Shopee, tokenA,
            "https://shopee.vn/product/1/101", "https://shopee.vn/product/1/101");
        var trackingB = new AffiliateTracking(Guid.NewGuid(), userB, AffiliatePlatform.Shopee, tokenB,
            "https://shopee.vn/product/1/102", "https://shopee.vn/product/1/102");
        await _trackings.InsertAsync(trackingA);
        await _trackings.InsertAsync(trackingB);
        var conversion = new AffiliateConversion(Guid.NewGuid(), AffiliatePlatform.Shopee,
            $"CONV-{orderId:N}", DateTime.UtcNow);
        conversion.ApplyAttributedCommission(90_000m, 90_000m, CommissionSource.NetCommission, null, 90_000m);
        await _conversions.InsertAsync(conversion);
        var order = new AffiliateOrder(orderId, conversion.Id, $"ORDER-{orderId:N}");
        order.Update(AffiliateOrderStatus.Pending, "Marketplace", 900_000m, 90_000m, 90_000m);
        await _orders.InsertAsync(order);

        foreach (var row in new[]
                 {
                     (Name: "Item A", Token: tokenA, Tracking: trackingA, User: userA, Commission: 40_000m),
                     (Name: "Item B", Token: tokenB, Tracking: trackingB, User: userB, Commission: 50_000m)
                 })
        {
            var item = new AffiliateOrderItem(Guid.NewGuid(), orderId, $"ITEM-{row.Name}", null);
            item.Update(row.Name, row.Commission * 10m, 1, row.Commission, row.Commission,
                row.Commission, 0m, false, "Pending");
            await _items.InsertAsync(item);
            var attribution = new AffiliateOrderItemAttribution(Guid.NewGuid(), item.Id, row.Token);
            attribution.UpdateSource(row.Commission * 10m, 1, row.Commission, row.Commission,
                0m, false, "Pending");
            attribution.Match(row.Tracking.Id, row.User, 100m, row.Commission);
            await _attributions.InsertAsync(attribution);
        }
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId) => new(new ClaimsIdentity(new List<Claim>
    {
        new(AbpClaimTypes.UserId, userId.ToString()),
        new(AbpClaimTypes.UserName, $"user-{userId:N}"),
        new(AbpClaimTypes.Email, $"user-{userId:N}@test.local")
    }, "Test"));
}
