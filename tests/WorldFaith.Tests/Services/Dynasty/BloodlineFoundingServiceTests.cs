using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §8 / Roadmap Phase 3 — god action founds a blessing or curse lineage.
public class BloodlineFoundingServiceTests
{
    private readonly BloodlineFoundingService _sut = new();

    [Fact]
    public void Blessing_AttachesActiveGenerationZeroInstanceToFounder()
    {
        var saint = new NpcDocument { Name = "Elowen", WorldId = "w1" };

        var line = _sut.FoundLineage(saint, "sun_god", GodDomain.Light, "House Solaris",
            foundingStrength: 100f, currentYear: 400, isDivineLineage: true);

        line.Kind.Should().Be(BloodlineKind.DivineLineage);
        line.FounderNpcId.Should().Be(saint.Id);
        saint.InheritedBlessings.Should().ContainSingle();
        var inst = saint.InheritedBlessings[0];
        inst.State.Should().Be(BlessingState.Active);
        inst.GenerationDistanceFromFounder.Should().Be(0);
        saint.PrimaryBloodlineId.Should().Be(line.Id);
    }

    [Fact]
    public void Curse_CreatesCursedLineage()
    {
        var heir = new NpcDocument { Name = "Vael", WorldId = "w1" };

        var line = _sut.FoundLineage(heir, "void_god", GodDomain.Darkness, "Void-Touched",
            foundingStrength: 60f, currentYear: 400, isCurse: true);

        line.Kind.Should().Be(BloodlineKind.CursedLineage);
        heir.InheritedBlessings.Should().ContainSingle();
    }

    [Fact]
    public void DivineLineage_DecaysSlowerThanPlainBlessing()
    {
        var a = new NpcDocument { WorldId = "w1" };
        var b = new NpcDocument { WorldId = "w1" };

        var divine = _sut.FoundLineage(a, "g", GodDomain.Light, "Divine", 80f, 400, isDivineLineage: true);
        var plain  = _sut.FoundLineage(b, "g", GodDomain.Light, "Plain", 80f, 400);

        divine.Blessings[0].GenerationalDecayRate
            .Should().BeLessThan(plain.Blessings[0].GenerationalDecayRate);
    }
}
