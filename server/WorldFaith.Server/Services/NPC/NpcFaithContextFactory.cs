using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

/// <summary>
/// Bridges persistent NPC state (Phase 3) to the belief-math service (Phase 2):
/// turns an <see cref="NpcDocument"/> plus situational inputs into an
/// <see cref="NpcFaithContext"/>. Race affinity is supplied by the caller
/// (from the existing IRaceAffinityService); traits are layered on here.
/// </summary>
public static class NpcFaithContextFactory
{
    public static NpcFaithContext ForConversion(
        NpcDocument npc,
        GodArchetype targetArchetype,
        float raceAffinity,
        float socialPressure = 1f,
        float recentEventImpact = 1f,
        float trustDifference = 1f,
        float fearPressure = 1f)
    {
        return new NpcFaithContext
        {
            Npc = npc,
            BaseOpenness = Math.Clamp(npc.Openness / 100f, 0.01f, 1f),
            RaceAffinity = raceAffinity,
            TraitModifier = TraitAffinity.GetTraitModifier(npc.Traits, targetArchetype),
            SocialPressure = socialPressure,
            RecentEventImpact = recentEventImpact,
            TrustDifference = trustDifference,
            FearPressure = fearPressure,
            Faith = npc.Faith,
            Trust = npc.Trust,
            Doubt = npc.Doubt,
            Loyalty = npc.Loyalty,
            Openness = npc.Openness,
            Traits = npc.Traits,
        };
    }
}
