using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Rival Aura Traces (Gameplay Spec §10.3–10.5) ─────────────
// Rival gods stay hidden, but their actions leave discoverable traces on NPCs.
// How much the player learns escalates with investigation — and a rival's
// identity/gift is only fully revealed through a valid in-world cause
// (confession, investigation, direct conflict), never for free.

public record AuraTraceInput(
    bool HasRivalInfluence,
    bool NpcConfided,            // NPC willingly told via prayer / connected follower
    bool ExaminedBySpecialist,   // priest, oracle, champion, runestone, ward, sacred site
    int RepeatedPatternCount,    // rival repeated a recognizable pattern across events
    bool PlayerCounteredDirectly,
    bool EnteredSacredArea);     // affected NPC entered the player's sacred area / used a gift

public interface IAuraTraceService
{
    AuraTraceStrength Evaluate(AuraTraceInput input);
}

public class AuraTraceService : IAuraTraceService
{
    public AuraTraceStrength Evaluate(AuraTraceInput i)
    {
        if (!i.HasRivalInfluence) return AuraTraceStrength.None;

        // Revealed requires a valid disclosure cause (§10.5): confession, or direct
        // conflict combined with expert examination of a repeated pattern.
        bool revealed = i.NpcConfided
            || (i.PlayerCounteredDirectly && i.ExaminedBySpecialist && i.RepeatedPatternCount >= 2);
        if (revealed) return AuraTraceStrength.Revealed;

        // Identified: likely rival / known signature.
        bool identified = (i.ExaminedBySpecialist && i.RepeatedPatternCount >= 2)
            || (i.PlayerCounteredDirectly && i.ExaminedBySpecialist)
            || i.RepeatedPatternCount >= 4;
        if (identified) return AuraTraceStrength.Identified;

        // Recognizable: domain family / emotional quality.
        bool recognizable = i.ExaminedBySpecialist || i.RepeatedPatternCount >= 2 || i.EnteredSacredArea;
        if (recognizable) return AuraTraceStrength.Recognizable;

        // Faint: something spiritual recently affected the NPC.
        return AuraTraceStrength.Faint;
    }
}
