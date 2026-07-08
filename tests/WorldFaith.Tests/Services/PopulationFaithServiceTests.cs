using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §10 — grouped commoner simulation with group splitting.
public class PopulationFaithServiceTests
{
    private readonly PopulationFaithService _sut = new();

    private static PopulationFaithGroup Farmers(int count, string? god = "old") => new()
    {
        WorldId = "w1", CivilizationId = "c1", RegionId = "r1",
        Race = RaceType.Human, Class = NpcTier.Commoner, GodId = god, Count = count
    };

    [Fact]
    public void TwoPercentPressure_ConvertsAndSplitsGroup()
    {
        var group = Farmers(1000);
        var groups = new List<PopulationFaithGroup> { group };

        int converts = _sut.ApplyGroupConversionPressure(groups, group, pressure: 0.02f, targetGodId: "nature");

        converts.Should().Be(20);
        group.Count.Should().Be(980, "the group splits rather than flipping everyone");
        groups.Should().HaveCount(2);
        groups.Single(g => g.GodId == "nature").Count.Should().Be(20);
    }

    [Fact]
    public void RepeatedConversion_MergesIntoExistingTargetGroup()
    {
        var group = Farmers(1000);
        var groups = new List<PopulationFaithGroup> { group };

        _sut.ApplyGroupConversionPressure(groups, group, 0.02f, "nature"); // 20
        _sut.ApplyGroupConversionPressure(groups, group, 0.02f, "nature"); // ~20 more, merged

        groups.Should().HaveCount(2, "converts merge into one target group, not many");
        groups.Single(g => g.GodId == "nature").Count.Should().BeGreaterThan(20);
    }

    [Fact]
    public void ZeroPressure_DoesNothing()
    {
        var group = Farmers(1000);
        var groups = new List<PopulationFaithGroup> { group };

        _sut.ApplyGroupConversionPressure(groups, group, 0f, "nature").Should().Be(0);
        group.Count.Should().Be(1000);
        groups.Should().HaveCount(1);
    }

    [Fact]
    public void Pressure_IsCappedAt25PercentPerEvent()
    {
        var group = Farmers(1000);
        var groups = new List<PopulationFaithGroup> { group };

        // Even with absurd pressure, at most 25% convert in one event.
        _sut.ApplyGroupConversionPressure(groups, group, 5f, "nature").Should().Be(250);
    }
}
