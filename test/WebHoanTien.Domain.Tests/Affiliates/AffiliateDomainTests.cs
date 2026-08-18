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

    [Theory]
    [InlineData("https://shopee.vn/san-pham-i.123.456", true)]
    [InlineData("https://s.shopee.vn/abc", true)]
    [InlineData("http://shopee.vn/item", false)]
    [InlineData("https://shopee.vn.evil.example/item", false)]
    [InlineData("https://127.0.0.1/item", false)]
    public void Url_Normalizer_Should_Use_Exact_Https_Allowlist(string url, bool expected)
    {
        new ShopeeUrlNormalizer().TryNormalize(url, out _, out _).ShouldBe(expected);
    }
}
