using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Memory;

/// <summary>
/// Memory System — GDD v1.0 Section 7 (God resources: Memory).
/// Relics generate passive Faith for origin god.
/// Forgotten gods survive qua relics and cults.
/// Environmental memory tích lũy theo race/region.
/// </summary>
public interface IMemoryService
{
    Task<List<DeltaEvent>> TickAsync(string worldId, long tick);
    Task<float> GetRelicFaithGenerationAsync(string worldId, string godId);
    Task TransferRelicOwnershipAsync(string relicId, string? newOwnerId, string? newCivId);
}

public class MemoryService : IMemoryService
{
    private readonly IRelicRepository _relicRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IRaceRepository _raceRepo;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IRelicRepository relicRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IRaceRepository raceRepo,
        ILogger<MemoryService> logger)
    {
        _relicRepo = relicRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _raceRepo = raceRepo;
        _logger = logger;
    }

    // ─── Tick — relic faith gen, decay ───────────────────────

    public async Task<List<DeltaEvent>> TickAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var relics = await _relicRepo.GetByWorldAsync(worldId);

        foreach (var relic in relics)
        {
            if (!relic.IsActive || relic.OriginGodId == null) continue;

            var god = await _godRepo.GetByIdAsync(relic.OriginGodId);
            if (god == null) continue;

            // Relic phát Faith thụ động for origin god
            float faithGen = relic.FaithBonus;

            // Bonus if civ holding relic follows the god's religion
            if (relic.LocationCivId != null)
            {
                var civ = await _civRepo.GetByIdAsync(relic.LocationCivId);
                if (civ?.RulingReligionId != null)
                {
                    // Add 50% if ruling religion of civ matches this god
                    faithGen *= 1.5f;
                }
            }

            // Forgotten god: relic là nguồn faith duy nhất
            if (god.IsForgotten)
            {
                god.Faith = MathF.Min(500f, god.Faith + faithGen);
                await _godRepo.UpdateAsync(god);

                // Ghi memory nếu đây là tick quan trọng
                if (tick % 100 == 0)
                    _logger.LogDebug("Forgotten god {Name} sustained by relic '{Relic}': +{Faith:F1} faith",
                        god.Name, relic.Name, faithGen);
            }
            else
            {
                // God is alive — relic is a bonus
                god.Faith = MathF.Min(1000f, god.Faith + faithGen * 0.5f);
                await _godRepo.UpdateAsync(god);
            }

            // Relic decay nếu not ai giữ
            bool isAbandoned = relic.CurrentOwnerId == null && relic.LocationCivId == null && relic.LocationDungeonId == null;
            if (isAbandoned && tick % 200 == 0)
            {
                relic.FaithBonus = MathF.Max(0.1f, relic.FaithBonus - 0.5f);
                relic.MemoryPower = MathF.Max(5f, relic.MemoryPower - 1f);
                await _relicRepo.UpdateAsync(relic);
            }
        }

        // Environmental memory decay (very slow)
        if (tick % 500 == 0)
        {
            var races = await _raceRepo.GetByWorldAsync(worldId);
            foreach (var race in races)
            {
                bool changed = false;
                foreach (var key in race.EnvironmentalMemory.Keys.ToList())
                {
                    // Memory fades 5% per 500 ticks
                    race.EnvironmentalMemory[key] *= 0.95f;
                    if (MathF.Abs(race.EnvironmentalMemory[key]) < 0.5f)
                    {
                        race.EnvironmentalMemory.Remove(key);
                    }
                    changed = true;
                }
                if (changed) await _raceRepo.UpdateAsync(race);
            }
        }

        return deltas;
    }

    // ─── Relic Faith Calculation ──────────────────────────────

    public async Task<float> GetRelicFaithGenerationAsync(string worldId, string godId)
    {
        var relics = await _relicRepo.GetByGodAsync(worldId, godId);
        return relics.Where(r => r.IsActive).Sum(r => r.FaithBonus);
    }

    // ─── Relic Transfer ───────────────────────────────────────

    public async Task TransferRelicOwnershipAsync(string relicId, string? newOwnerId, string? newCivId)
    {
        var relic = await _relicRepo.GetByIdAsync(relicId);
        if (relic == null) return;

        relic.CurrentOwnerId = newOwnerId;
        relic.LocationCivId = newCivId;
        await _relicRepo.UpdateAsync(relic);

        _logger.LogInformation("Relic '{Name}' transferred → NPC:{NpcId} / Civ:{CivId}",
            relic.Name, newOwnerId ?? "none", newCivId ?? "none");
    }
}
