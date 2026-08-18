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
    private readonly IAffiliateOrderAppService _service;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public AffiliateOrderIsolationTests()
    {
        _users = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _trackings = GetRequiredService<IRepository<AffiliateTracking, Guid>>();
        _conversions = GetRequiredService<IRepository<AffiliateConversion, Guid>>();
        _orders = GetRequiredService<IRepository<AffiliateOrder, Guid>>();
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
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId) => new(new ClaimsIdentity(new List<Claim>
    {
        new(AbpClaimTypes.UserId, userId.ToString()),
        new(AbpClaimTypes.UserName, $"user-{userId:N}"),
        new(AbpClaimTypes.Email, $"user-{userId:N}@test.local")
    }, "Test"));
}
