using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.Tests.Affiliates;

public class WalletDomainTests
{
    [Fact]
    public void Request_Should_Enforce_Minimum_Withdrawal_Amount()
    {
        var userId = Guid.NewGuid();
        var account = new UserPayoutAccount(Guid.NewGuid(), userId, "VCB", "123456", "Owner");

        Should.Throw<BusinessException>(() => new WithdrawalRequest(Guid.NewGuid(), userId, "CB-MIN", account,
            WebHoanTienConsts.MinimumWithdrawalAmount - 1m, 0m)).Code
            .ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalBelowMinimum);
    }

    [Fact]
    public void Balance_Should_Subtract_Only_Pending_And_Paid_Withdrawals()
    {
        var balance = new WalletBalanceSnapshot(100_000m, 25_000m, 20_000m, 30_000m);

        balance.RawBalance.ShouldBe(50_000m);
        balance.AvailableBalance.ShouldBe(50_000m);
        new WalletBalanceSnapshot(10_000m, 0m, 20_000m, 5_000m).AvailableBalance.ShouldBe(0m);
    }

    [Fact]
    public void Request_Should_Snapshot_Bank_And_Allow_Only_One_Final_Transition()
    {
        var userId = Guid.NewGuid();
        var account = new UserPayoutAccount(Guid.NewGuid(), userId, "VCB", "00123456789", "Nguyen Van A");
        var request = new WithdrawalRequest(Guid.NewGuid(), userId, "cb-001", account, 50_000m, 0m);
        account.Update("ACB", "999999999", "Nguyen Van B");

        request.BankCode.ShouldBe("VCB");
        request.AccountNumber.ShouldBe("00123456789");
        request.AccountHolderName.ShouldBe("NGUYEN VAN A");
        request.NetAmount.ShouldBe(50_000m);

        request.MarkPaid(Guid.NewGuid(), "TXN-001", DateTime.UtcNow, " paid ");
        request.Status.ShouldBe(WithdrawalRequestStatus.Paid);
        request.PaymentReference.ShouldBe("TXN-001");
        request.AdminNote.ShouldBe("paid");

        var exception = Should.Throw<BusinessException>(() =>
            request.Reject(Guid.NewGuid(), "duplicate", DateTime.UtcNow, null));
        exception.Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
    }

    [Fact]
    public void User_Can_Only_Cancel_Own_Pending_Request()
    {
        var userId = Guid.NewGuid();
        var account = new UserPayoutAccount(Guid.NewGuid(), userId, "VCB", "123456", "Owner");
        var request = new WithdrawalRequest(Guid.NewGuid(), userId, "CB-002", account, 10_000m, 0m);

        Should.Throw<BusinessException>(() => request.Cancel(Guid.NewGuid(), DateTime.UtcNow))
            .Code.ShouldBe(WebHoanTienDomainErrorCodes.WithdrawalNotOwned);

        request.Cancel(userId, DateTime.UtcNow);
        request.Status.ShouldBe(WithdrawalRequestStatus.Cancelled);
    }

    [Fact]
    public async Task Proof_Validator_Should_Check_Signature_Mime_Extension_And_Hash()
    {
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };
        var validator = new WithdrawalProofValidator();

        var proof = await validator.ReadAsync(new MemoryStream(content), "payment.png", "image/png",
            content.Length, CancellationToken.None);

        proof.FileName.ShouldBe("payment.png");
        proof.ContentType.ShouldBe("image/png");
        proof.Sha256.Length.ShouldBe(64);

        await Should.ThrowAsync<BusinessException>(() => validator.ReadAsync(new MemoryStream(content),
            "payment.jpg", "image/jpeg", content.Length, CancellationToken.None));
        await Should.ThrowAsync<BusinessException>(() => validator.ReadAsync(new MemoryStream(new byte[] { 1, 2, 3 }),
            "payment.png", "image/png", 3, CancellationToken.None));
    }
}
