using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Race Age Profiles & Population Families (Dynasty Spec §9, §10) ──
// Keep commoners as aggregated groups for performance; only promote a member to
// a full named NPC on a notable event. Generation pace and decay come from race.

public static class RaceAgeProfiles
{
    public static readonly IReadOnlyDictionary<RaceType, RaceAgeProfile> All =
        new Dictionary<RaceType, RaceAgeProfile>
        {
            [RaceType.Human]     = new() { Race = RaceType.Human,     AverageLifespanYears = 80,   AdultAgeYears = 18,  TypicalParenthoodStart = 22,  TypicalGenerationYears = 25,  BloodlineDecayModifier = 1.0f },
            [RaceType.Elf]       = new() { Race = RaceType.Elf,       AverageLifespanYears = 850,  AdultAgeYears = 100, TypicalParenthoodStart = 140, TypicalGenerationYears = 180, FertilityModifier = 0.5f, BloodlineDecayModifier = 0.45f },
            [RaceType.Dwarf]     = new() { Race = RaceType.Dwarf,     AverageLifespanYears = 320,  AdultAgeYears = 45,  TypicalParenthoodStart = 70,  TypicalGenerationYears = 85,  BloodlineDecayModifier = 0.7f },
            [RaceType.Orc]       = new() { Race = RaceType.Orc,       AverageLifespanYears = 65,   AdultAgeYears = 14,  TypicalParenthoodStart = 18,  TypicalGenerationYears = 20,  FertilityModifier = 1.2f, BloodlineDecayModifier = 1.15f },
            [RaceType.Beastfolk] = new() { Race = RaceType.Beastfolk, AverageLifespanYears = 90,   AdultAgeYears = 14,  TypicalParenthoodStart = 18,  TypicalGenerationYears = 22,  FertilityModifier = 1.15f, MutationModifier = 1.2f },
            [RaceType.Demon]     = new() { Race = RaceType.Demon,     AverageLifespanYears = 500,  AdultAgeYears = 40,  TypicalParenthoodStart = 70,  TypicalGenerationYears = 90,  BloodlineDecayModifier = 0.9f, MutationModifier = 1.8f },
            [RaceType.Angel]     = new() { Race = RaceType.Angel,     AverageLifespanYears = 2000, AdultAgeYears = 200, TypicalParenthoodStart = 300, TypicalGenerationYears = 400, FertilityModifier = 0.25f, BloodlineDecayModifier = 0.3f },
            [RaceType.Undead]    = new() { Race = RaceType.Undead,    AverageLifespanYears = 9999, AdultAgeYears = 0,   TypicalParenthoodStart = 0,   TypicalGenerationYears = 9999, FertilityModifier = 0f, BloodlineDecayModifier = 0.2f },
        };

    public static RaceAgeProfile Get(RaceType race)
        => All.TryGetValue(race, out var p) ? p : All[RaceType.Human];
}

public interface IPopulationFamilyService
{
    /// <summary>Years between generations for a race (Undead ~ never reproduce).</summary>
    int GenerationYears(RaceType race);

    /// <summary>
    /// Advance a commoner group by one generation: apply births (scaled by the
    /// race's fertility) and deaths. Returns the net population change.
    /// </summary>
    int SimulateGeneration(PopulationFamilyGroup group);
}

public class PopulationFamilyService : IPopulationFamilyService
{
    public int GenerationYears(RaceType race) => RaceAgeProfiles.Get(race).TypicalGenerationYears;

    public int SimulateGeneration(PopulationFamilyGroup group)
    {
        var profile = RaceAgeProfiles.Get(group.Race);

        int births = (int)MathF.Round(group.Count * group.BirthRatePerGeneration * profile.FertilityModifier);
        int deaths = (int)MathF.Round(group.Count * group.DeathRatePerGeneration);
        int net = births - deaths;

        group.Count = Math.Max(0, group.Count + net);
        return net;
    }
}
