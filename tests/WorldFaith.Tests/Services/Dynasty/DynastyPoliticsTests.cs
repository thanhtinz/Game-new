using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Dynasty;

// Dynasty Spec §10 — marriage scoring.
public class NpcMarriageDecisionServiceTests
{
    private readonly NpcMarriageDecisionService _sut = new();

    [Fact]
    public void AmbitiousSeeker_WeightsStatusGainHigher()
    {
        var plain = new NpcDocument();
        var ambitious = new NpcDocument { Traits = { NpcTrait.Ambitious } };
        var candidate = new NpcDocument();
        var ctx = new MarriageContext { StatusGain = 40f };

        _sut.ScoreMarriage(ambitious, candidate, ctx)
            .Should().BeGreaterThan(_sut.ScoreMarriage(plain, candidate, ctx));
    }

    [Fact]
    public void SharedFaith_ScoresHigherThanDifferentFaith()
    {
        var seeker = new NpcDocument();
        var candidate = new NpcDocument();

        float shared = _sut.ScoreMarriage(seeker, candidate, new MarriageContext { SharedFaith = true });
        float diff   = _sut.ScoreMarriage(seeker, candidate, new MarriageContext { SharedFaith = false });

        shared.Should().BeGreaterThan(diff);
    }
}

// Dynasty Spec §14 — family reputation shifts.
public class DynastyReputationServiceTests
{
    private readonly DynastyReputationService _sut = new();

    [Fact]
    public void BlessedEvent_RaisesDivineFavorAndHonor()
    {
        var family = new FamilyHouseDocument();
        _sut.ApplyEvent(family, new DynastyHistoryEvent { EventType = DynastyEventType.Blessed });

        family.DivineFavor.Should().BeGreaterThan(0f);
        family.Honor.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void CursedEvent_RaisesInfamy_SuccessionDispute_LowersLegitimacy()
    {
        var family = new FamilyHouseDocument();
        _sut.ApplyEvent(family, new DynastyHistoryEvent { EventType = DynastyEventType.Cursed });
        _sut.ApplyEvent(family, new DynastyHistoryEvent { EventType = DynastyEventType.SuccessionDispute });

        family.Infamy.Should().BeGreaterThan(0f);
        family.PoliticalLegitimacy.Should().BeLessThan(0f);
    }

    [Fact]
    public void Reputation_IsClamped()
    {
        var family = new FamilyHouseDocument { DivineFavor = 98f };
        for (int i = 0; i < 5; i++)
            _sut.ApplyEvent(family, new DynastyHistoryEvent { EventType = DynastyEventType.Blessed });

        family.DivineFavor.Should().BeLessThanOrEqualTo(100f);
    }
}

// Dynasty Spec §11 — succession.
public class SuccessionServiceTests
{
    private readonly SuccessionService _sut = new();

    [Fact]
    public void LegitimateStrongLivingMember_IsChosenOverIllegitimateOrDead()
    {
        var family = new FamilyHouseDocument { Id = "house1" };

        var legit = new NpcDocument
        {
            Name = "Legit", FamilyId = "house1", BirthYear = 780,
            FatherNpcId = "f", MotherNpcId = "m",
            InheritedBlessings = { new() { Strength = 70f } }
        };
        var bastard = new NpcDocument { Name = "Bastard", FamilyId = "house1", BirthYear = 782 };
        var dead = new NpcDocument
        {
            Name = "Dead", FamilyId = "house1", BirthYear = 760, DeathYear = 800,
            FatherNpcId = "f", MotherNpcId = "m", InheritedBlessings = { new() { Strength = 99f } }
        };

        var heir = _sut.ChooseHeir(family, new[] { bastard, dead, legit }, currentYear: 810);

        heir.Should().Be(legit);
    }

    [Fact]
    public void NonFamilyMembers_AreNotEligible()
    {
        var family = new FamilyHouseDocument { Id = "house1" };
        var outsider = new NpcDocument { FamilyId = "other", BirthYear = 780 };

        _sut.ChooseHeir(family, new[] { outsider }, 810).Should().BeNull();
    }
}
