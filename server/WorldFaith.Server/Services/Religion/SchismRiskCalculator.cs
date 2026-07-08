using WorldFaith.Server.Models;

namespace WorldFaith.Server.Services.Religion;

// ─── Schism Risk (NPC Master Spec §7 evolution / §15 indicator) ───
// When a faction inside a religion drifts far from the official doctrine and
// grows dissatisfied, pressure builds toward a split (mercy sect leaving a
// punishment church, a radical cult, etc.). This computes the risk the existing
// ReligionService schism system can act on, and the doctrine a splinter adopts.

public static class SchismRiskCalculator
{
    /// <summary>
    /// Normalized distance between two doctrines across all five axes:
    /// 0 = identical, 1 = polar opposite (each axis spans -100..+100).
    /// </summary>
    public static float DoctrineDistance(DoctrineValues a, DoctrineValues b)
    {
        float sum =
            MathF.Abs(a.MercyVsPunishment - b.MercyVsPunishment) +
            MathF.Abs(a.IsolationVsExpansion - b.IsolationVsExpansion) +
            MathF.Abs(a.HarmonyVsDominion - b.HarmonyVsDominion) +
            MathF.Abs(a.FreedomVsOrder - b.FreedomVsOrder) +
            MathF.Abs(a.SacrificeVsProsperity - b.SacrificeVsProsperity);

        return Math.Clamp(sum / (5f * 200f), 0f, 1f);
    }

    /// <summary>
    /// Schism risk (0-1) rises with doctrine distance, the faction's size and its
    /// dissatisfaction. A tiny content faction never splits; a large angry one that
    /// wants the opposite doctrine almost certainly does.
    /// </summary>
    public static float CalculateSchismRisk(
        DoctrineValues church, DoctrineValues factionDesired,
        float factionSizeRatio, float dissatisfaction)
    {
        float distance = DoctrineDistance(church, factionDesired);
        float risk = distance
            * Math.Clamp(factionSizeRatio, 0f, 1f)
            * Math.Clamp(dissatisfaction, 0f, 1f)
            * 3f; // amplify so a mid-size, unhappy, opposed faction reaches high risk
        return Math.Clamp(risk, 0f, 1f);
    }

    /// <summary>
    /// Doctrine a breakaway sect adopts: the church's doctrine shifted toward the
    /// faction's desired values by <paramref name="shift"/> (0 = stay, 1 = fully adopt).
    /// </summary>
    public static DoctrineValues CreateSplinterDoctrine(
        DoctrineValues church, DoctrineValues factionDesired, float shift = 0.6f)
    {
        shift = Math.Clamp(shift, 0f, 1f);
        return new DoctrineValues
        {
            MercyVsPunishment     = Lerp(church.MercyVsPunishment,     factionDesired.MercyVsPunishment,     shift),
            IsolationVsExpansion  = Lerp(church.IsolationVsExpansion,  factionDesired.IsolationVsExpansion,  shift),
            HarmonyVsDominion     = Lerp(church.HarmonyVsDominion,     factionDesired.HarmonyVsDominion,     shift),
            FreedomVsOrder        = Lerp(church.FreedomVsOrder,        factionDesired.FreedomVsOrder,        shift),
            SacrificeVsProsperity = Lerp(church.SacrificeVsProsperity, factionDesired.SacrificeVsProsperity, shift),
        };
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
