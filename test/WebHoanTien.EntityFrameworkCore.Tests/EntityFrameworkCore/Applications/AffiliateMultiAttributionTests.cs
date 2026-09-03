using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;
using WebHoanTien.Notifications;
using WebHoanTien.Operations;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Applications;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class AffiliateMultiAttributionTests : WebHoanTienEntityFrameworkCoreTestBase
{
    private static readonly DateTime PurchaseTime = new(2026, 8, 21, 3, 6, 7, DateTimeKind.Utc);

    private readonly AffiliateConversionUpserter _upserter;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IRepository<CustomerNotification, Guid> _notifications;
    private readonly IRepository<ShopeeSettlementBatch, Guid> _settlementBatches;
    private readonly IRepository<ShopeeSettlementBill, Guid> _settlementBills;
    private readonly IRepository<ShopeeSettlementRecord, Guid> _settlementRecords;
    private readonly ICustomerWalletAppService _wallet;
    private readonly IAdminShopeeSettlementApprovalAppService _settlements;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public AffiliateMultiAttributionTests()
    {
        _upserter = GetRequiredService<AffiliateConversionUpserter>();
        _users = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _trackings = GetRequiredService<IRepository<AffiliateTracking, Guid>>();
        _orders = GetRequiredService<IRepository<AffiliateOrder, Guid>>();
        _items = GetRequiredService<IRepository<AffiliateOrderItem, Guid>>();
        _attributions = GetRequiredService<IRepository<AffiliateOrderItemAttribution, Guid>>();
        _notifications = GetRequiredService<IRepository<CustomerNotification, Guid>>();
        _settlementBatches = GetRequiredService<IRepository<ShopeeSettlementBatch, Guid>>();
        _settlementBills = GetRequiredService<IRepository<ShopeeSettlementBill, Guid>>();
        _settlementRecords = GetRequiredService<IRepository<ShopeeSettlementRecord, Guid>>();
        _wallet = GetRequiredService<ICustomerWalletAppService>();
        _settlements = GetRequiredService<IAdminShopeeSettlementApprovalAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Same_User_With_Two_Links_Should_Create_One_Order_Movement_And_Notification()
    {
        var userId = Guid.NewGuid();
        var tokenA = Token("same-a");
        var tokenB = Token("same-b");
        var orderId = $"ORDER-SAME-{Guid.NewGuid():N}";

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userId, tokenA);
            await AddTrackingAsync(userId, tokenB);
            var result = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, Source(orderId,
                AffiliateOrderStatus.Pending,
                new SourceItem("ITEM-A", tokenA, 4_000m),
                new SourceItem("ITEM-B", tokenB, 6_000m)));

            result.MatchedItemCount.ShouldBe(2);
            result.MultiTrackingOrderCount.ShouldBe(1);
            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Single();
            var itemIds = (await _items.GetListAsync(x => x.OrderId == order.Id)).Select(x => x.Id).ToList();
            var rows = await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId));
            rows.Count.ShouldBe(2);
            rows.All(x => x.UserId == userId).ShouldBeTrue();
            rows.Sum(x => x.UserCommissionSnapshot).ShouldBe(10_000m);
            (await _notifications.GetListAsync(x => x.UserId == userId &&
                x.EventKey == $"order:{order.Id:N}:pending")).Count.ShouldBe(1);
        });

        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            var history = await _wallet.GetHistoryAsync(new WalletHistoryInput { MaxResultCount = 20 });
            history.Items.Count(x => x.Kind == WalletMovementKind.Commission).ShouldBe(1);
            history.Items.Single(x => x.Kind == WalletMovementKind.Commission).Amount.ShouldBe(10_000m);
        }
    }

    [Fact]
    public async Task Shop_Link_Should_Match_All_Order_Items_By_Platform_And_Token()
    {
        var userId = Guid.NewGuid();
        var trackingId = Guid.NewGuid();
        var token = Token("shop-order");
        var orderId = $"ORDER-SHOP-{Guid.NewGuid():N}";

        await WithUnitOfWorkAsync(async () =>
        {
            await _users.InsertAsync(new IdentityUser(userId, $"user-{userId:N}", $"user-{userId:N}@test.local"));
            var tracking = new AffiliateTracking(trackingId, userId, AffiliatePlatform.Shopee, token,
                "https://shopee.vn/catsback.official", "https://shopee.vn/catsback.official");
            tracking.Hide(PurchaseTime.AddDays(-1));
            await _trackings.InsertAsync(tracking);

            var source = Source(orderId, AffiliateOrderStatus.Pending,
                new SourceItem("SHOP-ITEM-A", token, 4_000m),
                new SourceItem("SHOP-ITEM-B", token, 6_000m));
            var firstImport = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, source);
            var secondImport = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, source);

            firstImport.MatchedItemCount.ShouldBe(2);
            secondImport.MatchedItemCount.ShouldBe(2);
            (await _trackings.GetAsync(trackingId)).ProductId.ShouldBeNull();
            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Single();
            var rows = await ActiveAttributionsAsync(order.Id);
            rows.Count.ShouldBe(2);
            rows.All(x => x.TrackingId == trackingId).ShouldBeTrue();
            rows.All(x => x.UserId == userId).ShouldBeTrue();
            rows.All(x => x.Status == AffiliateAttributionStatus.Matched).ShouldBeTrue();
            rows.Sum(x => x.UserCommissionSnapshot).ShouldBe(10_000m);
            (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Count.ShouldBe(1);
            (await _notifications.GetListAsync(x => x.UserId == userId &&
                x.EventKey == $"order:{order.Id:N}:pending")).Count.ShouldBe(1);
        });

        await AssertSingleWalletMovementAsync(userId, 10_000m);
    }

    [Fact]
    public async Task Multiple_Users_And_Unknown_Token_Should_Keep_Unknown_Share_Uncredited()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var tokenA = Token("multi-a");
        var tokenB = Token("multi-b");
        var unknown = Token("unknown");
        var orderId = $"ORDER-MULTI-{Guid.NewGuid():N}";

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userA, tokenA);
            await AddUserAndTrackingAsync(userB, tokenB);
            var result = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, Source(orderId,
                AffiliateOrderStatus.Completed,
                new SourceItem("ITEM-A", tokenA, 2_000m),
                new SourceItem("ITEM-B", tokenB, 3_000m),
                new SourceItem("ITEM-C", unknown, 5_000m)));

            result.MatchedItemCount.ShouldBe(2);
            result.UnmatchedItemCount.ShouldBe(1);
            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Single();
            order.UserCommissionSnapshot.ShouldBe(5_000m);
            var itemIds = (await _items.GetListAsync(x => x.OrderId == order.Id)).Select(x => x.Id).ToList();
            var rows = await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId));
            rows.Single(x => x.AttributionValue == tokenA).UserCommissionSnapshot.ShouldBe(2_000m);
            rows.Single(x => x.AttributionValue == tokenB).UserCommissionSnapshot.ShouldBe(3_000m);
            var unmatched = rows.Single(x => x.AttributionValue == unknown);
            unmatched.Status.ShouldBe(AffiliateAttributionStatus.Unmatched);
            unmatched.UserCommissionSnapshot.ShouldBe(0m);
        });

        await AssertSingleWalletMovementAsync(userA, 2_000m);
        await AssertSingleWalletMovementAsync(userB, 3_000m);
    }

    [Fact]
    public async Task Reimport_Row_Order_And_Item_Removal_Should_Be_Idempotent_And_Restorable()
    {
        var userId = Guid.NewGuid();
        var tokenA = Token("repeat-a");
        var tokenB = Token("repeat-b");
        var orderId = $"ORDER-REPEAT-{Guid.NewGuid():N}";
        var itemA = new SourceItem("ITEM-A", tokenA, 4_000m);
        var itemB = new SourceItem("ITEM-B", tokenB, 6_000m);

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userId, tokenA);
            await AddTrackingAsync(userId, tokenB);
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee,
                Source(orderId, AffiliateOrderStatus.Pending, itemA, itemB));
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee,
                Source(orderId, AffiliateOrderStatus.Pending, itemB, itemA));

            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Single();
            (await _items.GetListAsync(x => x.OrderId == order.Id)).Count.ShouldBe(2);
            (await ActiveAttributionsAsync(order.Id)).Count.ShouldBe(2);

            await _upserter.UpsertAsync(AffiliatePlatform.Shopee,
                Source(orderId, AffiliateOrderStatus.Pending, itemA));
            (await _items.GetListAsync(x => x.OrderId == order.Id)).Count.ShouldBe(1);
            (await ActiveAttributionsAsync(order.Id)).Count.ShouldBe(1);

            await _upserter.UpsertAsync(AffiliatePlatform.Shopee,
                Source(orderId, AffiliateOrderStatus.Pending, itemA, itemB));
            (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Count.ShouldBe(1);
            (await _items.GetListAsync(x => x.OrderId == order.Id)).Count.ShouldBe(2);
            (await ActiveAttributionsAsync(order.Id)).Count.ShouldBe(2);
        });
    }

    [Fact]
    public async Task Settled_Order_With_Changed_Token_Should_Preserve_Paid_Allocation_And_Mark_Conflict()
    {
        var userId = Guid.NewGuid();
        var tokenA = Token("settled-a");
        var tokenB = Token("settled-b");
        var orderId = $"ORDER-SETTLED-{Guid.NewGuid():N}";

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userId, tokenA);
            await AddTrackingAsync(userId, tokenB);
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee, Source(orderId,
                AffiliateOrderStatus.Completed, new SourceItem("ITEM-A", tokenA, 10_000m)));
            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == orderId)).Single();
            var attribution = (await ActiveAttributionsAsync(order.Id)).Single();
            attribution.Settle(9_000m, 9_000m);
            order.Settle(9_000m, 9_000m, "SETTLEMENT-1", PurchaseTime.AddDays(30));
            await _attributions.UpdateAsync(attribution);
            await _orders.UpdateAsync(order, autoSave: true);

            var result = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, Source(orderId,
                AffiliateOrderStatus.Completed, new SourceItem("ITEM-A", tokenB, 10_000m)));

            result.ConflictMessage.ShouldNotBeNullOrWhiteSpace();
            result.ConflictMessage.ShouldContain(tokenA);
            result.ConflictMessage.ShouldContain(tokenB);
            var persistedOrder = await _orders.GetAsync(order.Id);
            persistedOrder.SettledNetCommission.ShouldBe(9_000m);
            persistedOrder.SettledUserCommission.ShouldBe(9_000m);
            var persisted = (await ActiveAttributionsAsync(order.Id)).Single();
            persisted.AttributionValue.ShouldBe(tokenA);
            persisted.Status.ShouldBe(AffiliateAttributionStatus.Conflict);
            persisted.SettledUserCommission.ShouldBe(9_000m);
        });

        await AssertSingleWalletMovementAsync(userId, 9_000m);
    }

    [Fact]
    public async Task Settlement_Should_Allocate_Fees_And_Tax_Without_Redistributing_Unknown_Share()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tokenA = Token("settlement-a");
        var tokenB = Token("settlement-b");
        var unknown = Token("settlement-unknown");
        var externalOrderId = $"ORDER-PAYOUT-{Guid.NewGuid():N}";
        var batchId = Guid.NewGuid();
        var billId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        Guid orderId = Guid.Empty;

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userA, tokenA);
            await AddUserAndTrackingAsync(userB, tokenB);
            await _users.InsertAsync(new IdentityUser(adminId, $"admin-{adminId:N}",
                $"admin-{adminId:N}@test.local"));
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee, Source(externalOrderId,
                AffiliateOrderStatus.Completed,
                new SourceItem("ITEM-A", tokenA, 2_000m),
                new SourceItem("ITEM-B", tokenB, 3_000m),
                new SourceItem("ITEM-C", unknown, 5_000m)));

            var order = (await _orders.GetListAsync(x => x.ExternalOrderId == externalOrderId)).Single();
            orderId = order.Id;
            var batch = new ShopeeSettlementBatch(batchId, ShopeeSettlementImportSource.Automation,
                "multi-user-settlement.json", Guid.NewGuid().ToString("N"));
            batch.UpdateSummary(1, 1, 1, 0, 0, 0, 0, 0,
                10_000m, 8_500m, 8_500m, 0m);
            var bill = new ShopeeSettlementBill(billId, batchId, "affiliate-test", "validation-test",
                "payout-test", PurchaseTime.AddDays(30), PurchaseTime, PurchaseTime.AddDays(1),
                10_000m, 9_000m, 8_500m, true, 1);
            var record = new ShopeeSettlementRecord(recordId, batchId, billId, externalOrderId,
                10_000m, 1_000m, 500m, 8_500m);
            record.SetPendingApproval(order.Id, order.ConversionId, null,
                "Có 1 affiliate link chưa ghép; phần này sẽ không được cộng cho người dùng.");
            await _settlementBatches.InsertAsync(batch);
            await _settlementBills.InsertAsync(bill);
            await _settlementRecords.InsertAsync(record);
        });

        using (_principalAccessor.Change(CreatePrincipal(adminId)))
        {
            var result = await _settlements.ApproveAsync(recordId);
            result.ApprovedCount.ShouldBe(1);
            result.ApprovedCommission.ShouldBe(8_500m);
            result.CreditedUserCommission.ShouldBe(4_250m);
        }

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await _orders.GetAsync(orderId);
            order.SettledNetCommission.ShouldBe(8_500m);
            order.SettledUserCommission.ShouldBe(4_250m);
            var rows = await ActiveAttributionsAsync(orderId);
            rows.Sum(x => x.SettledNetCommission ?? 0m).ShouldBe(8_500m);
            rows.Single(x => x.AttributionValue == tokenA).SettledNetCommission.ShouldBe(1_700m);
            rows.Single(x => x.AttributionValue == tokenA).SettledUserCommission.ShouldBe(1_700m);
            rows.Single(x => x.AttributionValue == tokenB).SettledNetCommission.ShouldBe(2_550m);
            rows.Single(x => x.AttributionValue == tokenB).SettledUserCommission.ShouldBe(2_550m);
            rows.Single(x => x.AttributionValue == unknown).SettledNetCommission.ShouldBe(4_250m);
            rows.Single(x => x.AttributionValue == unknown).SettledUserCommission.ShouldBe(0m);
            (await _notifications.GetListAsync(x => x.UserId == userA &&
                x.EventKey == $"order:{orderId:N}:settled")).Count.ShouldBe(1);
            (await _notifications.GetListAsync(x => x.UserId == userB &&
                x.EventKey == $"order:{orderId:N}:settled")).Count.ShouldBe(1);
        });

        await AssertSingleWalletMovementAsync(userA, 1_700m);
        await AssertSingleWalletMovementAsync(userB, 2_550m);
    }

    [Fact]
    public async Task Order_Tiers_Should_Follow_Purchase_Time_And_Shift_When_The_First_Order_Is_Cancelled()
    {
        var userId = Guid.NewGuid();
        var firstToken = Token("tier-first");
        var secondToken = Token("tier-second");
        var firstOrderId = $"ORDER-TIER-FIRST-{Guid.NewGuid():N}";
        var secondOrderId = $"ORDER-TIER-SECOND-{Guid.NewGuid():N}";
        var firstPurchaseTime = PurchaseTime;
        var secondPurchaseTime = PurchaseTime.AddHours(1);

        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndTrackingAsync(userId, firstToken);
            await AddTrackingAsync(userId, secondToken);

            // Import the later order first to prove that CSV/import order does not define the tier.
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee, SourceAt(secondOrderId,
                AffiliateOrderStatus.Pending, secondPurchaseTime,
                new SourceItem("ITEM-SECOND", secondToken, 10_000m)));
            await _upserter.UpsertAsync(AffiliatePlatform.Shopee, SourceAt(firstOrderId,
                AffiliateOrderStatus.Pending, firstPurchaseTime,
                new SourceItem("ITEM-FIRST", firstToken, 10_000m)));

            var first = (await _orders.GetListAsync(x => x.ExternalOrderId == firstOrderId)).Single();
            var second = (await _orders.GetListAsync(x => x.ExternalOrderId == secondOrderId)).Single();
            var firstAttribution = (await ActiveAttributionsAsync(first.Id)).Single();
            var secondAttribution = (await ActiveAttributionsAsync(second.Id)).Single();
            firstAttribution.UserShareRate.ShouldBe(100m);
            firstAttribution.UserCommissionSnapshot.ShouldBe(10_000m);
            secondAttribution.UserShareRate.ShouldBe(80m);
            secondAttribution.UserCommissionSnapshot.ShouldBe(8_000m);
            var secondNotifications = await _notifications.GetListAsync(x => x.UserId == userId &&
                x.EventKey == $"order:{second.Id:N}:pending");
            secondNotifications.Count.ShouldBe(1);
            secondNotifications[0].Message.ShouldContain("8.000");

            await _upserter.UpsertAsync(AffiliatePlatform.Shopee, SourceAt(firstOrderId,
                AffiliateOrderStatus.Cancelled, firstPurchaseTime,
                new SourceItem("ITEM-FIRST", firstToken, 10_000m)));

            second = await _orders.GetAsync(second.Id);
            secondAttribution = (await ActiveAttributionsAsync(second.Id)).Single();
            secondAttribution.UserShareRate.ShouldBe(100m);
            secondAttribution.UserCommissionSnapshot.ShouldBe(10_000m);
            second.UserCommissionSnapshot.ShouldBe(10_000m);
            secondNotifications = await _notifications.GetListAsync(x => x.UserId == userId &&
                x.EventKey == $"order:{second.Id:N}:pending");
            secondNotifications.Count.ShouldBe(1);
            secondNotifications[0].Message.ShouldContain("10.000");
        });
    }

    private async Task<List<AffiliateOrderItemAttribution>> ActiveAttributionsAsync(Guid orderId)
    {
        var itemIds = (await _items.GetListAsync(x => x.OrderId == orderId)).Select(x => x.Id).ToList();
        return itemIds.Count == 0
            ? new List<AffiliateOrderItemAttribution>()
            : await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId));
    }

    private async Task AssertSingleWalletMovementAsync(Guid userId, decimal amount)
    {
        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            var history = await _wallet.GetHistoryAsync(new WalletHistoryInput { MaxResultCount = 20 });
            history.Items.Count(x => x.Kind == WalletMovementKind.Commission).ShouldBe(1);
            history.Items.Single(x => x.Kind == WalletMovementKind.Commission).Amount.ShouldBe(amount);
        }
    }

    private async Task AddUserAndTrackingAsync(Guid userId, string token)
    {
        await _users.InsertAsync(new IdentityUser(userId, $"user-{userId:N}", $"user-{userId:N}@test.local"));
        await AddTrackingAsync(userId, token);
    }

    private Task AddTrackingAsync(Guid userId, string token)
    {
        var productId = Guid.NewGuid().ToString("N");
        return _trackings.InsertAsync(new AffiliateTracking(Guid.NewGuid(), userId, AffiliatePlatform.Shopee,
            token, $"https://shopee.vn/product/1/{productId}",
            $"https://shopee.vn/product/1/{productId}"));
    }

    private static NormalizedAffiliateConversion Source(string orderId, AffiliateOrderStatus status,
        params SourceItem[] items) => SourceAt(orderId, status, PurchaseTime, items);

    private static NormalizedAffiliateConversion SourceAt(string orderId, AffiliateOrderStatus status,
        DateTime purchaseTime, params SourceItem[] items)
    {
        var total = items.Sum(x => x.Commission);
        var normalizedItems = items.Select(x => new NormalizedAffiliateOrderItem(
            x.ItemId, "MODEL", x.ItemId, x.Commission * 10m, 1, x.Commission,
            0m, false, status.ToString(), new[]
            {
                new NormalizedAffiliateOrderItemAttribution(x.Token, x.Commission * 10m, 1,
                    x.Commission, 0m, false, status.ToString())
            })).ToList();
        var conversionStatus = status switch
        {
            AffiliateOrderStatus.Completed => AffiliateConversionStatus.Approved,
            AffiliateOrderStatus.Cancelled => AffiliateConversionStatus.Cancelled,
            AffiliateOrderStatus.Refunded => AffiliateConversionStatus.Refunded,
            AffiliateOrderStatus.Rejected => AffiliateConversionStatus.Rejected,
            _ => AffiliateConversionStatus.Pending
        };
        return new NormalizedAffiliateConversion(orderId, null, purchaseTime, null, conversionStatus,
            total, total, CommissionSource.NetCommission, new[]
            {
                new NormalizedAffiliateOrder(orderId, status, "Marketplace", total * 10m, total,
                    normalizedItems)
            });
    }

    private static string Token(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}";

    private static ClaimsPrincipal CreatePrincipal(Guid userId) => new(new ClaimsIdentity(new List<Claim>
    {
        new(AbpClaimTypes.UserId, userId.ToString()),
        new(AbpClaimTypes.UserName, $"user-{userId:N}")
    }, "TestAuth"));

    private sealed record SourceItem(string ItemId, string Token, decimal Commission);
}
