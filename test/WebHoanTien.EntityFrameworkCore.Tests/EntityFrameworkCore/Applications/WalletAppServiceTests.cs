using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Applications;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class WalletAppServiceTests : WebHoanTienEntityFrameworkCoreTestBase
{
    private static readonly byte[] ValidPng =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<UserPayoutAccount, Guid> _accounts;
    private readonly ICustomerWalletAppService _wallet;
    private readonly IAdminPayoutAppService _adminPayouts;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public WalletAppServiceTests()
    {
        _users = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _trackings = GetRequiredService<IRepository<AffiliateTracking, Guid>>();
        _conversions = GetRequiredService<IRepository<AffiliateConversion, Guid>>();
        _orders = GetRequiredService<IRepository<AffiliateOrder, Guid>>();
        _accounts = GetRequiredService<IRepository<UserPayoutAccount, Guid>>();
        _wallet = GetRequiredService<ICustomerWalletAppService>();
        _adminPayouts = GetRequiredService<IAdminPayoutAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Existing_Orders_Should_Drive_Balance_And_Cancel_Should_Release_It()
    {
        var userId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndAccountAsync(userId);
            await AddOrderAsync(userId, AffiliateOrderStatus.Settled, 50_000m);
            await AddOrderAsync(userId, AffiliateOrderStatus.Pending, 12_000m);
            await AddOrderAsync(userId, AffiliateOrderStatus.Refunded, 90_000m);
        });

        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            var initial = await _wallet.GetOverviewAsync();
            initial.TotalRecordedAmount.ShouldBe(50_000m);
            initial.PendingCommissionAmount.ShouldBe(12_000m);
            initial.AvailableBalance.ShouldBe(50_000m);

            var request = await _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 20_000m });
            (await _wallet.GetOverviewAsync()).AvailableBalance.ShouldBe(30_000m);

            var duplicate = await Should.ThrowAsync<BusinessException>(() =>
                _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 10_000m }));
            duplicate.Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalPendingExists);

            await _wallet.CancelWithdrawalRequestAsync(request.Id);
            var cancelled = await _wallet.GetOverviewAsync();
            cancelled.AvailableBalance.ShouldBe(50_000m);
            cancelled.PendingWithdrawal.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Paid_Request_Should_Store_Private_Proof_And_Subtract_Balance()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndAccountAsync(userId);
            await _users.InsertAsync(new IdentityUser(adminId, $"admin-{adminId:N}", $"admin-{adminId:N}@test.local"));
            await AddOrderAsync(userId, AffiliateOrderStatus.Settled, 30_000m);
        });

        Guid requestId;
        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            requestId = (await _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 20_000m })).Id;
        }

        using (_principalAccessor.Change(CreatePrincipal(adminId)))
        {
            var paid = await _adminPayouts.MarkPaidAsync(requestId, new MarkWithdrawalPaidInput
            {
                PaymentReference = "BANK-123",
                PaidAt = DateTime.UtcNow.AddSeconds(1)
            }, new MemoryStream(ValidPng), "proof.png", "image/png", ValidPng.Length, CancellationToken.None);
            paid.Status.ShouldBe(WithdrawalRequestStatus.Paid);
            paid.HasProof.ShouldBeTrue();
            (await _adminPayouts.GetProofAsync(requestId)).Content.ShouldBe(ValidPng);

            var duplicate = await _adminPayouts.MarkPaidAsync(requestId, new MarkWithdrawalPaidInput
            {
                PaymentReference = "BANK-123",
                PaidAt = DateTime.UtcNow.AddSeconds(1)
            }, new MemoryStream(ValidPng), "proof.png", "image/png", ValidPng.Length, CancellationToken.None);
            duplicate.Status.ShouldBe(WithdrawalRequestStatus.Paid);
        }

        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            (await _wallet.GetOverviewAsync()).AvailableBalance.ShouldBe(10_000m);
            (await _wallet.GetProofAsync(requestId)).Content.ShouldBe(ValidPng);
        }

        using (_principalAccessor.Change(CreatePrincipal(Guid.NewGuid())))
        {
            (await Should.ThrowAsync<BusinessException>(() => _wallet.GetProofAsync(requestId)))
                .Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalNotOwned);
        }
    }

    [Fact]
    public async Task Admin_Must_Not_Pay_When_Completed_Order_Was_Reversed()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        Guid orderId = Guid.Empty;
        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndAccountAsync(userId);
            await _users.InsertAsync(new IdentityUser(adminId, $"admin-{adminId:N}", $"admin-{adminId:N}@test.local"));
            orderId = await AddOrderAsync(userId, AffiliateOrderStatus.Settled, 20_000m);
        });

        Guid requestId;
        using (_principalAccessor.Change(CreatePrincipal(userId)))
            requestId = (await _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 15_000m })).Id;

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await _orders.GetAsync(orderId);
            order.Update(AffiliateOrderStatus.Refunded, order.ShopType, order.PurchaseAmount,
                order.NetCommission, order.UserCommissionSnapshot);
            await _orders.UpdateAsync(order);
        });

        using (_principalAccessor.Change(CreatePrincipal(adminId)))
        {
            var exception = await Should.ThrowAsync<BusinessException>(() => _adminPayouts.MarkPaidAsync(requestId,
                new MarkWithdrawalPaidInput { PaymentReference = "BANK-FAIL", PaidAt = DateTime.UtcNow.AddSeconds(1) },
                new MemoryStream(ValidPng), "proof.png", "image/png", ValidPng.Length, CancellationToken.None));
            exception.Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalNotBacked);
        }
    }

    [Fact]
    public async Task Withdrawal_Should_Require_Account_And_Available_Balance()
    {
        var userId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await _users.InsertAsync(new IdentityUser(userId, $"user-{userId:N}", $"user-{userId:N}@test.local"));
            await AddOrderAsync(userId, AffiliateOrderStatus.Settled, 20_000m);
        });

        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            (await Should.ThrowAsync<BusinessException>(() =>
                _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 10_000m })))
                .Code.ShouldBe(WebHoanTienDomainErrorCodes.PayoutAccountRequired);

            await WithUnitOfWorkAsync(() => _accounts.InsertAsync(
                new UserPayoutAccount(Guid.NewGuid(), userId, "VCB", "123456789", "Owner")));

            (await Should.ThrowAsync<BusinessException>(() =>
                _wallet.CreateWithdrawalRequestAsync(new CreateWithdrawalRequestInput { Amount = 25_000m })))
                .Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalInsufficientBalance);
        }
    }

    [Fact]
    public async Task Rejected_Request_Should_Release_Balance_And_Block_Further_Processing()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await AddUserAndAccountAsync(userId);
            await _users.InsertAsync(new IdentityUser(adminId, $"admin-{adminId:N}", $"admin-{adminId:N}@test.local"));
            await AddOrderAsync(userId, AffiliateOrderStatus.Settled, 30_000m);
        });

        Guid requestId;
        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            requestId = (await _wallet.CreateWithdrawalRequestAsync(
                new CreateWithdrawalRequestInput { Amount = 20_000m })).Id;
        }

        using (_principalAccessor.Change(CreatePrincipal(adminId)))
        {
            var rejected = await _adminPayouts.RejectAsync(requestId,
                new RejectWithdrawalInput { Reason = "Thông tin chuyển khoản không hợp lệ" });
            rejected.Status.ShouldBe(WithdrawalRequestStatus.Rejected);

            var invalidPayment = await Should.ThrowAsync<BusinessException>(() => _adminPayouts.MarkPaidAsync(requestId,
                new MarkWithdrawalPaidInput { PaymentReference = "BANK-LATE", PaidAt = DateTime.UtcNow.AddSeconds(1) },
                new MemoryStream(ValidPng), "proof.png", "image/png", ValidPng.Length, CancellationToken.None));
            invalidPayment.Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
        }

        using (_principalAccessor.Change(CreatePrincipal(userId)))
        {
            (await _wallet.GetOverviewAsync()).AvailableBalance.ShouldBe(30_000m);
            var invalidCancellation = await Should.ThrowAsync<BusinessException>(() =>
                _wallet.CancelWithdrawalRequestAsync(requestId));
            invalidCancellation.Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
        }
    }

    private async Task AddUserAndAccountAsync(Guid userId)
    {
        await _users.InsertAsync(new IdentityUser(userId, $"user-{userId:N}", $"user-{userId:N}@test.local"));
        await _accounts.InsertAsync(new UserPayoutAccount(Guid.NewGuid(), userId, "VCB", "00123456789", "Owner Name"));
    }

    private async Task<Guid> AddOrderAsync(Guid userId, AffiliateOrderStatus status, decimal commission)
    {
        var trackingId = Guid.NewGuid();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await _trackings.InsertAsync(new AffiliateTracking(trackingId, userId, AffiliatePlatform.Shopee, token,
            $"https://shopee.vn/product/1/{Math.Abs(trackingId.GetHashCode())}",
            $"https://shopee.vn/product/1/{Math.Abs(trackingId.GetHashCode())}"));

        var conversionId = Guid.NewGuid();
        var conversion = new AffiliateConversion(conversionId, AffiliatePlatform.Shopee,
            $"CONV-{conversionId:N}", DateTime.UtcNow);
        conversion.MapTo(trackingId, userId, token);
        conversion.ApplyCommission(commission, commission, CommissionSource.NetCommission, 100m);
        await _conversions.InsertAsync(conversion);

        var orderId = Guid.NewGuid();
        var order = new AffiliateOrder(orderId, conversionId, $"ORDER-{orderId:N}");
        order.Update(status == AffiliateOrderStatus.Settled ? AffiliateOrderStatus.Completed : status,
            "Marketplace", commission * 10m, commission, commission);
        if (status == AffiliateOrderStatus.Settled)
            order.Settle(commission, commission, $"SETTLEMENT-{orderId:N}", DateTime.UtcNow);
        await _orders.InsertAsync(order);
        return orderId;
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId) => new(new ClaimsIdentity(new List<Claim>
    {
        new(AbpClaimTypes.UserId, userId.ToString()),
        new(AbpClaimTypes.UserName, $"user-{userId:N}"),
        new(AbpClaimTypes.Email, $"user-{userId:N}@test.local")
    }, "Test"));
}
