using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.Religion;
using WorldFaith.Shared.Contracts;
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
    private readonly IWorldRepository _worldRepo;
    private readonly IGodRepository _godRepo;
    private readonly IBalanceConfigService _balance;
    private readonly IGovernmentService _govService;
    private readonly IBelieverTypeService _believerTypes;
    private readonly ILogger<CivilizationSimulationService> _logger;
    private readonly Random _rng = new();

    private static readonly string[] CivNamePrefixes = { "Ara", "Sol", "Mor", "Thal", "Eld", "Vor", "Kha", "Zyn" };
    private static readonly string[] CivNameSuffixes = { "eth", "ian", "ara", "os", "um", "or", "ix", "ar" };

    private static readonly Dictionary<RaceType, GovernmentType> RaceDefaultGov = new()
    {
        [RaceType.Human]     = GovernmentType.Monarchy,
        [RaceType.Elf]       = GovernmentType.NobleCouncil,
        [RaceType.Dwarf]     = GovernmentType.NobleCouncil,
        [RaceType.Orc]       = GovernmentType.TribalClan,
        [RaceType.Beastfolk] = GovernmentType.TribalClan,
        [RaceType.Demon]     = GovernmentType.MonsterHorde,
        [RaceType.Angel]     = GovernmentType.Theocracy,
        [RaceType.Undead]    = GovernmentType.Monarchy,
    };

    public CivilizationSimulationService(
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IWorldRepository worldRepo,
        IGodRepository godRepo,
        IBalanceConfigService balance,
        IGovernmentService govService,
        IBelieverTypeService believerTypes,
        ILogger<CivilizationSimulationService> logger)
    {
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _worldRepo = worldRepo;
        _godRepo = godRepo;
        _balance = balance;
        _govService = govService;
        _believerTypes = believerTypes;
        _logger = logger;
    }

    public async Task SpawnInitialCivilizationsAsync(string worldId, int count)
    {
        var personalities = Enum.GetValues<CivilizationPersonality>();
        var races = Enum.GetValues<RaceType>();

        for (int i = 0; i < count; i++)
        {
            var race = races[_rng.Next(races.Length)];
            var civ = new CivilizationDocument
            {
                WorldId    = worldId,
                Name       = GenerateCivName(),
                Personality= personalities[_rng.Next(personalities.Length)],
                PrimaryRace= race,
                Government = RaceDefaultGov.GetValueOrDefault(race, GovernmentType.Monarchy),
                Population = _rng.Next(80, 150),
                Economy    = _rng.Next(30, 70),
                Military   = _rng.Next(10, 40),
                Food       = _rng.Next(40, 80),
                Stability  = _rng.Next(50, 80),
                Happiness  = _rng.Next(40, 70),
                ControlledTiles = GenerateStartingTiles(i, count)
            };
            await _civRepo.CreateAsync(civ);
        }

        _logger.LogInformation("Spawned {Count} civilizations for world {WorldId}", count, worldId);
    }

    public async Task<List<CivilizationUpdateEvent>> TickAsync(string worldId, long tick)
    {
        var civs    = await _civRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        var world   = await _worldRepo.GetByIdAsync(worldId);
        var updates = new List<CivilizationUpdateEvent>();

        foreach (var civ in civs)
        {
            if (civ.State == CivilizationState.Fallen) continue;
            bool changed = false;

            // ── Government modifiers ──────────────────────────
            await _govService.ApplyGovernmentModifiersAsync(civ, tick);

            // ── Forbidden God: penalize civs that outlawed a god ──
            if (world != null && world.ForbiddenGodIds.Count > 0)
                changed |= await ApplyForbiddenGodPenaltyAsync(civ, world);

            // ── Civ-level forbidden: suppress god within civ ──
            changed |= await SuppressForbiddenGodsAsync(civ);

            // ── Rebellion check ───────────────────────────────
            float rebellionRisk = _govService.GetRebellionRisk(civ);
            if (_rng.NextDouble() < rebellionRisk * 0.01f)  // per tick
            {
                civ.Stability = Math.Max(0f, civ.Stability - 10f);
                civ.AiMemory.GodTrustLevel -= 5f;
                _logger.LogInformation("Civ {Name} rebellion flare (risk={Risk:P1})", civ.Name, rebellionRisk);
                changed = true;
            }

            // ── Food/famine tick ──────────────────────────────
            changed |= SimulateFoodCycle(civ, tick);

            // ── AI personality behavior ───────────────────────
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

            // ── Government evolution (every 200 ticks) ────────
            if (tick % 200 == 0)
                await _govService.EvolveGovernmentAsync(civ);

            changed |= await SimulatePopulationGrowthAsync(civ);
            changed |= CheckStateTransition(civ);

            if (changed)
            {
                await _civRepo.UpdateAsync(civ);
                updates.Add(MapToEvent(civ));
            }
        }

        return updates;
    }

    // ─── Forbidden God System ─────────────────────────────────

    private async Task<bool> ApplyForbiddenGodPenaltyAsync(CivilizationDocument civ, WorldDocument world)
    {
        if (civ.RulingReligionId == null) return false;
        var religion = await _religionRepo.GetByIdAsync(civ.RulingReligionId);
        if (religion == null) return false;

        // If the ruling god is forbidden world-wide
        if (world.ForbiddenGodIds.Contains(religion.GodId))
        {
            // Faith/trust drops — outsiders won't convert here
            civ.AiMemory.GodTrustLevel -= 2f;
            civ.Stability -= 0.5f;  // Internal tension

            // Cultists form to preserve the forbidden faith
            if (_rng.NextDouble() < 0.05)
            {
                await _believerTypes.ShiftBelieverTypesAsync(religion.Id, "Persecution", 0.05f);
                _logger.LogInformation("Civ {Name} followers go underground (god is forbidden)", civ.Name);
            }
            return true;
        }
        return false;
    }

    private async Task<bool> SuppressForbiddenGodsAsync(CivilizationDocument civ)
    {
        // Theocracy and Monarchy aggressively suppress rival religions
        if (civ.Government != GovernmentType.Theocracy && civ.Government != GovernmentType.Monarchy)
            return false;
        // Suppression logic handled by FaithService and GodRankService
        return false;
    }

    // ─── Food / Famine ────────────────────────────────────────

    private bool SimulateFoodCycle(CivilizationDocument civ, long tick)
    {
        if (tick % 20 != 0) return false;

        // Food consumed by population
        float consumption = civ.Population * 0.01f;
        civ.Food -= consumption;

        // Natural food regeneration based on economy
        float regen = civ.Economy * 0.05f;
        civ.Food = Math.Clamp(civ.Food + regen, 0f, 200f);

        // Famine
        if (civ.Food < 10f)
        {
            civ.Population = (int)(civ.Population * 0.98f); // 2% die per cycle
            civ.Happiness -= 5f;
            civ.Stability -= 3f;
        }
        else if (civ.Food > 80f)
        {
            civ.Happiness = Math.Min(100f, civ.Happiness + 0.5f);
        }

        civ.Happiness = Math.Clamp(civ.Happiness, 0f, 100f);
        civ.Stability = Math.Clamp(civ.Stability, 0f, 100f);
        return true;
    }

    private bool SimulateAggressive(CivilizationDocument civ, List<CivilizationDocument> allCivs, long tick)
    {
        // Mỗi 10 tick thử attacks civ gần nhất nếu đủ military
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
        // Tập trung build economy and military
        if (tick % 5 != 0) return false;

        civ.Economy += _rng.Next(1, 5);
        civ.Military += _rng.Next(1, 3);
        civ.Economy = Math.Min(civ.Economy, 200f);
        civ.Military = Math.Min(civ.Military, 150f);
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
            religion.DevotionLevel = Math.Min(1f, religion.DevotionLevel + 0.01f);
        }

        return true;
    }

    private bool SimulateLogical(CivilizationDocument civ, long tick)
    {
        // Cân bằng economy and military dựa theo situation
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
