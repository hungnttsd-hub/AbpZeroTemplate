using System;
using System.Collections.Generic;
using System.Linq;
using WebHoanTien.WordyWings;
using Xunit;

namespace WebHoanTien.WordyWings.Tests;
public class HideSeekScoringTests
{
    private const int Seed = 731;
    private static readonly System.Text.Json.JsonElement Level = HideSeekContent.Get("HS-01", "1.0.0");
    private static string Spot(string entity) => HideSeekContent.Occupants(Level, Seed).Single(p => p.Value == entity).Key;
    private static HideSeekDecision Decision(string entity, bool yes, Guid? reveal = null) => new(Guid.NewGuid(), reveal ?? Guid.NewGuid(), Spot(entity), yes);
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 2)]
    public void Distractor_then_target_uses_real_remaining_stars(bool wrong, int expected)
    {
        var result = HideSeekScoring.Grade(Level, Seed, new[] { Decision("dog", wrong), Decision("cat", true) });
        Assert.Equal(expected, result.Stars); Assert.False(result.Assisted);
    }
    [Fact]
    public void Correct_NO_cannot_complete()
    {
        Assert.Throws<ArgumentException>(() => HideSeekScoring.Grade(Level, Seed, new[] { Decision("dog", false) }));
    }
    [Fact]
    public void Assisted_retries_cost_once_and_never_count_as_independent()
    {
        var reveal = Guid.NewGuid();
        var result = HideSeekScoring.Grade(Level, Seed, new[] { Decision("cat", false, reveal), Decision("cat", false, reveal), Decision("cat", true, reveal) });
        Assert.Equal(2, result.Stars); Assert.True(result.Assisted); Assert.Equal(0, result.IndependentCorrect); Assert.Equal(1, result.IndependentWrong);
    }
    [Fact]
    public void Target_can_complete_at_zero_stars()
    {
        var decisions = new[] { Decision("dog", true), Decision("tiger", true), Decision("scissors", true), Decision("cat", true) };
        Assert.Equal(0, HideSeekScoring.Grade(Level, Seed, decisions).Stars);
    }
    [Fact]
    public void Duplicate_event_is_noop_but_conflicting_payload_is_rejected()
    {
        var wrong = Decision("dog", true); var yes = Decision("cat", true);
        Assert.Equal(2, HideSeekScoring.Grade(Level, Seed, new[] { wrong, wrong, yes, yes }).Stars);
        Assert.Throws<ArgumentException>(() => HideSeekScoring.Grade(Level, Seed, new[] { wrong, wrong with { Answer = false }, yes }));
    }
    [Fact]
    public void Reusing_reveal_or_spot_and_skipping_assisted_retry_are_rejected()
    {
        var wrong = Decision("dog", true);
        Assert.Throws<ArgumentException>(() => HideSeekScoring.Grade(Level, Seed, new[] { wrong, Decision("cat", true, wrong.RevealId) }));
        Assert.Throws<ArgumentException>(() => HideSeekScoring.Grade(Level, Seed, new[] { wrong, Decision("dog", false), Decision("cat", true) }));
        Assert.Throws<ArgumentException>(() => HideSeekScoring.Grade(Level, Seed, new[] { Decision("cat", false), Decision("dog", false), Decision("cat", true) }));
    }
    [Fact]
    public void All_six_levels_use_their_authored_target()
    {
        Assert.Equal(6, HideSeekContent.Levels.Count());
        foreach (var level in HideSeekContent.Levels) {
            var target = level.GetProperty("targetEntityId").GetString();
            var spot = HideSeekContent.Occupants(level, Seed).Single(p => p.Value == target).Key;
            var result = HideSeekScoring.Grade(level, Seed, new[] { new HideSeekDecision(Guid.NewGuid(), Guid.NewGuid(), spot, true) });
            Assert.Equal(3, result.Stars);
        }
    }
    [Fact]
    public void Client_and_server_share_xorshift_mapping_fixture()
    {
        // This fixture is independently checked by the TypeScript test suite.
        Assert.Equal(new[] { "cat", "dog", "scissors", "tiger" }, HideSeekContent.Occupants(Level, 731).Values);
    }
}
