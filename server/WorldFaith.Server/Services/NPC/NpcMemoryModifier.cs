using WorldFaith.Server.Models;

namespace WorldFaith.Server.Services.NPC;

// ─── NPC Memory (NPC Master Spec §8) ──────────────────────────
// Memory is what separates a living society from a random simulator: NPCs
// reinterpret new events through what they remember of a god's past actions.

public static class NpcMemoryModifier
{
    /// <summary>
    /// Combined belief modifier a god carries with a set of memories (Spec §8):
    /// good memories (trust/fear) raise it, doubt lowers it, weighted by strength.
    /// Clamped to [0.25, 2.25].
    /// </summary>
    public static float CalculateMemoryModifier(IEnumerable<NpcMemory> memories, string godId)
    {
        float modifier = 1.0f;

        foreach (var memory in memories.Where(m => m.GodId == godId))
        {
            modifier += (memory.TrustChange / 100f) * memory.Strength;
            modifier -= (memory.DoubtChange / 120f) * memory.Strength;
            modifier += (memory.FearChange / 150f) * memory.Strength;
        }

        return Math.Clamp(modifier, 0.25f, 2.25f);
    }

    /// <summary>
    /// Fade memory strength with age. Cultural memories persist (a region never
    /// forgets a god that ended a famine); personal memories decay by
    /// <see cref="NpcMemory.DecayPerAge"/> per 100 ticks. Returns memories whose
    /// strength has faded to ~zero so callers can prune them.
    /// </summary>
    public static List<NpcMemory> Decay(IEnumerable<NpcMemory> memories, long currentTick)
    {
        var faded = new List<NpcMemory>();

        foreach (var memory in memories)
        {
            if (memory.IsCulturalMemory) continue;

            long age = Math.Max(0, currentTick - memory.CreatedTick);
            float ageUnits = age / 100f;
            memory.Strength = Math.Max(0f, 1f - memory.DecayPerAge * ageUnits);

            if (memory.Strength <= 0.01f) faded.Add(memory);
        }

        return faded;
    }
}
