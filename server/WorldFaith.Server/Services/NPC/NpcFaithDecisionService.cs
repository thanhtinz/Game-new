using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

// ─── NPC Faith Decision (NPC Master Spec §5) ──────────────────
// Belief is probability-driven, not hard-coded. Gods push probabilities
// through race affinity, traits, social pressure, memories, trust and fear;
// the NPC interprets those signals and decides. Every decision carries a
// reason string for UI/debug (Spec §14).

/// <summary>
/// Inputs for a single faith decision. Kept as plain scalars so the math is
/// pure and testable, decoupled from the NpcDocument shape.
/// </summary>
public class NpcFaithContext
{
    /// <summary>Optional reference to the NPC the decision concerns (not required by the math).</summary>
    public NpcDocument? Npc { get; set; }

    // ── Conversion factors (multiplicative, ~1.0 = neutral) ──
    public float BaseOpenness { get; set; } = 0.5f;
    public float RaceAffinity { get; set; } = 1f;
    public float TraitModifier { get; set; } = 1f;
    public float SocialPressure { get; set; } = 1f;
    public float RecentEventImpact { get; set; } = 1f;
    public float TrustDifference { get; set; } = 1f;
    public float FearPressure { get; set; } = 1f;

    // ── Belief state used by doubt / obedience checks (0-100) ──
    public float Faith { get; set; }
    public float Trust { get; set; }
    public float Doubt { get; set; }
    public float Loyalty { get; set; }
    public float Openness { get; set; }
    public List<NpcTrait> Traits { get; set; } = new();
}

public class FaithDecisionResult
{
    public bool Success { get; set; }
    public float Chance { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public interface INpcFaithDecisionService
{
    FaithDecisionResult EvaluateConversion(NpcFaithContext context);
    FaithDecisionResult EvaluateDoubt(NpcFaithContext context);
    FaithDecisionResult EvaluateObedience(NpcFaithContext context);

    // Deterministic chance calculators — exposed for balancing and unit tests
    // (Spec §13 suggests testing the chance directly rather than the roll).
    float CalculateConversionChance(NpcFaithContext context);
    float CalculateDoubtChance(NpcFaithContext context);
    float CalculateObedienceChance(NpcFaithContext context);
}

public class NpcFaithDecisionService : INpcFaithDecisionService
{
    private readonly IRandomService _random;

    public NpcFaithDecisionService(IRandomService random) => _random = random;

    // ── Conversion ──────────────────────────────────────────
    public float CalculateConversionChance(NpcFaithContext c)
    {
        float chance = c.BaseOpenness
            * c.RaceAffinity
            * c.TraitModifier
            * c.SocialPressure
            * c.RecentEventImpact
            * c.TrustDifference
            * c.FearPressure;

        return Math.Clamp(chance, 0.01f, 0.95f);
    }

    public FaithDecisionResult EvaluateConversion(NpcFaithContext c)
    {
        float chance = CalculateConversionChance(c);
        bool success = _random.NextFloat() <= chance;
        return new FaithDecisionResult
        {
            Success = success,
            Chance = chance,
            Reason = success ? "Converted by combined pressure" : "Resisted conversion"
        };
    }

    // ── Doubt ───────────────────────────────────────────────
    public float CalculateDoubtChance(NpcFaithContext c)
    {
        float doubtChance = (c.Doubt / 100f)
            * (1f - c.Trust / 150f)
            * (c.Openness / 100f);

        if (c.Traits.Contains(NpcTrait.Fanatic)) doubtChance *= 0.45f;
        if (c.Traits.Contains(NpcTrait.Curious)) doubtChance *= 1.25f;
        if (c.Traits.Contains(NpcTrait.Traumatized)) doubtChance *= 1.15f;

        return Math.Clamp(doubtChance, 0f, 0.8f);
    }

    public FaithDecisionResult EvaluateDoubt(NpcFaithContext c)
    {
        float chance = CalculateDoubtChance(c);
        return new FaithDecisionResult
        {
            Success = _random.NextFloat() <= chance,
            Chance = chance,
            Reason = "Doubt check"
        };
    }

    // ── Obedience ───────────────────────────────────────────
    public float CalculateObedienceChance(NpcFaithContext c)
    {
        float obedience = (c.Faith + c.Trust + c.Loyalty) / 300f;
        obedience += c.Traits.Contains(NpcTrait.Fanatic) ? 0.20f : 0f;
        obedience -= c.Traits.Contains(NpcTrait.Reckless) ? 0.12f : 0f;
        return Math.Clamp(obedience, 0.05f, 0.98f);
    }

    public FaithDecisionResult EvaluateObedience(NpcFaithContext c)
    {
        float chance = CalculateObedienceChance(c);
        return new FaithDecisionResult
        {
            Success = _random.NextFloat() <= chance,
            Chance = chance,
            Reason = "Commandment obedience check"
        };
    }
}

/// <summary>
/// Trait-based affinity layer (NPC Master Spec §6). Traits nudge a race's base
/// affinity toward or away from a god archetype without ever hard-locking it —
/// a demon can still follow Light, just rarely. Uses the existing GodArchetype
/// rather than introducing a parallel GodDomain enum.
/// </summary>
public static class TraitAffinity
{
    public static float GetTraitModifier(IEnumerable<NpcTrait> traits, GodArchetype archetype)
    {
        var set = traits as ICollection<NpcTrait> ?? traits.ToList();
        float mod = 1f;

        if (set.Contains(NpcTrait.Genius) && archetype == GodArchetype.Knowledge) mod += 0.35f;
        if (set.Contains(NpcTrait.Compassionate) && archetype == GodArchetype.Light) mod += 0.25f;
        if (set.Contains(NpcTrait.Ambitious) && archetype == GodArchetype.Order) mod += 0.20f;
        if (set.Contains(NpcTrait.Reckless) && archetype == GodArchetype.Chaos) mod += 0.25f;
        if (set.Contains(NpcTrait.Cruel) && archetype == GodArchetype.Darkness) mod += 0.25f;
        if (set.Contains(NpcTrait.Merciful) && archetype == GodArchetype.Light) mod += 0.20f;
        if (set.Contains(NpcTrait.Honorable) && archetype == GodArchetype.Order) mod += 0.15f;
        if (set.Contains(NpcTrait.Fanatic)) mod += 0.15f; // fanatics amplify whatever they follow

        return Math.Clamp(mod, 0.10f, 1.80f);
    }
}
