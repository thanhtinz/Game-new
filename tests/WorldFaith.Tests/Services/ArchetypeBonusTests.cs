using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

public class ArchetypeBonusTests
{
    // ─── Cost Multipliers ────────────────────────────────────

    [Fact]
    public void GetMiracleCostMultiplier_LightGod_HealFollowerFree()
    {
        var mult = ArchetypeBonus.GetMiracleCostMultiplier(GodArchetype.Light, MiracleType.HealFollower);
        mult.Should().Be(0f);
    }

    [Fact]
    public void GetMiracleCostMultiplier_OrderGod_RevelationDiscounted()
    {
        var mult = ArchetypeBonus.GetMiracleCostMultiplier(GodArchetype.Order, MiracleType.Revelation);
        mult.Should().Be(0.80f);
    }

    [Fact]
    public void GetMiracleCostMultiplier_WarGod_HolyWarDiscounted()
    {
        var mult = ArchetypeBonus.GetMiracleCostMultiplier(GodArchetype.War, MiracleType.HolyWar);
        mult.Should().Be(0.70f);
    }

    [Fact]
    public void GetMiracleCostMultiplier_DarknessGod_CurseHalved()
    {
        var mult = ArchetypeBonus.GetMiracleCostMultiplier(GodArchetype.Darkness, MiracleType.Curse);
        mult.Should().Be(0.5f);
    }

    [Fact]
    public void GetMiracleCostMultiplier_NoBonus_ReturnsOne()
    {
        var mult = ArchetypeBonus.GetMiracleCostMultiplier(GodArchetype.Nature, MiracleType.HolyWar);
        mult.Should().Be(1f);
    }

    // ─── Effect Multipliers ──────────────────────────────────

    [Fact]
    public void GetMiracleEffectMultiplier_DarknessGod_CurseDoubled()
    {
        var mult = ArchetypeBonus.GetMiracleEffectMultiplier(GodArchetype.Darkness, MiracleType.Curse);
        mult.Should().Be(2.0f);
    }

    [Fact]
    public void GetMiracleEffectMultiplier_KnowledgeGod_DivineVoiceAmplified()
    {
        var mult = ArchetypeBonus.GetMiracleEffectMultiplier(GodArchetype.Knowledge, MiracleType.DivineVoice);
        mult.Should().Be(1.5f);
    }

    [Fact]
    public void GetMiracleEffectMultiplier_ChaosGod_IsRandom()
    {
        // Chaos is 0.8..1.6 — call multiple times, verify in range
        for (int i = 0; i < 20; i++)
        {
            var mult = ArchetypeBonus.GetMiracleEffectMultiplier(GodArchetype.Chaos, MiracleType.Storm);
            mult.Should().BeInRange(0.79f, 1.61f);
        }
    }

    [Fact]
    public void GetMiracleEffectMultiplier_NoBonus_ReturnsOne()
    {
        var mult = ArchetypeBonus.GetMiracleEffectMultiplier(GodArchetype.Order, MiracleType.Storm);
        mult.Should().Be(1f);
    }

    // ─── Faith Gen Multiplier ────────────────────────────────

    [Fact]
    public void GetFaithGenMultiplier_LightGod_HighTrust_HasBonus()
    {
        var god = new GodDocument
        {
            Archetype = GodArchetype.Light,
            Trust = 80f  // > 70 triggers +20%
        };
        var mult = ArchetypeBonus.GetFaithGenMultiplier(god, new List<ReligionDocument>(), new List<CivilizationDocument>());
        mult.Should().Be(1.2f);
    }

    [Fact]
    public void GetFaithGenMultiplier_LightGod_LowTrust_NoBonus()
    {
        var god = new GodDocument { Archetype = GodArchetype.Light, Trust = 50f };
        var mult = ArchetypeBonus.GetFaithGenMultiplier(god, new List<ReligionDocument>(), new List<CivilizationDocument>());
        mult.Should().Be(1f);
    }

    [Fact]
    public void GetFaithGenMultiplier_Capped_AtThreeX()
    {
        // Even with extreme bonuses, should be capped at 3x
        var god = new GodDocument { Archetype = GodArchetype.Order, Trust = 90f };
        var religions = Enumerable.Range(0, 100)
            .Select(_ => new ReligionDocument { TempleCount = 100 })
            .ToList();
        var mult = ArchetypeBonus.GetFaithGenMultiplier(god, religions, new List<CivilizationDocument>());
        mult.Should().BeLessOrEqualTo(3f);
    }
}
