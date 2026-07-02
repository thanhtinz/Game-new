using WorldFaith.Shared.Models;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.Dungeon;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Simulation;

/// <summary>
/// AI Director — GDD v1.0 Section 21.
/// Controls pacing, Age transitions, and crisis generation.
/// Keeps game tension alive — prevents stagnation and snowballing.
/// </summary>
public interface IAiDirectorService
{
    Task<List<DeltaEvent>> TickAsync(string worldId, long tick);
    Task<WorldAge> GetCurrentAgeAsync(string worldId);
}

public class AiDirectorService : IAiDirectorService
{
    private readonly IWorldRepository _worldRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IDungeonService _dungeonService;
    private readonly IBalanceConfigService _balance;
    private readonly ILogger<AiDirectorService> _logger;
    private readonly Random _rng = new();

    // Age thresholds (ticks)
    private static readonly Dictionary<WorldAge, long> AgeThresholds = new()
    {
        [WorldAge.EarlyAge]    = 0,
        [WorldAge.KingdomAge]  = 100,
        [WorldAge.ConflictAge] = 300,
        [WorldAge.CollapseAge] = 600,
        [WorldAge.RebirthAge]  = 850,
    };

    public AiDirectorService(
        IWorldRepository worldRepo,
        ICivilizationRepository civRepo,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        IDungeonService dungeonService,
        IBalanceConfigService balance,
        ILogger<AiDirectorService> logger)
    {
        _worldRepo = worldRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _dungeonService = dungeonService;
        _balance = balance;
        _logger = logger;
    }

    public async Task<List<DeltaEvent>> TickAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return deltas;

        // ── Age transition check ──────────────────────────────
        var newAge = CalculateAge(world, tick);
        if (newAge != world.CurrentAge)
        {
            world.CurrentAge = newAge;
            await _worldRepo.UpdateAsync(world);
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.MiraclePerformed,
                Description = $"🌍 Thế giới bước ando {GetAgeName(newAge)}!",
            });
            _logger.LogInformation("World {Id} entered {Age}", worldId, newAge);

            // Age-specific events
            deltas.AddRange(await OnAgeTransitionAsync(worldId, newAge, tick));
        }

        // ── Anti-stagnation: inject crisis if nothing happening ──
        if (tick % 80 == 0)
            deltas.AddRange(await CheckStagnationAsync(worldId, tick));

        // ── Anti-snowball: boost weaker gods ─────────────────
        if (tick % 150 == 0)
            deltas.AddRange(await BalancePowerAsync(worldId));

        // ── Collapse Age: force crisis events ────────────────
        if (world.CurrentAge == WorldAge.CollapseAge && tick % 50 == 0)
            deltas.AddRange(await GenerateCollapseEventAsync(worldId, tick));

        return deltas;
    }

    // ─── Age Calculation ──────────────────────────────────────

    private WorldAge CalculateAge(WorldDocument world, long tick)
    {
        // Base tick threshold
        WorldAge age = WorldAge.EarlyAge;
        foreach (var (a, threshold) in AgeThresholds.OrderBy(kv => kv.Value))
            if (tick >= threshold) age = a;

        // Also check civ state for early Collapse
        return age;
    }

    private static string GetAgeName(WorldAge age) => age switch
    {
        WorldAge.EarlyAge    => "Early Age",
        WorldAge.KingdomAge  => "Kingdom Age",
        WorldAge.ConflictAge => "Conflict Age",
        WorldAge.CollapseAge => "Collapse Age",
        WorldAge.RebirthAge  => "Rebirth Age",
        _ => age.ToString()
    };

    // ─── Age Transition Events ────────────────────────────────

    private async Task<List<DeltaEvent>> OnAgeTransitionAsync(string worldId, WorldAge age, long tick)
    {
        var deltas = new List<DeltaEvent>();

        switch (age)
        {
            case WorldAge.KingdomAge:
                // Kingdoms start forming — spawn dungeons near civs
                var civs = await _civRepo.GetByWorldAsync(worldId);
                foreach (var civ in civs.Take(3))
                {
                    var tile = civ.ControlledTiles.FirstOrDefault();
                    if (tile != null)
                        await _dungeonService.SpawnDungeonAsync(worldId,
                            tile.X + _rng.Next(-5, 5), tile.Y + _rng.Next(-5, 5),
                            DungeonType.AncientRuins);
                }
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    Description = "Kingdoms are expanding. Ancient ruins emerge across the land."
                });
                break;

            case WorldAge.ConflictAge:
                // Holy wars become possible — spawn ForbiddenSanctum
                await _dungeonService.SpawnDungeonAsync(worldId,
                    _rng.Next(10, 54), _rng.Next(10, 54), DungeonType.ForbiddenSanctum);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    Description = "⚔️ The Age of Conflict! Holy wars erupt. Sacred sites are sealed."
                });
                break;

            case WorldAge.CollapseAge:
                // DarkPortals spawn, civs start collapsing
                for (int i = 0; i < 2; i++)
                    await _dungeonService.SpawnDungeonAsync(worldId,
                        _rng.Next(5, 59), _rng.Next(5, 59), DungeonType.DarkPortal);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    Description = "💀 The world is collapsing! Dark portals open. Empires begin to crumble."
                });
                break;

            case WorldAge.RebirthAge:
                // New civs can emerge from collapsed ones
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.MiraclePerformed,
                    Description = "🌱 The Rebirth Age. New civilizations sprout from the ruins."
                });
                break;
        }

        return deltas;
    }

    // ─── Anti-Stagnation ──────────────────────────────────────

    private async Task<List<DeltaEvent>> CheckStagnationAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);

        // Check if any wars are happening
        bool anyWar = civs.Any(c => c.IsAtWar);
        int aliveGods = gods.Count(g => g.IsAlive && !g.IsForgotten);

        // If game is too peaceful for too long in Conflict Age
        if (!anyWar && aliveGods > 1 && tick > 300)
        {
            // Trigger natural disaster to shake things up
            if (_rng.NextDouble() < 0.15)
            {
                var target = civs.Where(c => c.State != CivilizationState.Fallen)
                    .OrderBy(_ => _rng.Next()).FirstOrDefault();
                if (target != null)
                {
                    target.Stability -= 20f;
                    target.Economy   -= 15f;
                    target.Stability = Math.Clamp(target.Stability, 0f, 100f);
                    await _civRepo.UpdateAsync(target);
                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.DivineConflict,
                        TargetId = target.Id,
                        Description = $"⚡ An unexpected natural disaster strikes {target.Name}!"
                    });
                }
            }
        }

        // If one god has > 60% followers → form alliance pressure
        int totalFollowers = gods.Where(g => g.IsAlive).Sum(g => g.FollowerCount);
        var dominant = gods.FirstOrDefault(g => g.IsAlive && totalFollowers > 0
            && (float)g.FollowerCount / totalFollowers > 0.6f);

        if (dominant != null)
        {
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.DivineConflict,
                SourceGodId = dominant.Id,
                Description = $"👑 {dominant.Name} is dominant — an alliance is forming against them!"
            });
        }

        return deltas;
    }

    // ─── Anti-Snowball ────────────────────────────────────────

    private async Task<List<DeltaEvent>> BalancePowerAsync(string worldId)
    {
        var deltas = new List<DeltaEvent>();
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var alive = gods.Where(g => g.IsAlive && !g.IsForgotten).ToList();
        if (alive.Count < 2) return deltas;

        // Weakest god gets a small faith boost via "desperate followers" mechanic
        var weakest = alive.OrderBy(g => g.FollowerCount).First();
        var strongest = alive.OrderByDescending(g => g.FollowerCount).First();

        if (strongest.FollowerCount > weakest.FollowerCount * 3)
        {
            // Schism in dominant religion — some followers become curious
            float schismBonus = weakest.Faith + 50f;
            weakest.Faith = Math.Min(1000f, schismBonus);
            await _godRepo.UpdateAsync(weakest);

            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.DivineConflict,
                SourceGodId = weakest.Id,
                Description = $"Discontent with the dominant religion is rising — {weakest.Name} is gaining attention."
            });
        }

        return deltas;
    }

    // ─── Collapse Age Crisis ──────────────────────────────────

    private async Task<List<DeltaEvent>> GenerateCollapseEventAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var activeCivs = civs.Where(c => c.State != CivilizationState.Fallen).ToList();
        if (!activeCivs.Any()) return deltas;

        var target = activeCivs.OrderBy(c => c.Stability).First(); // weakest first

        if (target.Stability < 20f)
        {
            target.State = CivilizationState.Collapsing;
            await _civRepo.UpdateAsync(target);
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.CivilizationCollapsed,
                TargetId = target.Id,
                Description = $"💔 {target.Name} is collapsing! People scatter. This is the last chance to save your believers."
            });
        }

        return deltas;
    }

    public async Task<WorldAge> GetCurrentAgeAsync(string worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        return world?.CurrentAge ?? WorldAge.EarlyAge;
    }
}
