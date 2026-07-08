using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §8 — memory modifier & decay.
public class NpcMemoryModifierTests
{
    [Fact]
    public void PositiveMemory_RaisesModifier_NegativeLowersIt()
    {
        var good = new List<NpcMemory> { new() { GodId = "g1", TrustChange = 60f, Strength = 1f } };
        var bad  = new List<NpcMemory> { new() { GodId = "g1", DoubtChange = 90f, Strength = 1f } };

        NpcMemoryModifier.CalculateMemoryModifier(good, "g1").Should().BeGreaterThan(1f);
        NpcMemoryModifier.CalculateMemoryModifier(bad, "g1").Should().BeLessThan(1f);
    }

    [Fact]
    public void Modifier_IgnoresOtherGodsMemories()
    {
        var memories = new List<NpcMemory> { new() { GodId = "other", TrustChange = 90f, Strength = 1f } };
        NpcMemoryModifier.CalculateMemoryModifier(memories, "g1").Should().Be(1f);
    }

    [Fact]
    public void PersonalMemory_DecaysWithAge_CulturalMemoryPersists()
    {
        var personal = new NpcMemory { CreatedTick = 0, DecayPerAge = 0.10f, Strength = 1f };
        var cultural = new NpcMemory { CreatedTick = 0, DecayPerAge = 0.10f, Strength = 1f, IsCulturalMemory = true };
        var list = new List<NpcMemory> { personal, cultural };

        NpcMemoryModifier.Decay(list, currentTick: 500); // 5 age-units → 1 - 0.5 = 0.5

        personal.Strength.Should().BeApproximately(0.5f, 0.001f);
        cultural.Strength.Should().Be(1f, "cultural memory does not fade");
    }
}

// NPC Master Spec §15 — player-facing risk indicators.
public class NpcIndicatorServiceTests
{
    private readonly NpcIndicatorService _sut = new();

    [Fact]
    public void BrokenIntegrity_FlagsAtRiskOfFall()
    {
        var npc = new NpcDocument();
        npc.DivineProfile.DoctrineIntegrity.Score = 20f;

        _sut.Evaluate(npc).AtRiskOfFall.Should().BeTrue();
    }

    [Fact]
    public void ForbiddenSecret_FlagsHiddenCultist()
    {
        var npc = new NpcDocument();
        npc.Secrets.Add(new NpcSecret { Type = NpcSecretType.ForbiddenGodWorship });

        _sut.Evaluate(npc).HiddenCultist.Should().BeTrue();
    }

    [Fact]
    public void LowTrustHighDoubt_FlagsFaithShaken()
    {
        var npc = new NpcDocument { Trust = 30f, Doubt = 70f };
        _sut.Evaluate(npc).FaithShaken.Should().BeTrue();
    }

    [Fact]
    public void StrongDisasterMemory_FlagsMemoryScar_WithReason()
    {
        var npc = new NpcDocument();
        npc.Memories.Add(new NpcMemory { Type = NpcMemoryType.SacredSiteDestroyed, Strength = 0.9f });

        var ind = _sut.Evaluate(npc);
        ind.MemoryScar.Should().BeTrue();
        ind.Reasons.Should().NotBeEmpty("indicators carry reasons for the UI");
    }

    [Fact]
    public void HealthyNpc_HasNoWarnings()
    {
        var npc = new NpcDocument { Trust = 70f, Doubt = 10f };
        var ind = _sut.Evaluate(npc);
        (ind.AtRiskOfFall || ind.HiddenCultist || ind.FaithShaken || ind.MemoryScar).Should().BeFalse();
    }
}
