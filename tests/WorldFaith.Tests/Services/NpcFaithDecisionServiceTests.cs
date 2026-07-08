using FluentAssertions;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §13 — belief-math formulas must be tested or balancing breaks silently.
public class NpcFaithDecisionServiceTests
{
    private readonly NpcFaithDecisionService _sut = new(new RandomService(seed: 12345));

    [Fact]
    public void FanaticNpc_ShouldHaveLowerDoubtChance()
    {
        var baseCtx = new NpcFaithContext { Doubt = 60f, Trust = 40f, Openness = 70f };
        var fanaticCtx = new NpcFaithContext
        {
            Doubt = 60f, Trust = 40f, Openness = 70f,
            Traits = { NpcTrait.Fanatic }
        };

        float normal = _sut.CalculateDoubtChance(baseCtx);
        float fanatic = _sut.CalculateDoubtChance(fanaticCtx);

        fanatic.Should().BeLessThan(normal, "fanatics resist doubt");
    }

    [Fact]
    public void CuriousNpc_ShouldHaveHigherDoubtChance()
    {
        var baseCtx = new NpcFaithContext { Doubt = 50f, Trust = 40f, Openness = 60f };
        var curiousCtx = new NpcFaithContext
        {
            Doubt = 50f, Trust = 40f, Openness = 60f,
            Traits = { NpcTrait.Curious }
        };

        _sut.CalculateDoubtChance(curiousCtx)
            .Should().BeGreaterThan(_sut.CalculateDoubtChance(baseCtx));
    }

    [Fact]
    public void ConversionChance_IsClampedBetween1And95Percent()
    {
        // Extreme low: everything near zero
        var lowCtx = new NpcFaithContext
        {
            BaseOpenness = 0f, RaceAffinity = 0f, TraitModifier = 0f,
            SocialPressure = 0f, RecentEventImpact = 0f, TrustDifference = 0f, FearPressure = 0f
        };
        // Extreme high: everything large
        var highCtx = new NpcFaithContext
        {
            BaseOpenness = 5f, RaceAffinity = 5f, TraitModifier = 5f,
            SocialPressure = 5f, RecentEventImpact = 5f, TrustDifference = 5f, FearPressure = 5f
        };

        _sut.CalculateConversionChance(lowCtx).Should().Be(0.01f);
        _sut.CalculateConversionChance(highCtx).Should().Be(0.95f);
    }

    [Fact]
    public void FanaticObedience_ShouldExceedRecklessObedience()
    {
        var fanatic = new NpcFaithContext { Faith = 60f, Trust = 60f, Loyalty = 60f, Traits = { NpcTrait.Fanatic } };
        var reckless = new NpcFaithContext { Faith = 60f, Trust = 60f, Loyalty = 60f, Traits = { NpcTrait.Reckless } };

        _sut.CalculateObedienceChance(fanatic)
            .Should().BeGreaterThan(_sut.CalculateObedienceChance(reckless));
    }

    [Fact]
    public void EvaluateConversion_AlwaysReturnsReason()
    {
        var result = _sut.EvaluateConversion(new NpcFaithContext());
        result.Reason.Should().NotBeNullOrEmpty("every faith decision must carry a reason for UI/debug");
    }
}

public class TraitAffinityTests
{
    [Fact]
    public void GeniusTrait_BoostsKnowledgeAffinity()
    {
        float withGenius = TraitAffinity.GetTraitModifier(new[] { NpcTrait.Genius }, GodArchetype.Knowledge);
        float without    = TraitAffinity.GetTraitModifier(System.Array.Empty<NpcTrait>(), GodArchetype.Knowledge);

        withGenius.Should().BeGreaterThan(without, "a genius is drawn to a Knowledge god");
    }

    [Fact]
    public void TraitModifier_NeverHardLocks_StaysWithinBounds()
    {
        float mod = TraitAffinity.GetTraitModifier(
            new[] { NpcTrait.Genius, NpcTrait.Fanatic, NpcTrait.Compassionate }, GodArchetype.Knowledge);

        mod.Should().BeInRange(0.10f, 1.80f, "affinity nudges probability but never guarantees or forbids belief");
    }
}
