using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §3.2 — soft population capacity & migration.
public class PopulationPressureServiceTests
{
    private readonly PopulationPressureService _sut = new();
    private static CapacityFactors Rich() => new(90, 90, 90, 90, 90);

    [Fact]
    public void ScarceFood_CapsCapacityLow()
    {
        float rich = _sut.ComputeSoftCapacity(Rich(), 1000f);
        float starving = _sut.ComputeSoftCapacity(new CapacityFactors(5, 90, 90, 90, 90), 1000f);

        starving.Should().BeLessThan(rich, "the scarcest resource limits capacity");
    }

    [Fact]
    public void UnderCapacity_Grows()
    {
        float cap = _sut.ComputeSoftCapacity(Rich(), 1000f);
        _sut.Evaluate((int)(cap * 0.5f), cap).Should().Be(PopulationOutcome.Growing);
    }

    [Fact]
    public void SlightlyOverCapacity_Migrates()
    {
        float cap = _sut.ComputeSoftCapacity(Rich(), 1000f);
        _sut.Evaluate((int)(cap * 1.1f), cap).Should().Be(PopulationOutcome.Migrating);
    }

    [Fact]
    public void FarOverCapacity_Declines()
    {
        float cap = _sut.ComputeSoftCapacity(Rich(), 1000f);
        _sut.Evaluate((int)(cap * 2.0f), cap).Should().Be(PopulationOutcome.Declining);
    }

    [Fact]
    public void ZeroCapacity_AlwaysDeclines()
    {
        _sut.Evaluate(10, 0f).Should().Be(PopulationOutcome.Declining);
    }
}
