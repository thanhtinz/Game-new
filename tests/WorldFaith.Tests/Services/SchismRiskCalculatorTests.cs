using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Religion;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §7 evolution / §15 — schism risk from doctrine disagreement.
public class SchismRiskCalculatorTests
{
    private static DoctrineValues Punishment() => new() { MercyVsPunishment = 90f, FreedomVsOrder = 80f };
    private static DoctrineValues Mercy()      => new() { MercyVsPunishment = -90f, FreedomVsOrder = -60f };

    [Fact]
    public void OpposedFactions_HaveGreaterDistanceThanIdentical()
    {
        SchismRiskCalculator.DoctrineDistance(Punishment(), Mercy())
            .Should().BeGreaterThan(SchismRiskCalculator.DoctrineDistance(Punishment(), Punishment()));
    }

    [Fact]
    public void LargeDissatisfiedOpposedFaction_HasHighSchismRisk()
    {
        float risk = SchismRiskCalculator.CalculateSchismRisk(
            Punishment(), Mercy(), factionSizeRatio: 0.4f, dissatisfaction: 0.9f);

        risk.Should().BeGreaterThan(0.3f, "a big unhappy mercy faction splits from a punishment church");
    }

    [Fact]
    public void ContentAlignedFaction_HasNoSchismRisk()
    {
        SchismRiskCalculator.CalculateSchismRisk(
            Punishment(), Punishment(), factionSizeRatio: 0.4f, dissatisfaction: 0.0f)
            .Should().Be(0f);
    }

    [Fact]
    public void SplinterDoctrine_SitsBetweenChurchAndFaction()
    {
        var splinter = SchismRiskCalculator.CreateSplinterDoctrine(Punishment(), Mercy(), shift: 0.6f);

        splinter.MercyVsPunishment.Should().BeLessThan(90f, "the sect moves toward mercy");
        splinter.MercyVsPunishment.Should().BeGreaterThan(-90f, "but does not fully become the faction");
    }
}
