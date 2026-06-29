using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Chat;
using WorldFaith.Server.Services.Dungeon;
using WorldFaith.Server.Services.Evolution;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Leaderboard;
using WorldFaith.Server.Services.Memory;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Server.Services.Organization;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.Religion;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Services.Simulation;

public class WorldSimulationLoop : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<WorldHub, IWorldHubClient> _hubContext;
    private readonly ILogger<WorldSimulationLoop> _logger;

    public WorldSimulationLoop(
        IServiceScopeFactory scopeFactory,
        IHubContext<WorldHub, IWorldHubClient> hubContext,
        ILogger<WorldSimulationLoop> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorldSimulationLoop khởi động");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Đọc tick interval từ balance config mỗi lần để hỗ trợ live tuning
                using var configScope = _scopeFactory.CreateScope();
                var balance = configScope.ServiceProvider.GetRequiredService<IBalanceConfigService>();
                int tickMs = await balance.GetIntAsync("faith.tick_interval_ms");
                if (tickMs < 100) tickMs = 500; // safety floor

                await TickAllWorldsAsync(balance);
                await Task.Delay(tickMs, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong simulation tick");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task TickAllWorldsAsync(IBalanceConfigService balance)
    {
        using var scope = _scopeFactory.CreateScope();
        var worldRepo = scope.ServiceProvider.GetRequiredService<IWorldRepository>();
        var godRepo = scope.ServiceProvider.GetRequiredService<IGodRepository>();
        var civSim = scope.ServiceProvider.GetRequiredService<ICivilizationSimulationService>();
        var faithService = scope.ServiceProvider.GetRequiredService<IFaithService>();

        var activeWorlds = await worldRepo.GetActiveWorldsAsync();
        int rebirthInterval = await balance.GetIntAsync("world.rebirth_tick_interval");
        if (rebirthInterval < 10) rebirthInterval = 1000;

        foreach (var world in activeWorlds)
        {
            long newTick = world.Tick + 1;
            int cycle = world.Cycle;

            await faithService.GenerateFaithTickAsync(world.Id);
            var civUpdates = await civSim.TickAsync(world.Id, newTick);

            if (newTick % 5 == 0)
            {
                var religionService = scope.ServiceProvider.GetRequiredService<IReligionService>();
                var religionUpdates = await religionService.TickAsync(world.Id, newTick);
                foreach (var ru in religionUpdates)
                    await _hubContext.Clients.Group(world.Id).OnReligionUpdate(ru);
            }

            if (newTick % 3 == 0)
            {
                var evolutionService = scope.ServiceProvider.GetRequiredService<IEvolutionService>();
                var evoDeltas = await evolutionService.TickAsync(world.Id, newTick);
                if (evoDeltas.Any())
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = evoDeltas });
            }

            // AchievementService — passive talent/achievement checks (every 30 ticks)
            if (newTick % 30 == 0)
            {
                var achievSvc = scope.ServiceProvider.GetRequiredService<IAchievementService>();
                await achievSvc.TickAchievementSystemAsync(world.Id, newTick);

                // v1.2: Escort tick — protection, kidnap attempts
                var escortSvc = scope.ServiceProvider.GetRequiredService<IEscortService>();
                var escortDeltas = await escortSvc.TickEscortsAsync(world.Id, newTick);
                if (escortDeltas.Any())
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = escortDeltas });
            }

            // NPCInteractionService: crime, accidents, social events (every 10 ticks)
            if (newTick % 10 == 0)
            {
                var npcService = scope.ServiceProvider.GetRequiredService<INpcInteractionService>();
                var npcEvents = await npcService.TickAsync(world.Id, newTick);
                if (npcEvents.Any())
                {
                    var npcDeltas = npcEvents.Select(e => new DeltaEvent
                    {
                        Type = WorldEventType.DivineConflict,
                        TargetId = e.CivilizationId,
                        Description = e.Description
                    }).ToList();
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = npcDeltas });
                }
            }

            // OrganizationService: Noble Houses, Court, Guild, Underground (every 20 ticks)
            if (newTick % 20 == 0)
            {
                var orgService = scope.ServiceProvider.GetRequiredService<IOrganizationService>();
                var orgDeltas = await orgService.TickAsync(world.Id, newTick);
                if (orgDeltas.Any())
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = orgDeltas });
            }

            // AI Director — pacing, age transitions, crisis (every 20 ticks)
            if (newTick % 20 == 0)
            {
                var aiDirector = scope.ServiceProvider.GetRequiredService<IAiDirectorService>();
                var directorDeltas = await aiDirector.TickAsync(world.Id, newTick);
                if (directorDeltas.Any())
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = directorDeltas });
            }

            // DungeonService tick (every 50 ticks)
            if (newTick % 50 == 0)
            {
                var dungeonService = scope.ServiceProvider.GetRequiredService<IDungeonService>();
                var dungeonDeltas = await dungeonService.TickAsync(world.Id, newTick);
                if (dungeonDeltas.Any())
                    await _hubContext.Clients.Group(world.Id).OnWorldTick(
                        new WorldTickEvent { Tick = newTick, Cycle = cycle, Deltas = dungeonDeltas });
            }

            // MemoryService tick — relic faith gen (every 10 ticks)
            if (newTick % 10 == 0)
            {
                var memoryService = scope.ServiceProvider.GetRequiredService<IMemoryService>();
                await memoryService.TickAsync(world.Id, newTick);
            }

            // GodRankService — check ranks every 100 ticks
            if (newTick % 100 == 0)
            {
                var rankService = scope.ServiceProvider.GetRequiredService<IGodRankService>();
                var gods2 = await godRepo.GetByWorldAsync(world.Id);
                foreach (var g in gods2)
                {
                    await rankService.UpdateRankAsync(g.Id);
                    await rankService.CheckForgottenStateAsync(world.Id, g.Id);
                }
            }

            bool isRebirth = newTick % rebirthInterval == 0;
            if (isRebirth)
            {
                cycle++;
                await HandleRebirthAsync(scope, world.Id, cycle);
            }

            // Check win condition (nếu có scenario)
            var scenarioCtrl = scope.ServiceProvider.GetRequiredService<IScenarioController>();
            // Load scenario config từ world document
            if (!Enum.TryParse<ScenarioType>(world.ScenarioType, out var scenarioType))
                scenarioType = ScenarioType.Standard;
            var scenarioCfg = ScenarioConfigs.All.TryGetValue(scenarioType, out var cfg)
                ? cfg : ScenarioConfigs.All[ScenarioType.Standard];
            var (ended, winnerId, reason) = await scenarioCtrl.CheckWinConditionAsync(
                world.Id, newTick, cycle, scenarioCfg);

            if (ended)
            {
                await worldRepo.DeactivateAsync(world.Id);
                var gods2 = await godRepo.GetByWorldAsync(world.Id);
                var rankings = gods2
                    .OrderByDescending(g => g.FollowerCount)
                    .Select((g, i) => (g.PlayerId, Rank: i + 1))
                    .ToDictionary(x => x.PlayerId, x => x.Rank);

                await _hubContext.Clients.Group(world.Id).OnGameEnd(new GameEndEvent
                {
                    Condition       = VictoryCondition.LastSurvivingGod,
                    WinnerGodId     = gods2.FirstOrDefault(g => g.PlayerId == winnerId)?.Id,
                    FinalRankings   = rankings
                });
                _logger.LogInformation("Game ended: {WorldId} — {Reason}", world.Id, reason);
                continue;
            }

            await scenarioCtrl.ApplyScenarioEffectsAsync(world.Id, scenarioCfg, newTick);
            await worldRepo.UpdateTickAsync(world.Id, newTick, cycle);

            // Broadcast tick event đến tất cả clients trong world group
            var tickEvent = new WorldTickEvent
            {
                Tick = newTick,
                Cycle = cycle,
                Deltas = civUpdates.Select(u => new DeltaEvent
                {
                    Type = u.Collapsed ? WorldEventType.CivilizationCollapsed : WorldEventType.MiraclePerformed,
                    TargetId = u.CivilizationId,
                    Description = u.Collapsed
                        ? $"{u.Name} has collapsed"
                        : $"{u.Name}: population {u.Population}"
                }).ToList()
            };

            await _hubContext.Clients.Group(world.Id).OnWorldTick(tickEvent);

            // Broadcast god updates
            var gods = await godRepo.GetByWorldAsync(world.Id);
            foreach (var god in gods)
            {
                var godUpdate = new GodUpdateEvent
                {
                    GodId = god.Id,
                    Faith = god.Faith,
                    Trust = god.Trust,
                    Fear = god.Fear,
                    FollowerCount = god.FollowerCount,
                    IsAlive = god.IsAlive
                };
                await _hubContext.Clients.Group(world.Id).OnGodUpdate(godUpdate);
            }
        }
    }

    private async Task HandleRebirthAsync(IServiceScope scope, string worldId, int newCycle)
    {
        var godRepo = scope.ServiceProvider.GetRequiredService<IGodRepository>();
        var civRepo = scope.ServiceProvider.GetRequiredService<ICivilizationRepository>();
        var religionRepo = scope.ServiceProvider.GetRequiredService<IReligionRepository>();

        var gods = await godRepo.GetByWorldAsync(worldId);
        var survivedIds = new List<string>();
        var fadedIds = new List<string>();

        foreach (var god in gods)
        {
            if (god.FollowerCount > 0)
            {
                survivedIds.Add(god.Id);
                // Reset faith một phần, giữ lại followers
                await godRepo.UpdateFaithAsync(god.Id, 100f, god.Trust * 0.8f, god.Fear * 0.5f, god.FollowerCount / 2);
            }
            else
            {
                fadedIds.Add(god.Id);
                await godRepo.KillGodAsync(god.Id);
            }
        }

        // Delete civilizations cũ, chuẩn was world mới
        await civRepo.DeleteByWorldAsync(worldId);

        // Delete evolution entities cũ
        var entityRepo = scope.ServiceProvider.GetRequiredService<IEvolutionEntityRepository>();
        await entityRepo.DeleteByWorldAsync(worldId);

        // Record leaderboard
        var leaderboard = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
        var rankings = new Dictionary<string, int>();
        int rank = 1;
        foreach (var godId in survivedIds)
        {
            var god = await godRepo.GetByIdAsync(godId);
            if (god?.PlayerId != null) rankings[god.PlayerId] = rank++;
        }
        foreach (var godId in fadedIds)
        {
            var god = await godRepo.GetByIdAsync(godId);
            if (god?.PlayerId != null) rankings[god.PlayerId] = rank++;
        }
        await leaderboard.RecordGameEndAsync(new WorldEndResultDto
        {
            WorldId = worldId,
            WinnerPlayerId = survivedIds.Any()
                ? (await godRepo.GetByIdAsync(survivedIds[0]))?.PlayerId ?? ""
                : "",
            VictoryCondition = "Rebirth",
            PlayerRankings = rankings,
            TotalCycles = newCycle
        });

        // Broadcast chat thông báo rebirth
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        await chatService.BroadcastSystemAsync(worldId,
            $"🌍 Thế giới bước sang cycles {newCycle}. {fadedIds.Count} thần has biến mất mãi mãi.");

        // Spawn civilizations mới
        var civSim = scope.ServiceProvider.GetRequiredService<ICivilizationSimulationService>();
        await civSim.SpawnInitialCivilizationsAsync(worldId, 6);

        var rebirthEvent = new WorldRebirthEvent
        {
            NewCycle = newCycle,
            SurvivedGodIds = survivedIds,
            FadedGodIds = fadedIds
        };

        await _hubContext.Clients.Group(worldId).OnWorldRebirth(rebirthEvent);
        _logger.LogInformation("World {WorldId} rebirth cycle {Cycle}. Survived: {S}, Faded: {F}",
            worldId, newCycle, survivedIds.Count, fadedIds.Count);
    }
}
