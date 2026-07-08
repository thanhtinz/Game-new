using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

// ─── Social Influence (NPC Master Spec §9) ────────────────────
// NPCs do not convert in isolation. Royalty, nobles, religious elite,
// adventurers and servants spread faith with very different strengths and
// paths. Faith flows along the relationship graph, weighted by social class.

public static class SocialInfluence
{
    /// <summary>Base influence strength by social tier (Spec §9 table).</summary>
    public static float GetTierInfluenceWeight(NpcTier tier) => tier switch
    {
        NpcTier.Royalty    => 25f,
        NpcTier.Noble      => 10f,
        NpcTier.Adventurer => 5f,
        NpcTier.Servant    => 3f,
        _                  => 1f,   // Commoner — numbers matter more than the individual
    };

    /// <summary>
    /// Effective influence for an NPC. Religious elite (Priest and above)
    /// spread doctrine strongly regardless of birth tier.
    /// </summary>
    public static float GetInfluenceWeight(NpcDocument npc)
    {
        float weight = GetTierInfluenceWeight(npc.Tier);
        if (npc.DivineProfile.ChurchRank >= ChurchRank.Priest)
            weight = Math.Max(weight, 20f);
        return weight;
    }

    /// <summary>
    /// Faith pressure a source NPC exerts on a target through one relationship:
    /// class weight × tie strength (influence × trust) × the source's own conviction.
    /// </summary>
    public static float CalculateFaithPressure(NpcDocument source, NpcRelationship rel)
    {
        float classWeight = GetInfluenceWeight(source);
        float tieStrength = rel.InfluenceWeight * (rel.Trust / 100f);
        float conviction = source.Faith / 100f;
        return classWeight * tieStrength * conviction;
    }
}

public interface INpcSocialInfluenceService
{
    float ComputeFaithPressure(NpcDocument source, NpcRelationship rel);
    void ApplyFaithPressure(NpcDocument target, float pressure, string? sourceGodId);
}

public class NpcSocialInfluenceService : INpcSocialInfluenceService
{
    public float ComputeFaithPressure(NpcDocument source, NpcRelationship rel)
        => SocialInfluence.CalculateFaithPressure(source, rel);

    /// <summary>
    /// Apply accumulated pressure to a target's belief state. If the source
    /// follows a different god, pressure grows the target's doubt and erodes
    /// trust; if the same god, it deepens faith and trust.
    /// </summary>
    public void ApplyFaithPressure(NpcDocument target, float pressure, string? sourceGodId)
    {
        float delta = Math.Clamp(pressure * 0.1f, 0f, 15f);

        if (sourceGodId != null && target.GodInfluenceId == sourceGodId)
        {
            target.Faith = Math.Clamp(target.Faith + delta, 0f, 100f);
            target.Trust = Math.Clamp(target.Trust + delta * 0.5f, 0f, 100f);
        }
        else
        {
            target.Doubt = Math.Clamp(target.Doubt + delta, 0f, 100f);
            target.Trust = Math.Max(0f, target.Trust - delta * 0.3f);
        }
    }
}
