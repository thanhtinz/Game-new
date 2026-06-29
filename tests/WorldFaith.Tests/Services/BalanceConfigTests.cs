using FluentAssertions;
using MongoDB.Driver;
using Moq;
using WorldFaith.Server.Services.Admin;
using Xunit;

namespace WorldFaith.Tests.Services;

public class BalanceConfigDefaultsTests
{
    // ─── Default Values ──────────────────────────────────────

    [Fact]
    public void BalanceDefaults_AllKeysHaveValues()
    {
        foreach (var (key, (value, category, desc, type)) in BalanceDefaults.All)
        {
            key.Should().NotBeNullOrEmpty($"key should not be empty");
            value.Should().NotBeNullOrEmpty($"value for '{key}' should not be empty");
            category.Should().NotBeNullOrEmpty($"category for '{key}' should not be empty");
            desc.Should().NotBeNullOrEmpty($"description for '{key}' should not be empty");
            type.Should().BeOneOf("float", "int", "bool", "string");
        }
    }

    [Fact]
    public void BalanceDefaults_MiracleCosts_AllPositive()
    {
        var miracleCostKeys = BalanceDefaults.All
            .Where(kv => kv.Key.StartsWith("miracle.cost."))
            .ToList();

        miracleCostKeys.Should().NotBeEmpty();
        foreach (var (key, (value, _, _, _)) in miracleCostKeys)
        {
            float.TryParse(value, out float cost).Should().BeTrue($"{key} should be parseable float");
            cost.Should().BeGreaterThan(0f, $"{key} cost should be positive");
        }
    }

    [Fact]
    public void BalanceDefaults_RebirthInterval_IsReasonable()
    {
        BalanceDefaults.All.Should().ContainKey("world.rebirth_tick_interval");
        var (value, _, _, _) = BalanceDefaults.All["world.rebirth_tick_interval"];
        int.TryParse(value, out int interval).Should().BeTrue();
        interval.Should().BeGreaterThan(10, "rebirth interval must be > 10 ticks");
        interval.Should().BeLessOrEqualTo(10000, "rebirth interval should be reasonable");
    }

    [Fact]
    public void BalanceDefaults_FaithMaxIsSane()
    {
        var (value, _, _, _) = BalanceDefaults.All["faith.max_faith"];
        float.TryParse(value, out float maxFaith).Should().BeTrue();
        maxFaith.Should().BeGreaterThan(100f);
        maxFaith.Should().BeLessOrEqualTo(100000f);
    }

    [Fact]
    public void BalanceDefaults_EvolutionThresholds_HigherStageNeedsMorePoints()
    {
        float.TryParse(BalanceDefaults.All["evolution.wild_to_divine_pts"].value, out float tier1);
        float.TryParse(BalanceDefaults.All["evolution.divine_to_celestial"].value, out float tier2);

        tier2.Should().BeGreaterThan(tier1, "higher evolution stage should require more points");
    }

    [Fact]
    public void BalanceDefaults_HasAtLeast40Params()
    {
        BalanceDefaults.All.Count.Should().BeGreaterThanOrEqualTo(40,
            "should have at least 40 tunable balance parameters");
    }

    // ─── Categories ──────────────────────────────────────────

    [Theory]
    [InlineData("faith")]
    [InlineData("miracle")]
    [InlineData("religion")]
    [InlineData("evolution")]
    [InlineData("civ")]
    [InlineData("world")]
    public void BalanceDefaults_AllCategoriesPresent(string category)
    {
        BalanceDefaults.All.Values
            .Any(v => v.category == category)
            .Should().BeTrue($"category '{category}' should have at least one entry");
    }
}
