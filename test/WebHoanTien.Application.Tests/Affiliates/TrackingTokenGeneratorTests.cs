using System.Collections.Generic;
using Shouldly;
using WebHoanTien.Affiliates;
using Xunit;

namespace WebHoanTien.Tests.Affiliates;

public class TrackingTokenGeneratorTests
{
    [Fact]
    public void Create_Should_Return_Unique_Hex_Tokens_Without_Separators()
    {
        var generator = new TrackingTokenGenerator();
        var tokens = new HashSet<string>();

        for (var index = 0; index < 100; index++)
        {
            var token = generator.Create();

            token.Length.ShouldBe(WebHoanTienConsts.TrackingTokenLength);
            token.ShouldMatch("^[0-9a-f]{32}$");
            tokens.Add(token).ShouldBeTrue();
        }
    }
}
