using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// Race ↔ bloodline-domain compatibility (Dynasty Spec §5 balancing table:
// Race Affinity 0.2–1.7). Race modifies probability, never hard-locks: any race
// can carry any bloodline, some just far more easily. Separate from the existing
// faith-economy IRaceAffinityService, which is repo-based and uses GodArchetype.
public interface IBloodlineAffinityService
{
    float GetDomainAffinity(RaceType race, GodDomain domain);
}

public class BloodlineAffinityService : IBloodlineAffinityService
{
    private static readonly Dictionary<(RaceType, GodDomain), float> Matrix = new()
    {
        [(RaceType.Human, GodDomain.Light)]      = 1.10f,
        [(RaceType.Human, GodDomain.Order)]      = 1.15f,
        [(RaceType.Human, GodDomain.War)]        = 1.05f,
        [(RaceType.Human, GodDomain.Knowledge)]  = 1.05f,

        [(RaceType.Elf, GodDomain.Nature)]       = 1.60f,
        [(RaceType.Elf, GodDomain.Light)]        = 1.30f,
        [(RaceType.Elf, GodDomain.Moon)]         = 1.35f,
        [(RaceType.Elf, GodDomain.Knowledge)]    = 1.25f,
        [(RaceType.Elf, GodDomain.Darkness)]     = 0.55f,
        [(RaceType.Elf, GodDomain.Chaos)]        = 0.70f,

        [(RaceType.Dwarf, GodDomain.Order)]      = 1.40f,
        [(RaceType.Dwarf, GodDomain.Fire)]       = 1.30f,
        [(RaceType.Dwarf, GodDomain.Knowledge)]  = 1.15f,

        [(RaceType.Orc, GodDomain.War)]          = 1.55f,
        [(RaceType.Orc, GodDomain.Fire)]         = 1.20f,
        [(RaceType.Orc, GodDomain.Nature)]       = 0.95f,
        [(RaceType.Orc, GodDomain.Knowledge)]    = 0.65f,

        [(RaceType.Beastfolk, GodDomain.Nature)] = 1.50f,
        [(RaceType.Beastfolk, GodDomain.Moon)]   = 1.20f,

        [(RaceType.Demon, GodDomain.Darkness)]   = 1.65f,
        [(RaceType.Demon, GodDomain.Chaos)]      = 1.45f,
        [(RaceType.Demon, GodDomain.Fire)]       = 1.40f,
        [(RaceType.Demon, GodDomain.Light)]      = 0.35f,

        [(RaceType.Angel, GodDomain.Light)]      = 1.70f,
        [(RaceType.Angel, GodDomain.Order)]      = 1.30f,
        [(RaceType.Angel, GodDomain.Darkness)]   = 0.25f,

        [(RaceType.Undead, GodDomain.Death)]     = 1.65f,
        [(RaceType.Undead, GodDomain.Darkness)]  = 1.40f,
        [(RaceType.Undead, GodDomain.Light)]     = 0.20f,
    };

    public float GetDomainAffinity(RaceType race, GodDomain domain)
        => Matrix.TryGetValue((race, domain), out var v) ? v : 1.0f;
}
