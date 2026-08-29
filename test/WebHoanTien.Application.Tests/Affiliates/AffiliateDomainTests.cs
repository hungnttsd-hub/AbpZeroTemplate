using System;
using System.Linq;
using Shouldly;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.Tests.Affiliates;

public class AffiliateDomainTests
{
    private readonly AffiliateCommissionCalculator _calculator = new();

    [Fact]
    public void Should_Calculate_Seventy_Percent_In_Vnd()
    {
        _calculator.CalculateUserCommission(100000m, 70m).ShouldBe(70000m);
    }

    [Theory]
    [InlineData(1, 60, 100)]
    [InlineData(2, 60, 80)]
    [InlineData(3, 60, 60)]
    [InlineData(4, 60, 60)]
    [InlineData(5, 60, 60)]
    [InlineData(10, 70, 70)]
    public void User_Share_Rate_Should_Follow_Introductory_Order_Tiers(int orderNumber,
        decimal configuredRate, decimal expectedRate)
    {
        AffiliateUserShareRatePolicy.Resolve(orderNumber, configuredRate).ShouldBe(expectedRate);
    }

    [Fact]
    public void Allocation_Should_Keep_Exact_Totals_And_Deterministic_Residual()
    {
        var result = _calculator.Allocate(100001m, 70m, new[]
        {
            new CommissionAllocationInput("b", 1m),
            new CommissionAllocationInput("a", 1m),
            new CommissionAllocationInput("c", 1m)
        });
        result.Sum(x => x.NetCommission).ShouldBe(100001m);
        result.Sum(x => x.UserCommission).ShouldBe(70001m);
        result.Select(x => x.Key).ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Cancelled_Conversion_Should_Keep_Snapshot_But_Pay_Zero()
    {
        var conversion = new AffiliateConversion(Guid.NewGuid(), AffiliatePlatform.Shopee, "conversion-1", DateTime.UtcNow);
        conversion.ApplyCommission(100000m, 100000m, CommissionSource.NetCommission, 70m);
        conversion.ChangeStatus(AffiliateConversionStatus.Cancelled, DateTime.UtcNow);
        conversion.UserCommissionSnapshot.ShouldBe(70000m);
        conversion.PayableUserCommission.ShouldBe(0m);
    }

    [Fact]
    public void Completed_Order_Should_Not_Be_Payable_Until_Settled()
    {
        var order = new AffiliateOrder(Guid.NewGuid(), Guid.NewGuid(), "ORDER-1");
        order.Update(AffiliateOrderStatus.Completed, null, 100_000m, 10_000m, 6_000m);

        order.PayableUserCommission.ShouldBe(0m);

        order.Settle(9_000m, 5_400m, "BK-001", DateTime.UtcNow);

        order.Status.ShouldBe(AffiliateOrderStatus.Settled);
        order.PayableUserCommission.ShouldBe(5_400m);
        order.SettledNetCommission.ShouldBe(9_000m);
    }

    [Fact]
    public void Closing_Rule_Should_Preserve_Its_Historical_Period()
    {
        var effectiveFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var effectiveTo = effectiveFrom.AddDays(1);
        var rule = new AffiliateCommissionRule(Guid.NewGuid(), AffiliatePlatform.Shopee, 70m, effectiveFrom, null);

        rule.CloseAt(effectiveTo);

        rule.AppliesAt(effectiveTo.AddTicks(-1)).ShouldBeTrue();
        rule.AppliesAt(effectiveTo).ShouldBeFalse();
    }

    [Theory]
    [InlineData("https://shopee.vn/san-pham-i.123.456", true)]
    [InlineData("https://s.shopee.vn/abc", true)]
    [InlineData("https://s.shopee.vn/an_redir?origin_link=https%3A%2F%2Fshopee.vn%2Fproduct%2F1%2F2&affiliate_id=123&sub_id=token", true)]
    [InlineData("https://vn.shp.ee/XQpAhLd5", true)]
    [InlineData("http://shopee.vn/item", false)]
    [InlineData("https://shopee.vn.evil.example/item", false)]
    [InlineData("https://127.0.0.1/item", false)]
    public void Url_Normalizer_Should_Use_Exact_Https_Allowlist(string url, bool expected)
    {
        new ShopeeUrlNormalizer().TryNormalize(url, out _, out _).ShouldBe(expected);
    }

    [Fact]
    public void Url_Normalizer_Should_Canonicalize_Product_Links()
    {
        var valid = new ShopeeUrlNormalizer().TryNormalize(
            "https://shopee.vn/dep-suc-nam-nu-i.123.456?affiliate_id=old&uls_trackid=one", out var normalized, out var itemId);

        valid.ShouldBeTrue();
        normalized.ShouldBe("https://shopee.vn/product/123/456");
        itemId.ShouldBe("456");
    }

    [Fact]
    public void Url_Normalizer_Should_Canonicalize_Opaanlp_Links()
    {
        var valid = new ShopeeUrlNormalizer().TryNormalize(
            "https://shopee.vn/opaanlp/1126579299/21187242685?__mobile__=1&utm_source=old", out var normalized, out var itemId);

        valid.ShouldBeTrue();
        normalized.ShouldBe("https://shopee.vn/product/1126579299/21187242685");
        itemId.ShouldBe("21187242685");
    }

    [Fact]
    public void Url_Normalizer_Should_Ignore_Varying_Shopee_Tracking_Parameters()
    {
        var normalizer = new ShopeeUrlNormalizer();
        normalizer.TryNormalize("https://shopee.vn/product/123/456?d_id=one&uls_trackid=first", out var first, out _).ShouldBeTrue();
        normalizer.TryNormalize("https://shopee.vn/product/123/456?d_id=two&uls_trackid=second", out var second, out _).ShouldBeTrue();

        first.ShouldBe(second);
        first.ShouldBe("https://shopee.vn/product/123/456");
    }

    [Fact]
    public void Url_Normalizer_Should_Remove_All_Query_Parameters_From_Non_Product_Links()
    {
        var valid = new ShopeeUrlNormalizer().TryNormalize(
            "https://shopee.vn/collection/123?affiliate_id=old&sub_id=old&foo=bar", out var normalized, out _);

        valid.ShouldBeTrue();
        normalized.ShouldBe("https://shopee.vn/collection/123");
    }

    [Fact]
    public void Payout_Account_Should_Normalize_Bank_And_Holder_Name()
    {
        var account = new UserPayoutAccount(Guid.NewGuid(), Guid.NewGuid(), " vcb ", "00123456789", " Nguyễn Văn A ");

        account.BankCode.ShouldBe("VCB");
        account.AccountNumber.ShouldBe("00123456789");
        account.AccountHolderName.ShouldBe("NGUYỄN VĂN A");
    }

    [Fact]
    public void Tracking_Should_Hide_And_Restore_Without_Changing_Identity()
    {
        var trackingId = Guid.NewGuid();
        var tracking = new AffiliateTracking(trackingId, Guid.NewGuid(), AffiliatePlatform.Shopee, "token-1",
            "https://shopee.vn/product/1/2", "https://shopee.vn/product/1/2");
        var hiddenAt = DateTime.UtcNow;

        tracking.Hide(hiddenAt);

        tracking.Id.ShouldBe(trackingId);
        tracking.TrackingToken.ShouldBe("token-1");
        tracking.IsHidden.ShouldBeTrue();
        tracking.HiddenAt.ShouldBe(hiddenAt);

        tracking.Show();

        tracking.Id.ShouldBe(trackingId);
        tracking.TrackingToken.ShouldBe("token-1");
        tracking.IsHidden.ShouldBeFalse();
        tracking.HiddenAt.ShouldBeNull();
    }
}
