using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §9.3 — gift availability by faith relationship.
public class GiftPermissionServiceTests
{
    private readonly GiftPermissionService _sut = new();

    [Fact]
    public void HighFaithHighTrust_IsChampion_AndCanReceiveSignatureGift()
    {
        var tier = _sut.TierFromFaith(95f, 80f);
        tier.Should().Be(FaithRelationshipTier.Champion);
        _sut.CanOffer(tier, GiftPowerTier.Signature).Should().BeTrue();
    }

    [Fact]
    public void Believer_CannotReceiveHighGift()
    {
        var tier = _sut.TierFromFaith(40f, 20f);
        tier.Should().Be(FaithRelationshipTier.Believer);
        _sut.CanOffer(tier, GiftPowerTier.High).Should().BeFalse();
        _sut.CanOffer(tier, GiftPowerTier.Small).Should().BeTrue();
    }

    [Fact]
    public void Unknown_GetsNoPermanentGift()
    {
        var tier = _sut.TierFromFaith(2f, 0f);
        tier.Should().Be(FaithRelationshipTier.Unknown);
        _sut.MaxGiftTier(tier).Should().Be(GiftPowerTier.None);
    }
}

// Gameplay Spec §13.2 — divine attention capacity (anti-snowball).
public class DivineAttentionServiceTests
{
    private readonly DivineAttentionService _sut = new();

    [Fact]
    public void MinorGifts_CostNoOngoingAttention()
        => _sut.AttentionCost(GiftPowerTier.Minor).Should().Be(0f);

    [Fact]
    public void CapacityBlocksTooManyHighGifts()
    {
        var active = new[] { GiftPowerTier.Signature, GiftPowerTier.High }; // 20 + 10 = 30
        _sut.CanGrant(capacity: 35f, active, GiftPowerTier.High).Should().BeFalse("would reach 40 > 35");
        _sut.CanGrant(capacity: 45f, active, GiftPowerTier.High).Should().BeTrue();
    }

    [Fact]
    public void UsedAttention_SumsActiveGifts()
        => _sut.UsedAttention(new[] { GiftPowerTier.Specialized, GiftPowerTier.Small })
               .Should().Be(5f);
}
