using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §9–§10 — race lifespan & aggregated commoner families.
public class PopulationFamilyServiceTests
{
    private readonly PopulationFamilyService _sut = new();

    [Fact]
    public void ElvesHaveLongerGenerationInterval_ThanHumans()
    {
        _sut.GenerationYears(RaceType.Elf)
            .Should().BeGreaterThan(_sut.GenerationYears(RaceType.Human), "long-lived races breed slowly");
    }

    [Fact]
    public void HealthyGroup_GrowsOverAGeneration()
    {
        var group = new PopulationFamilyGroup
        {
            Race = RaceType.Human, Count = 1000,
            BirthRatePerGeneration = 0.30f, DeathRatePerGeneration = 0.18f
        };

        int net = _sut.SimulateGeneration(group);
        net.Should().BePositive();
        group.Count.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void DyingGroup_ShrinksOverAGeneration()
    {
        var group = new PopulationFamilyGroup
        {
            Race = RaceType.Human, Count = 1000,
            BirthRatePerGeneration = 0.10f, DeathRatePerGeneration = 0.30f
        };

        _sut.SimulateGeneration(group).Should().BeNegative();
        group.Count.Should().BeLessThan(1000);
    }

    [Fact]
    public void Undead_DoNotReproduceNormally()
    {
        var group = new PopulationFamilyGroup
        {
            Race = RaceType.Undead, Count = 1000,
            BirthRatePerGeneration = 0.30f, DeathRatePerGeneration = 0f
        };

        // Fertility modifier 0 → no births; with no deaths the count is stable.
        _sut.SimulateGeneration(group);
        group.Count.Should().Be(1000);
    }
}
