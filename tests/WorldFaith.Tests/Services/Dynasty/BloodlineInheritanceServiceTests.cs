using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §16 — bloodline inheritance must be tested or balancing breaks.
public class BloodlineInheritanceServiceTests
{
    private static BloodlineInheritanceService Make(int seed, float affinity = 1f)
        => new(new RandomService(seed), new FixedAffinity(affinity));

    private static (BloodlineBlessingDefinition def, InheritedBlessingInstance parent, BloodlineDocument line)
        Phoenix(float founding = 100f, float decay = 0.2f, bool divine = false, float purity = 90f)
    {
        var def = new BloodlineBlessingDefinition
        {
            BlessingId = "phoenix", Name = "Phoenix Blood", GodId = "fire_god",
            Domain = GodDomain.Fire, FoundingStrength = founding,
            GenerationalDecayRate = decay, IsDivineLineage = divine
        };
        var line = new BloodlineDocument { Id = "line_phoenix", Purity = purity, Blessings = { def } };
        var parent = new InheritedBlessingInstance
        {
            BlessingId = "phoenix", SourceBloodlineId = "line_phoenix", SourceGodId = "fire_god",
            Strength = founding * 0.9f, Potential = 90f, State = BlessingState.Active
        };
        return (def, parent, line);
    }

    [Fact]
    public void StrongBlessing_UsuallyCreatesActiveOrDormantChildInstance()
    {
        var svc = Make(42);
        var (def, parent, line) = Phoenix();
        var child = new NpcDocument { Race = RaceType.Human };

        var result = svc.RollInheritance(def, parent, child, line, divineFavorModifier: 1f);

        result.Should().NotBeNull();
        result!.State.Should().BeOneOf(BlessingState.Active, BlessingState.Dormant);
        result.GenerationDistanceFromFounder.Should().Be(1);
    }

    [Fact]
    public void DivineLineage_DecaysSlowerThanNormalLineage()
    {
        // No randomness: fix variance to 1.0 by using a deterministic RNG and comparing
        // averages across many rolls, so the divine (slower-decay) line stays stronger.
        float normalTotal = 0f, divineTotal = 0f;
        for (int seed = 0; seed < 40; seed++)
        {
            var child = new NpcDocument { Race = RaceType.Human };
            var (nDef, nParent, nLine) = Phoenix(decay: 0.4f, divine: false);
            var (dDef, dParent, dLine) = Phoenix(decay: 0.4f, divine: true);
            normalTotal += Make(seed).RollInheritance(nDef, nParent, child, nLine, 1f)?.Strength ?? 0f;
            divineTotal += Make(seed).RollInheritance(dDef, dParent, child, dLine, 1f)?.Strength ?? 0f;
        }

        divineTotal.Should().BeGreaterThan(normalTotal, "divine lineages resist generational decay");
    }

    [Fact]
    public void WeakBlessing_ThatCannotBecomeDormant_IsLost()
    {
        var svc = Make(1);
        var (def, parent, line) = Phoenix();
        def.CanBecomeDormant = false;
        parent.Strength = 0.5f; // below the survival floor

        svc.RollInheritance(def, parent, new NpcDocument { Race = RaceType.Human }, line, 1f)
            .Should().BeNull();
    }

    [Fact]
    public void Siblings_CanInheritDifferentStrengths()
    {
        var (def, parent, line) = Phoenix();
        var a = Make(1).RollInheritance(def, parent, new NpcDocument { Race = RaceType.Human }, line, 1f);
        var b = Make(2).RollInheritance(def, parent, new NpcDocument { Race = RaceType.Human }, line, 1f);

        a.Should().NotBeNull(); b.Should().NotBeNull();
        a!.Strength.Should().NotBe(b!.Strength, "random variance makes siblings differ");
    }

    private sealed class FixedAffinity : IBloodlineAffinityService
    {
        private readonly float _v;
        public FixedAffinity(float v) => _v = v;
        public float GetDomainAffinity(RaceType race, GodDomain domain) => _v;
    }
}
