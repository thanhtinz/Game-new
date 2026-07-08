using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §9 — class influence and relationship faith spread.
public class NpcSocialInfluenceServiceTests
{
    private readonly NpcSocialInfluenceService _sut = new();

    [Fact]
    public void RoyaltyInfluence_ShouldExceedCommoner()
    {
        SocialInfluence.GetTierInfluenceWeight(NpcTier.Royalty)
            .Should().BeGreaterThan(SocialInfluence.GetTierInfluenceWeight(NpcTier.Commoner));
    }

    [Fact]
    public void PriestCommoner_GainsReligiousEliteInfluence()
    {
        var npc = new NpcDocument { Tier = NpcTier.Commoner };
        npc.DivineProfile.ChurchRank = ChurchRank.Priest;

        SocialInfluence.GetInfluenceWeight(npc)
            .Should().BeGreaterThanOrEqualTo(20f, "a priest spreads doctrine regardless of birth tier");
    }

    [Fact]
    public void FaithPressure_ScalesWithClassFaithAndTie()
    {
        var royal = new NpcDocument { Tier = NpcTier.Royalty, Faith = 90f };
        var commoner = new NpcDocument { Tier = NpcTier.Commoner, Faith = 20f };
        var strongTie = new NpcRelationship { InfluenceWeight = 1f, Trust = 90f };

        _sut.ComputeFaithPressure(royal, strongTie)
            .Should().BeGreaterThan(_sut.ComputeFaithPressure(commoner, strongTie));
    }

    [Fact]
    public void ApplyPressure_SameGod_DeepensFaith_DifferentGod_GrowsDoubt()
    {
        var sameGod = new NpcDocument { GodInfluenceId = "g1", Faith = 40f, Doubt = 10f };
        _sut.ApplyFaithPressure(sameGod, pressure: 50f, sourceGodId: "g1");
        sameGod.Faith.Should().BeGreaterThan(40f);

        var rivalGod = new NpcDocument { GodInfluenceId = "g1", Faith = 40f, Doubt = 10f };
        _sut.ApplyFaithPressure(rivalGod, pressure: 50f, sourceGodId: "g2");
        rivalGod.Doubt.Should().BeGreaterThan(10f);
    }
}
