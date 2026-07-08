using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §3-4 — persistent NPC data model + bridge to belief math.
public class NpcPersistentModelTests
{
    [Fact]
    public void NpcDocument_HasBeliefAndPersonalityDefaults()
    {
        var npc = new NpcDocument();

        npc.Faith.Should().BeInRange(0f, 100f);
        npc.Trust.Should().BeInRange(0f, 100f);
        npc.Openness.Should().BeInRange(0f, 100f);
        npc.Traits.Should().BeEmpty();
        npc.Secrets.Should().BeEmpty();
        npc.Memories.Should().BeEmpty();
    }

    [Fact]
    public void ContextFactory_AppliesTraitModifierFromNpc()
    {
        var plain  = new NpcDocument { Openness = 60f };
        var genius = new NpcDocument { Openness = 60f, Traits = { NpcTrait.Genius } };

        var plainCtx  = NpcFaithContextFactory.ForConversion(plain,  GodArchetype.Knowledge, raceAffinity: 1f);
        var geniusCtx = NpcFaithContextFactory.ForConversion(genius, GodArchetype.Knowledge, raceAffinity: 1f);

        geniusCtx.TraitModifier.Should().BeGreaterThan(plainCtx.TraitModifier,
            "a genius NPC is more drawn to a Knowledge god");
    }

    [Fact]
    public void ContextFactory_ProducesUsableConversionChance()
    {
        var npc = new NpcDocument { Openness = 80f, Traits = { NpcTrait.Compassionate } };
        var ctx = NpcFaithContextFactory.ForConversion(npc, GodArchetype.Light, raceAffinity: 1.3f, socialPressure: 1.2f);

        var svc = new NpcFaithDecisionService(new RandomService(seed: 7));
        svc.CalculateConversionChance(ctx).Should().BeInRange(0.01f, 0.95f);
    }
}
