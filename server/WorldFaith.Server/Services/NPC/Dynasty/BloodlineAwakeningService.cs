using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Bloodline Awakening (Dynasty Spec §8) ────────────────────
// A forgotten descendant can awaken ancient divine power generations later.
// Dormant blessings have a small per-check chance, boosted by potential and by
// dramatic moments (near death, a sacred site, a direct miracle, high faith).

public class WorldEventContext
{
    public bool NearDeath { get; set; }
    public bool AtSacredSite { get; set; }
    public bool ReceivedDirectMiracle { get; set; }
    public bool HighFaithMoment { get; set; }
}

public interface IBloodlineAwakeningService
{
    /// <summary>Try to awaken the NPC's first dormant blessing; true if one awakened.</summary>
    bool TryAwaken(NpcDocument npc, WorldEventContext eventContext);
}

public class BloodlineAwakeningService : IBloodlineAwakeningService
{
    private readonly IRandomService _rng;

    public BloodlineAwakeningService(IRandomService rng) => _rng = rng;

    public bool TryAwaken(NpcDocument npc, WorldEventContext ctx)
    {
        foreach (var blessing in npc.InheritedBlessings)
        {
            if (blessing.State != BlessingState.Dormant) continue;

            float chance = 0.02f;
            chance += blessing.Potential / 500f;
            chance += ctx.NearDeath ? 0.08f : 0f;
            chance += ctx.AtSacredSite ? 0.06f : 0f;
            chance += ctx.ReceivedDirectMiracle ? 0.12f : 0f;
            chance += ctx.HighFaithMoment ? 0.07f : 0f;
            chance = Math.Clamp(chance, 0f, 0.65f);

            if (_rng.NextFloat() <= chance)
            {
                blessing.State = BlessingState.Active;
                blessing.Strength = Math.Max(blessing.Strength, blessing.Potential * 0.55f);
                return true;
            }
        }

        return false;
    }
}
