using System.Net;
using Shouldly;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.Tests.Affiliates;

public class AffiliateNetworkSafetyTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("100.64.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.0.2.10", true)]
    [InlineData("198.18.0.1", true)]
    [InlineData("198.51.100.1", true)]
    [InlineData("203.0.113.1", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("fc00::1", true)]
    [InlineData("2001:db8::1", true)]
    [InlineData("1.1.1.1", false)]
    [InlineData("2606:4700:4700::1111", false)]
    public void Reserved_And_Private_Addresses_Should_Be_Rejected(string value, bool expected)
    {
        AffiliateNetworkSafety.IsPrivateOrReserved(IPAddress.Parse(value)).ShouldBe(expected);
    }
}
