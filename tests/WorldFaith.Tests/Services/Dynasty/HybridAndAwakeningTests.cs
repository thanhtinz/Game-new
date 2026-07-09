using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §7 — hybrid bloodlines.
public class HybridBloodlineServiceTests
{
    private static readonly IReadOnlyList<HybridBloodlineRule> Rules = new List<HybridBloodlineRule>
    {
        new() { DomainA = GodDomain.Light, DomainB = GodDomain.Nature,
                ResultName = "World Tree Lineage", MinimumCombinedStrength = 80f, MutationChance = 1f },
    };

    private static (List<InheritedBlessingInstance> blessings, Dictionary<string, GodDomain> domains)
        Pair(float strengthA, float strengthB)
    {
        var blessings = new List<InheritedBlessingInstance>
        {
            new() { BlessingId = "a", Strength = strengthA },
            new() { BlessingId = "b", Strength = strengthB },
        };
        var domains = new Dictionary<string, GodDomain> { ["a"] = GodDomain.Light, ["b"] = GodDomain.Nature };
        return (blessings, domains);
    }

    [Fact]
    public void CompatibleStrongPair_ProducesHybrid()
    {
        var sut = new HybridBloodlineService(Rules, new RandomService(1));
        var (blessings, domains) = Pair(50f, 50f); // combined 100 >= 80

        sut.TryFindHybrid(blessings, domains)!.ResultName.Should().Be("World Tree Lineage");
    }

    [Fact]
    public void WeakPair_ProducesNoHybrid()
    {
        var sut = new HybridBloodlineService(Rules, new RandomService(1));
        var (blessings, domains) = Pair(20f, 20f); // combined 40 < 80

        sut.TryFindHybrid(blessings, domains).Should().BeNull();
    }

    [Fact]
    public void IncompatibleDomains_ProduceNoHybrid()
    {
        var sut = new HybridBloodlineService(Rules, new RandomService(1));
        var blessings = new List<InheritedBlessingInstance>
        {
            new() { BlessingId = "a", Strength = 60f }, new() { BlessingId = "b", Strength = 60f }
        };
        var domains = new Dictionary<string, GodDomain> { ["a"] = GodDomain.War, ["b"] = GodDomain.Death };

        sut.TryFindHybrid(blessings, domains).Should().BeNull();
    }
}

// Dynasty Spec §8 — dormant bloodline awakening.
public class BloodlineAwakeningServiceTests
{
    private static NpcDocument WithDormant(float potential = 90f) => new()
    {
        InheritedBlessings =
        {
            new() { BlessingId = "ancient", State = BlessingState.Dormant, Strength = 5f, Potential = potential }
        }
    };

    [Fact]
    public void DormantBlessing_CanAwaken_UnderDramaticEvent()
    {
        // High potential + direct miracle + sacred site pushes chance high; find a seed that awakens.
        var ctx = new WorldEventContext { ReceivedDirectMiracle = true, AtSacredSite = true, HighFaithMoment = true };
        bool awakenedAtLeastOnce = false;

        for (int seed = 0; seed < 25 && !awakenedAtLeastOnce; seed++)
        {
            var npc = WithDormant();
            if (new BloodlineAwakeningService(new RandomService(seed)).TryAwaken(npc, ctx))
            {
                npc.InheritedBlessings[0].State.Should().Be(BlessingState.Active);
                npc.InheritedBlessings[0].Strength.Should().BeGreaterThan(5f, "awakening boosts strength");
                awakenedAtLeastOnce = true;
            }
        }

        awakenedAtLeastOnce.Should().BeTrue("a high-potential dormant blessing awakens under dramatic events");
    }

    [Fact]
    public void ActiveBlessing_IsNotAffected()
    {
        var npc = new NpcDocument
        {
            InheritedBlessings = { new() { State = BlessingState.Active, Strength = 50f, Potential = 90f } }
        };

        new BloodlineAwakeningService(new RandomService(1))
            .TryAwaken(npc, new WorldEventContext { ReceivedDirectMiracle = true })
            .Should().BeFalse("only dormant blessings awaken");
    }
}
