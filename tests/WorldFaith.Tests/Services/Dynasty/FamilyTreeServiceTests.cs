using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §6 — family tree / child creation.
public class FamilyTreeServiceTests
{
    private static FamilyTreeService Make(int seed)
    {
        var rng = new RandomService(seed);
        var inheritance = new BloodlineInheritanceService(rng, new FixedAffinity(1f));
        return new FamilyTreeService(inheritance, new GeneMixingService(rng));
    }

    private static (NpcDocument father, NpcDocument mother, Dictionary<string, BloodlineDocument> lines) Couple()
    {
        var def = new BloodlineBlessingDefinition
        {
            BlessingId = "solar", Name = "Solar Blood", GodId = "sun", Domain = GodDomain.Light,
            FoundingStrength = 100f, GenerationalDecayRate = 0.15f, IsDivineLineage = true
        };
        var line = new BloodlineDocument { Id = "house_solaris", Purity = 95f, Blessings = { def } };

        var father = new NpcDocument
        {
            Name = "Aldric", FamilyName = "Solaris", Sex = SexType.Male, Race = RaceType.Human,
            FamilyId = "fam1", WorldId = "w1",
            InheritedBlessings = { new() { BlessingId = "solar", SourceBloodlineId = "house_solaris",
                SourceGodId = "sun", Strength = 90f, Potential = 90f, State = BlessingState.Active } }
        };
        var mother = new NpcDocument { Name = "Mira", Sex = SexType.Female, Race = RaceType.Human, WorldId = "w1" };

        return (father, mother, new Dictionary<string, BloodlineDocument> { ["house_solaris"] = line });
    }

    [Fact]
    public void CreateChild_SetsParentLinks_Name_AndBirthYear()
    {
        var (father, mother, lines) = Couple();
        var child = Make(7).CreateChild(father, mother, currentYear: 812, givenName: "Cael", lines);

        child.Name.Should().Be("Cael");
        child.FamilyName.Should().Be("Solaris", "child takes the named house");
        child.BirthYear.Should().Be(812);
        child.FatherNpcId.Should().Be(father.Id);
        child.MotherNpcId.Should().Be(mother.Id);
        father.ChildrenIds.Should().Contain(child.Id);
        mother.ChildrenIds.Should().Contain(child.Id);
    }

    [Fact]
    public void CreateChild_InheritsBloodline_AndPicksPrimary()
    {
        var (father, mother, lines) = Couple();
        var child = Make(3).CreateChild(father, mother, 812, "Cael", lines);

        child.BloodlineIds.Should().Contain("house_solaris");
        child.PrimaryBloodlineId.Should().Be("house_solaris");
    }

    [Fact]
    public void CreateChild_MixesGenesWithinBounds()
    {
        var (father, mother, lines) = Couple();
        father.Genes.Strength = 80f; mother.Genes.Strength = 40f;

        var child = Make(9).CreateChild(father, mother, 812, "Cael", lines);

        child.Genes.Strength.Should().BeInRange(0f, 100f);
        child.Genes.Strength.Should().BeInRange(50f, 70f, "roughly the parents' average ± variance");
    }

    private sealed class FixedAffinity : IBloodlineAffinityService
    {
        private readonly float _v;
        public FixedAffinity(float v) => _v = v;
        public float GetDomainAffinity(RaceType race, GodDomain domain) => _v;
    }
}
