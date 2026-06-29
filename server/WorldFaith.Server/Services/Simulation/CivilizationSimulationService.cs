using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Services.Simulation;

public interface ICivilizationSimulationService
{
    Task<List<CivilizationUpdateEvent>> TickAsync(string worldId, long tick);
    Task SpawnInitialCivilizationsAsync(string worldId, int count);
}

public class CivilizationSimulationService : ICivilizationSimulationService
{
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IBalanceConfigService _balance;
    private readonly ILogger<CivilizationSimulationService> _logger;
    private readonly Random _rng = new();

    private static readonly string[] CivNamePrefixes = { "Ara", "Sol", "Mor", "Thal", "Eld", "Vor", "Kha", "Zyn" };
    private static readonly string[] CivNameSuffixes = { "eth", "ian", "ara", "os", "um", "or", "ix", "ar" };

    public CivilizationSimulationService(
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IBalanceConfigService balance,
        ILogger<CivilizationSimulationService> logger)
    {
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _balance = balance;
        _logger = logger;
    }

    public async Task SpawnInitialCivilizationsAsync(string worldId, int count)
    {
        var personalities = Enum.GetValues<CivilizationPersonality>();

        for (int i = 0; i < count; i++)
        {
            var civ = new CivilizationDocument
            {
                WorldId = worldId,
                Name = GenerateCivName(),
                Personality = personalities[_rng.Next(personalities.Length)],
                Population = _rng.Next(80, 150),
                Economy = _rng.Next(30, 70),
                Military = _rng.Next(10, 40),
                ControlledTiles = GenerateStartingTiles(i, count)
            };
            await _civRepo.CreateAsync(civ);
        }

        _logger.LogInformation("Spawned {Count} civilizations for world {WorldId}", count, worldId);
    }

    public async Task<List<CivilizationUpdateEvent>> TickAsync(string worldId, long tick)
    {
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        var updates = new List<CivilizationUpdateEvent>();

        foreach (var civ in civs)
        {
            bool changed = false;

            // AI hành động theo personality
            switch (civ.Personality)
            {
                case CivilizationPersonality.Aggressive:
                    changed |= SimulateAggressive(civ, civs, tick);
                    break;
                case CivilizationPersonality.Defensive:
                    changed |= SimulateDefensive(civ, tick);
                    break;
                case CivilizationPersonality.Fanatic:
                    changed |= SimulateFanatic(civ, religions, tick);
                    break;
                case CivilizationPersonality.Logical:
                    changed |= SimulateLogical(civ, tick);
                    break;
                case CivilizationPersonality.Opportunistic:
                    changed |= SimulateOpportunistic(civ, civs, religions, tick);
                    break;
            }

            // Tăng trưởng dân số từ balance config
            changed |= await SimulatePopulationGrowthAsync(civ);

            // Kiểm tra state transition
            changed |= CheckStateTransition(civ);

            if (changed)
            {
                await _civRepo.UpdateAsync(civ);
                updates.Add(MapToEvent(civ));
            }
        }

        return updates;
    }

    // ─── AI Behaviors ──────────────────────────────────────

    private bool SimulateAggressive(CivilizationDocument civ, List<CivilizationDocument> allCivs, long tick)
    {
        // Mỗi 10 tick thử tấn công civ gần nhất nếu đủ military
        if (tick % 10 != 0) return false;
        if (civ.Military < 40f) return false;
        if (civ.State == CivilizationState.Collapsing) return false;

        var nearestEnemy = allCivs
            .Where(c => c.Id != civ.Id && !c.IsAtWar)
            .OrderBy(_ => _rng.Next())
            .FirstOrDefault();

        if (nearestEnemy == null) return false;

        civ.IsAtWar = true;
        civ.AiMemory.CurrentTarget = nearestEnemy.Id;
        civ.AiMemory.TicksAtWar = 0;
        return true;
    }

    private bool SimulateDefensive(CivilizationDocument civ, long tick)
    {
        // Tập trung build economy và military
        if (tick % 5 != 0) return false;

        civ.Economy += _rng.Next(1, 5);
        civ.Military += _rng.Next(1, 3);
        civ.Economy = MathF.Min(civ.Economy, 200f);
        civ.Military = MathF.Min(civ.Military, 150f);
        return true;
    }

    private bool SimulateFanatic(CivilizationDocument civ, List<ReligionDocument> religions, long tick)
    {
        // Ưu tiên spread religion, tăng devotion
        if (tick % 7 != 0) return false;
        if (!civ.ReligionIds.Any()) return false;

        var religion = religions.FirstOrDefault(r => civ.ReligionIds.Contains(r.Id));
        if (religion != null)
        {
            religion.DevotionLevel = MathF.Min(1f, religion.DevotionLevel + 0.01f);
        }

        return true;
    }

    private bool SimulateLogical(CivilizationDocument civ, long tick)
    {
        // Cân bằng economy và military dựa theo situation
        if (tick % 8 != 0) return false;

        if (civ.Economy < civ.Military)
            civ.Economy += _rng.Next(2, 6);
        else
            civ.Military += _rng.Next(1, 4);

        return true;
    }

    private bool SimulateOpportunistic(CivilizationDocument civ, List<CivilizationDocument> allCivs,
        List<ReligionDocument> religions, long tick)
    {
        // Tấn công civ yếu, join religion mạnh nhất
        if (tick % 12 != 0) return false;

        var weakestCiv = allCivs
            .Where(c => c.Id != civ.Id && c.Military < civ.Military * 0.6f)
            .OrderBy(c => c.Military)
            .FirstOrDefault();

        if (weakestCiv != null && !civ.IsAtWar)
        {
            civ.IsAtWar = true;
            civ.AiMemory.CurrentTarget = weakestCiv.Id;
            return true;
        }

        return false;
    }

    private async Task<bool> SimulatePopulationGrowthAsync(CivilizationDocument civ)
    {
        if (civ.State == CivilizationState.Fallen) return false;

        float growthRate = civ.State switch
        {
            CivilizationState.Tribal    => await _balance.GetFloatAsync("civ.pop_growth_tribal"),
            CivilizationState.Kingdom   => await _balance.GetFloatAsync("civ.pop_growth_kingdom"),
            CivilizationState.Empire    => await _balance.GetFloatAsync("civ.pop_growth_empire"),
            CivilizationState.Collapsing=> -await _balance.GetFloatAsync("civ.pop_decay_collapsing"),
            _ => 0f
        };

        int delta = (int)(civ.Population * growthRate);
        if (delta == 0) return false;
        civ.Population = Math.Max(0, civ.Population + delta);
        return true;
    }

    private bool SimulatePopulationGrowth(CivilizationDocument civ)
    {
        // Legacy sync wrapper — dùng async version trong tick
        return false;
    }

    private bool CheckStateTransition(CivilizationDocument civ)
    {
        var oldState = civ.State;

        if (civ.Population <= 0)
        {
            civ.State = CivilizationState.Fallen;
        }
        else if (civ.Population < 50)
        {
            civ.State = CivilizationState.Collapsing;
        }
        else if (civ.Population >= 5000 && civ.Economy >= 150f)
        {
            civ.State = CivilizationState.Empire;
        }
        else if (civ.Population >= 500 && civ.Economy >= 80f)
        {
            civ.State = CivilizationState.Kingdom;
        }
        else if (civ.State != CivilizationState.Fallen)
        {
            civ.State = CivilizationState.Tribal;
        }

        return civ.State != oldState;
    }

    // ─── Helpers ───────────────────────────────────────────

    private string GenerateCivName()
    {
        var prefix = CivNamePrefixes[_rng.Next(CivNamePrefixes.Length)];
        var suffix = CivNameSuffixes[_rng.Next(CivNameSuffixes.Length)];
        return prefix + suffix;
    }

    private List<TileCoord> GenerateStartingTiles(int civIndex, int totalCivs)
    {
        // Phân bố đều trên map 64x64
        int sector = 64 / (int)Math.Sqrt(totalCivs);
        int row = civIndex / (int)Math.Sqrt(totalCivs);
        int col = civIndex % (int)Math.Sqrt(totalCivs);
        int baseX = col * sector + sector / 2;
        int baseY = row * sector + sector / 2;

        return new List<TileCoord>
        {
            new() { X = baseX, Y = baseY },
            new() { X = baseX + 1, Y = baseY },
            new() { X = baseX, Y = baseY + 1 },
        };
    }

    private static CivilizationUpdateEvent MapToEvent(CivilizationDocument civ) => new()
    {
        CivilizationId = civ.Id,
        Name = civ.Name,
        Population = civ.Population,
        State = civ.State,
        NewRulingReligionId = civ.RulingReligionId,
        Collapsed = civ.State == CivilizationState.Fallen
    };
}
