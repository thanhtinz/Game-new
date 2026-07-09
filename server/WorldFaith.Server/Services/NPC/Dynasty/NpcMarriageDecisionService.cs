using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Marriage Decision (Dynasty Spec §10) ─────────────────────
// NPCs do not marry randomly. A match is scored by politics, faith, family
// approval, bloodline value and status — modulated by personality.

public class MarriageContext
{
    public float PoliticalAllianceValue { get; set; }
    public bool SharedFaith { get; set; }
    public float FamilyApproval { get; set; }
    public float BloodlineCompatibilityScore { get; set; }
    public float StatusGain { get; set; }
    public float RivalFamilyPenalty { get; set; }
    public float ForbiddenDoctrinePenalty { get; set; }
    public float CloseKinshipPenalty { get; set; }
}

public interface INpcMarriageDecisionService
{
    float ScoreMarriage(NpcDocument seeker, NpcDocument candidate, MarriageContext ctx);
}

public class NpcMarriageDecisionService : INpcMarriageDecisionService
{
    public float ScoreMarriage(NpcDocument seeker, NpcDocument candidate, MarriageContext ctx)
    {
        float score = 0f;
        score += ctx.PoliticalAllianceValue;
        score += ctx.SharedFaith ? 15f : -5f;
        score += ctx.FamilyApproval;
        score += ctx.BloodlineCompatibilityScore;
        // Ambitious NPCs weight status gain more heavily.
        score += seeker.Traits.Contains(NpcTrait.Ambitious) ? ctx.StatusGain * 1.2f : ctx.StatusGain;
        score -= ctx.RivalFamilyPenalty;
        score -= ctx.ForbiddenDoctrinePenalty;
        score -= ctx.CloseKinshipPenalty;
        return score;
    }
}
