using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

// ─── Player-Facing Indicators (NPC Master Spec §15) ───────────
// Backend for the debug/inspector UI. Turns raw NPC state into the warning
// flags a player reads to decide when to intervene. Each flag carries a reason.

public class NpcIndicators
{
    public bool AtRiskOfFall { get; set; }   // doctrine integrity below safe threshold
    public bool HiddenCultist { get; set; }  // secretly worships a forbidden god
    public bool FaithShaken { get; set; }    // low trust + high doubt
    public bool MemoryScar { get; set; }     // a past disaster still weighs on them
    public List<string> Reasons { get; set; } = new();
}

public interface INpcIndicatorService
{
    NpcIndicators Evaluate(NpcDocument npc);
}

public class NpcIndicatorService : INpcIndicatorService
{
    // Doctrine integrity at/below this is "Broken" — fall risk (Spec §7 table 15-34).
    private const float FallRiskThreshold = 35f;

    private static readonly HashSet<NpcSecretType> ForbiddenSecrets = new()
    {
        NpcSecretType.HiddenFaith, NpcSecretType.ForbiddenGodWorship, NpcSecretType.CultMembership
    };

    private static readonly HashSet<NpcMemoryType> ScarringMemories = new()
    {
        NpcMemoryType.ForestBurned, NpcMemoryType.SacredSiteDestroyed, NpcMemoryType.MiracleFailed,
        NpcMemoryType.WarDefeat, NpcMemoryType.SaintFell, NpcMemoryType.ProphetMartyred
    };

    public NpcIndicators Evaluate(NpcDocument npc)
    {
        var ind = new NpcIndicators();

        if (npc.DivineProfile.DoctrineIntegrity.Score < FallRiskThreshold)
        {
            ind.AtRiskOfFall = true;
            ind.Reasons.Add($"Doctrine integrity {npc.DivineProfile.DoctrineIntegrity.Score:F0} — fall risk");
        }

        if (npc.Secrets.Any(s => !s.IsExposed && ForbiddenSecrets.Contains(s.Type)))
        {
            ind.HiddenCultist = true;
            ind.Reasons.Add("Holds a hidden/forbidden faith secret");
        }

        if (npc.Trust < 40f && npc.Doubt > 50f)
        {
            ind.FaithShaken = true;
            ind.Reasons.Add($"Trust {npc.Trust:F0} low, doubt {npc.Doubt:F0} high");
        }

        if (npc.Memories.Any(m => m.Strength > 0.5f && ScarringMemories.Contains(m.Type)))
        {
            ind.MemoryScar = true;
            ind.Reasons.Add("A past disaster still shapes their belief");
        }

        return ind;
    }
}
