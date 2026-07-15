using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Gift Availability & Divine Attention (Gameplay Spec §9.3, §13.2) ──
// Faith relationship UNLOCKS permission to offer a gift (it never forces the NPC
// to accept). Divine Attention capacity stops gift snowballing: each active
// high-level gift occupies attention/upkeep.

public enum GiftPowerTier { None, Minor, Small, Specialized, High, Signature }

public interface IGiftPermissionService
{
    /// <summary>Derive the faith relationship tier from an NPC's faith and trust.</summary>
    FaithRelationshipTier TierFromFaith(float faith, float trust);

    /// <summary>Highest gift power a relationship tier permits (§9.3 table).</summary>
    GiftPowerTier MaxGiftTier(FaithRelationshipTier tier);

    bool CanOffer(FaithRelationshipTier tier, GiftPowerTier requested);
}

public class GiftPermissionService : IGiftPermissionService
{
    public FaithRelationshipTier TierFromFaith(float faith, float trust)
    {
        // High tiers also require trust in the god's character, not just belief.
        if (faith >= 90f && trust >= 70f) return FaithRelationshipTier.Champion;
        if (faith >= 75f && trust >= 55f) return FaithRelationshipTier.Consecrated;
        if (faith >= 55f && trust >= 35f) return FaithRelationshipTier.Devoted;
        if (faith >= 30f)                 return FaithRelationshipTier.Believer;
        if (faith >= 10f)                 return FaithRelationshipTier.Curious;
        return FaithRelationshipTier.Unknown;
    }

    public GiftPowerTier MaxGiftTier(FaithRelationshipTier tier) => tier switch
    {
        FaithRelationshipTier.Champion    => GiftPowerTier.Signature,
        FaithRelationshipTier.Consecrated => GiftPowerTier.High,
        FaithRelationshipTier.Devoted     => GiftPowerTier.Specialized,
        FaithRelationshipTier.Believer    => GiftPowerTier.Small,
        FaithRelationshipTier.Curious     => GiftPowerTier.Minor,
        _                                 => GiftPowerTier.None,
    };

    public bool CanOffer(FaithRelationshipTier tier, GiftPowerTier requested)
        => (int)requested <= (int)MaxGiftTier(tier);
}

// ── Divine Attention capacity (§13.2) ─────────────────────────
public interface IDivineAttentionService
{
    /// <summary>Attention/upkeep cost of an active gift of a given tier.</summary>
    float AttentionCost(GiftPowerTier tier);

    /// <summary>Total attention currently occupied by a god's active gifts.</summary>
    float UsedAttention(IEnumerable<GiftPowerTier> activeGifts);

    /// <summary>Whether granting one more gift of <paramref name="newGift"/> fits within capacity.</summary>
    bool CanGrant(float capacity, IEnumerable<GiftPowerTier> activeGifts, GiftPowerTier newGift);
}

public class DivineAttentionService : IDivineAttentionService
{
    public float AttentionCost(GiftPowerTier tier) => tier switch
    {
        GiftPowerTier.Minor       => 0f,   // trivial signs cost no ongoing attention
        GiftPowerTier.Small       => 1f,
        GiftPowerTier.Specialized => 4f,
        GiftPowerTier.High        => 10f,
        GiftPowerTier.Signature   => 20f,
        _                         => 0f,
    };

    public float UsedAttention(IEnumerable<GiftPowerTier> activeGifts)
        => activeGifts.Sum(AttentionCost);

    public bool CanGrant(float capacity, IEnumerable<GiftPowerTier> activeGifts, GiftPowerTier newGift)
        => UsedAttention(activeGifts) + AttentionCost(newGift) <= capacity;
}
