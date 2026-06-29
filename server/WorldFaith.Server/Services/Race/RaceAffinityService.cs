using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Race;

/// <summary>
/// Race Faith Affinity System — GDD v1.0 Section 9.
/// Manage affinity matrix giữa các race and god archetypes.
/// Affinity ảnh hưởng conversion speed, faith gain, trust, resistance to doubt.
/// Affinity = probability, not phải destiny — traits cá nhân có thể override.
/// </summary>
public interface IRaceAffinityService
{
    Task<float> GetAffinityAsync(string worldId, RaceType race, GodArchetype domain);
    Task<float> GetConversionModifierAsync(string worldId, RaceType race, GodArchetype domain, List<RaceTrait> npcTraits);
    Task<float> GetFaithGainModifierAsync(string worldId, RaceType race, GodArchetype domain);
    Task SeedRaceDataAsync(string worldId);
    Task RecordEnvironmentalMemoryAsync(string worldId, RaceType race, string godId, float trustDelta, string reason);
}

public class RaceAffinityService : IRaceAffinityService
{
    private readonly IRaceRepository _raceRepo;
    private readonly ILogger<RaceAffinityService> _logger;

    // ─── Default Affinity Matrix từ GDD v1.0 ────────────────
    // Format: race → (domain → percentage)
    private static readonly Dictionary<RaceType, Dictionary<GodArchetype, float>> DefaultMatrix = new()
    {
        [RaceType.Human] = new()
        {
            [GodArchetype.Order]     = 140f,
            [GodArchetype.Light]     = 130f,
            [GodArchetype.War]       = 130f,
            [GodArchetype.Knowledge] = 120f,
            [GodArchetype.Nature]    = 110f,
            [GodArchetype.Chaos]     = 90f,
            [GodArchetype.Darkness]  = 80f,
            [GodArchetype.Death]     = 80f,
        },
        [RaceType.Elf] = new()
        {
            [GodArchetype.Nature]    = 160f,
            [GodArchetype.Light]     = 140f,
            [GodArchetype.Knowledge] = 130f,
            [GodArchetype.Order]     = 100f,
            [GodArchetype.War]       = 80f,
            [GodArchetype.Chaos]     = 50f,
            [GodArchetype.Darkness]  = 40f,
            [GodArchetype.Death]     = 40f,
        },
        [RaceType.Dwarf] = new()
        {
            [GodArchetype.Order]     = 160f,
            [GodArchetype.Knowledge] = 140f,
            [GodArchetype.War]       = 130f,
            [GodArchetype.Light]     = 100f,
            [GodArchetype.Nature]    = 80f,
            [GodArchetype.Darkness]  = 70f,
            [GodArchetype.Death]     = 70f,
            [GodArchetype.Chaos]     = 40f,
        },
        [RaceType.Orc] = new()
        {
            [GodArchetype.War]       = 160f,
            [GodArchetype.Chaos]     = 140f,
            [GodArchetype.Death]     = 110f,
            [GodArchetype.Darkness]  = 100f,
            [GodArchetype.Nature]    = 90f,
            [GodArchetype.Order]     = 70f,
            [GodArchetype.Light]     = 60f,
            [GodArchetype.Knowledge] = 40f,
        },
        [RaceType.Beastfolk] = new()
        {
            [GodArchetype.Nature]    = 160f,
            [GodArchetype.Chaos]     = 130f,
            [GodArchetype.War]       = 120f,
            [GodArchetype.Death]     = 100f,
            [GodArchetype.Darkness]  = 90f,
            [GodArchetype.Light]     = 80f,
            [GodArchetype.Order]     = 50f,
            [GodArchetype.Knowledge] = 40f,
        },
        [RaceType.Demon] = new()
        {
            [GodArchetype.Darkness]  = 160f,
            [GodArchetype.Chaos]     = 150f,
            [GodArchetype.Death]     = 140f,
            [GodArchetype.War]       = 120f,
            [GodArchetype.Nature]    = 60f,
            [GodArchetype.Order]     = 40f,
            [GodArchetype.Knowledge] = 70f,
            [GodArchetype.Light]     = 20f,  // Taboo
        },
        [RaceType.Angel] = new()
        {
            [GodArchetype.Light]     = 160f,
            [GodArchetype.Order]     = 150f,
            [GodArchetype.Knowledge] = 120f,
            [GodArchetype.Nature]    = 100f,
            [GodArchetype.War]       = 80f,
            [GodArchetype.Chaos]     = 30f,
            [GodArchetype.Death]     = 20f,  // Taboo
            [GodArchetype.Darkness]  = 10f,  // Deep Taboo
        },
        [RaceType.Undead] = new()
        {
            [GodArchetype.Death]     = 160f,
            [GodArchetype.Darkness]  = 140f,
            [GodArchetype.Knowledge] = 110f,  // memory/history
            [GodArchetype.Chaos]     = 90f,
            [GodArchetype.Order]     = 70f,
            [GodArchetype.War]       = 80f,
            [GodArchetype.Light]     = 30f,
            [GodArchetype.Nature]    = 20f,
        },
    };

    // Trait overrides (bonus % thêm ando affinity)
    private static readonly Dictionary<RaceTrait, Dictionary<GodArchetype, float>> TraitBonuses = new()
    {
        [RaceTrait.Genius]       = new() { [GodArchetype.Knowledge] = 50f },
        [RaceTrait.Fanatic]      = new(), // amplifies current faith, no domain change
        [RaceTrait.Compassionate]= new() { [GodArchetype.Light] = 40f, [GodArchetype.Nature] = 20f },
        [RaceTrait.Ambitious]    = new() { [GodArchetype.War] = 30f, [GodArchetype.Darkness] = 20f, [GodArchetype.Order] = 20f },
        [RaceTrait.Curious]      = new() { [GodArchetype.Knowledge] = 30f, [GodArchetype.Chaos] = 20f },
        [RaceTrait.Traditional]  = new(), // strengthens existing religion, no new domain
        [RaceTrait.Reckless]     = new() { [GodArchetype.Chaos] = 40f, [GodArchetype.War] = 20f },
        [RaceTrait.Traumatized]  = new(), // context-dependent, handled separately
    };

    public RaceAffinityService(IRaceRepository raceRepo, ILogger<RaceAffinityService> logger)
    {
        _raceRepo = raceRepo;
        _logger = logger;
    }

    // ─── Affinity Lookup ──────────────────────────────────────

    public async Task<float> GetAffinityAsync(string worldId, RaceType race, GodArchetype domain)
    {
        // Ưu tiên DB (có environmental memory modifier)
        var raceDoc = await _raceRepo.GetByTypeAsync(worldId, race);
        if (raceDoc != null)
        {
            var entry = raceDoc.AffinityMatrix.FirstOrDefault(a => a.Domain == domain);
            if (entry != null) return entry.Percentage;
        }

        // Fallback to default matrix
        if (DefaultMatrix.TryGetValue(race, out var matrix) && matrix.TryGetValue(domain, out var pct))
            return pct;
        return 100f; // Neutral nếu not có data
    }

    /// <summary>
    /// Conversion modifier for NPC cụ thể.
    /// Kết hợp racial affinity + personal traits + environmental memory.
    /// </summary>
    public async Task<float> GetConversionModifierAsync(
        string worldId, RaceType race, GodArchetype domain, List<RaceTrait> npcTraits)
    {
        float baseAffinity = await GetAffinityAsync(worldId, race, domain);

        // Trait bonus
        float traitBonus = 0f;
        foreach (var trait in npcTraits)
        {
            if (TraitBonuses.TryGetValue(trait, out var bonuses) && bonuses.TryGetValue(domain, out var b))
                traitBonus += b;
        }

        // Fanatic trait: amplify current base by 30% (not add domain)
        if (npcTraits.Contains(RaceTrait.Fanatic))
            baseAffinity *= 1.3f;

        float total = MathF.Min(200f, baseAffinity + traitBonus);

        // Convert percentage to multiplier: 100% = 1.0x, 150% = 1.5x, 50% = 0.5x
        return total / 100f;
    }

    /// <summary>
    /// Faith gain multiplier for region with racial composition.
    /// </summary>
    public async Task<float> GetFaithGainModifierAsync(string worldId, RaceType race, GodArchetype domain)
    {
        float affinity = await GetAffinityAsync(worldId, race, domain);
        return affinity / 100f;  // 160% → 1.6x
    }

    // ─── Seed Race Data ───────────────────────────────────────

    public async Task SeedRaceDataAsync(string worldId)
    {
        var existing = await _raceRepo.GetByWorldAsync(worldId);
        if (existing.Any()) return;

        foreach (var (raceType, affinityMap) in DefaultMatrix)
        {
            var raceDoc = new RaceDocument
            {
                WorldId = worldId,
                Type = raceType,
                Name = raceType.ToString(),
                AffinityMatrix = affinityMap.Select(kv => new RaceAffinityEntry
                {
                    Domain = kv.Key,
                    Percentage = kv.Value
                }).ToList()
            };
            await _raceRepo.CreateAsync(raceDoc);
        }
        _logger.LogInformation("Seeded race affinity data for world {WorldId} ({Count} races)", worldId, DefaultMatrix.Count);
    }

    // ─── Environmental Memory ─────────────────────────────────

    /// <summary>
    /// Ghi nhớ sự kiện ảnh hưởng đến trust of race with god.
    /// Ví dụ: God floods orc camp → orcs mất trust with god đó.
    /// </summary>
    public async Task RecordEnvironmentalMemoryAsync(
        string worldId, RaceType race, string godId, float trustDelta, string reason)
    {
        var raceDoc = await _raceRepo.GetByTypeAsync(worldId, race);
        if (raceDoc == null) return;

        var key = $"god_{godId}";
        raceDoc.EnvironmentalMemory.TryGetValue(key, out float current);
        raceDoc.EnvironmentalMemory[key] = MathF.Clamp(current + trustDelta, -50f, 50f);

        await _raceRepo.UpdateAsync(raceDoc);
        _logger.LogInformation("Race {Race} memory updated for God {GodId}: {Delta:+0.0;-0.0} ({Reason})",
            race, godId, trustDelta, reason);
    }

    // ─── Static Helper ────────────────────────────────────────

    public static AffinityTier GetAffinityTier(float percentage) => percentage switch
    {
        >= 150f => AffinityTier.DeepHarmony,
        >= 120f => AffinityTier.Preferred,
        >= 90f  => AffinityTier.Neutral,
        >= 60f  => AffinityTier.Difficult,
        >= 30f  => AffinityTier.Rejected,
        _       => AffinityTier.Taboo
    };
}
