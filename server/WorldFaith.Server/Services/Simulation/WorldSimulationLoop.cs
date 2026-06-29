using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Services.Simulation;

public class WorldSimulationLoop : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<WorldHub, IWorldHubClient> _hubContext;
    private readonly ILogger<WorldSimulationLoop> _logger;

    // 1 tick = 500ms
    private const int TickIntervalMs = 500;
    // Rebirth cycle sau 1000 ticks
    private const int RebirthTickInterval = 1000;

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
                await TickAllWorldsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong simulation tick");
            }

            await Task.Delay(TickIntervalMs, stoppingToken);
        }
    }

    private async Task TickAllWorldsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var worldRepo = scope.ServiceProvider.GetRequiredService<IWorldRepository>();
        var godRepo = scope.ServiceProvider.GetRequiredService<IGodRepository>();
        var civSim = scope.ServiceProvider.GetRequiredService<ICivilizationSimulationService>();
        var faithService = scope.ServiceProvider.GetRequiredService<IFaithService>();

        var activeWorlds = await worldRepo.GetActiveWorldsAsync();

        foreach (var world in activeWorlds)
        {
            long newTick = world.Tick + 1;
            int cycle = world.Cycle;

            // Faith generation mỗi tick
            await faithService.GenerateFaithTickAsync(world.Id);

            // AI Civilization tick
            var civUpdates = await civSim.TickAsync(world.Id, newTick);

            // Kiểm tra rebirth cycle
            bool isRebirth = newTick % RebirthTickInterval == 0;
            if (isRebirth)
            {
                cycle++;
                await HandleRebirthAsync(scope, world.Id, cycle);
            }

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
                        ? $"{u.Name} đã sụp đổ"
                        : $"{u.Name}: dân số {u.Population}"
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

        // Xóa civilizations cũ, chuẩn bị world mới
        await civRepo.DeleteByWorldAsync(worldId);

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
