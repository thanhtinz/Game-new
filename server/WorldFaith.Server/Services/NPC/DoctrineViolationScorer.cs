using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

/// <summary>
/// Derives a <see cref="ViolationSeverity"/> from how strongly an act conflicts
/// with a doctrine (NPC Master Spec §7). Severity grows with forbidden-tag
/// hits, distance on the doctrine axes, and whether the act was public and
/// intentional. This replaces "caller guesses the severity" with a scored
/// value that balance data can drive.
/// </summary>
public static class DoctrineViolationScorer
{
    public static ViolationSeverity CalculateSeverity(DoctrineValues doctrine, DoctrineEvent evt)
    {
        float conflict = 0f;

        foreach (var tag in evt.Tags)
        {
            if (doctrine.ForbiddenTags.Contains(tag)) conflict += 30f;
            if (doctrine.SacredTags.Contains(tag)) conflict -= 10f;
        }

        conflict += MathF.Abs(doctrine.MercyVsPunishment - evt.MercyImpact) / 8f;
        conflict += MathF.Abs(doctrine.FreedomVsOrder - evt.OrderImpact) / 8f;
        conflict += MathF.Abs(doctrine.HarmonyVsDominion - evt.DominionImpact) / 8f;

        if (evt.WasPublic) conflict *= 1.25f;
        if (evt.WasIntentional) conflict *= 1.30f;

        return conflict switch
        {
            < 20f => ViolationSeverity.MinorContradiction,
            < 45f => ViolationSeverity.ModerateViolation,
            < 70f => ViolationSeverity.MajorViolation,
            < 90f => ViolationSeverity.SevereBetrayal,
            _     => ViolationSeverity.DoctrineInversion
        };
    }
}
