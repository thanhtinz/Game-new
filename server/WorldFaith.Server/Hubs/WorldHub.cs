using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Simulation;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Hubs;

// Interface cho client methods (strongly-typed hub)
public interface IWorldHubClient
{
    Task OnWorldTick(WorldTickEvent evt);
    Task OnMiracleResult(MiracleResultEvent evt);
    Task OnGodUpdate(GodUpdateEvent evt);
    Task OnCivilizationUpdate(CivilizationUpdateEvent evt);
    Task OnReligionUpdate(ReligionUpdateEvent evt);
    Task OnWorldRebirth(WorldRebirthEvent evt);
    Task OnGameEnd(GameEndEvent evt);
    Task OnError(ErrorEvent evt);
    Task OnWorldState(WorldStateDto state);
    Task OnJoinedWorld(GodDto god);
}

[Authorize]
public class WorldHub : Hub<IWorldHubClient>
{
    private string PlayerId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value
        ?? Context.ConnectionId;

    private readonly IWorldRepository _worldRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IMiracleService _miracleService;
    private readonly ICivilizationSimulationService _civSim;
    private readonly ILogger<WorldHub> _logger;

    // ConnectionId -> GodId mapping (in-memory, dùng Redis nếu scale)
    private static readonly Dictionary<string, string> ConnectionGodMap = new();
    private static readonly Dictionary<string, string> ConnectionWorldMap = new();

    public WorldHub(
        IWorldRepository worldRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IMiracleService miracleService,
        ICivilizationSimulationService civSim,
        ILogger<WorldHub> logger)
    {
        _worldRepo = worldRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _miracleService = miracleService;
        _civSim = civSim;
        _logger = logger;
    }

    // ─── Client → Server Methods ────────────────────────────

    public async Task JoinWorld(JoinWorldRequest req)
    {
        var world = await _worldRepo.GetByIdAsync(req.WorldId);
        if (world == null)
        {
            await Clients.Caller.OnError(new ErrorEvent { Code = "WORLD_NOT_FOUND", Message = "World không tồn tại" });
            return;
        }

        var playerId = PlayerId;

        // Kiểm tra god đã tồn tại chưa
        var existingGod = await _godRepo.GetByPlayerAndWorldAsync(playerId, req.WorldId);
        GodDocument god;

        if (existingGod != null)
        {
            god = existingGod;
        }
        else
        {
            // Kiểm tra số lượng god
            var currentGods = await _godRepo.GetByWorldAsync(req.WorldId);
            if (currentGods.Count >= world.MaxGods)
            {
                await Clients.Caller.OnError(new ErrorEvent { Code = "WORLD_FULL", Message = "World đã đầy god" });
                return;
            }

            god = new GodDocument
            {
                WorldId = req.WorldId,
                PlayerId = playerId,
                Name = req.GodName,
                Archetype = req.Archetype
            };
            await _godRepo.CreateAsync(god);
        }

        ConnectionGodMap[Context.ConnectionId] = god.Id;
        ConnectionWorldMap[Context.ConnectionId] = req.WorldId;

        await Groups.AddToGroupAsync(Context.ConnectionId, req.WorldId);

        // Gửi world state hiện tại cho client
        var state = await BuildWorldStateAsync(req.WorldId);
        await Clients.Caller.OnWorldState(state);
        await Clients.Caller.OnJoinedWorld(MapGodToDto(god));

        _logger.LogInformation("God {GodName} ({GodId}) tham gia world {WorldId}", god.Name, god.Id, req.WorldId);
    }

    public async Task PerformMiracle(PerformMiracleRequest req)
    {
        if (!ConnectionGodMap.TryGetValue(Context.ConnectionId, out var godId))
        {
            await Clients.Caller.OnError(new ErrorEvent { Code = "NOT_IN_WORLD", Message = "Chưa join world" });
            return;
        }

        ConnectionWorldMap.TryGetValue(Context.ConnectionId, out var worldId);

        var result = await _miracleService.PerformMiracleAsync(
            worldId!, godId, req.Miracle, req.TargetX, req.TargetY, req.TargetCivilizationId);

        // Broadcast kết quả miracle đến toàn bộ world
        await Clients.Group(worldId!).OnMiracleResult(result);
    }

    public async Task CounterMiracle(CounterMiracleRequest req)
    {
        if (!ConnectionGodMap.TryGetValue(Context.ConnectionId, out var godId)) return;
        ConnectionWorldMap.TryGetValue(Context.ConnectionId, out var worldId);

        var result = await _miracleService.CounterMiracleAsync(worldId!, godId, req.MiracleEventId, req.CounterMiracle);
        if (result != null)
            await Clients.Group(worldId!).OnMiracleResult(result);
    }

    public async Task CreateWorld(CreateWorldRequest req)
    {
        var world = new WorldDocument
        {
            Name = req.WorldName,
            Mode = req.Mode,
            MaxGods = req.MaxGods,
            Width = req.WorldWidth,
            Height = req.WorldHeight,
            VictoryCondition = req.VictoryCondition,
            IsActive = true
        };
        await _worldRepo.CreateAsync(world);

        // Khởi tạo civilizations ban đầu
        await _civSim.SpawnInitialCivilizationsAsync(world.Id, 6);

        await Clients.Caller.OnJoinedWorld(new GodDto { });
        _logger.LogInformation("World mới được tạo: {WorldId} ({WorldName})", world.Id, world.Name);
    }

    public async Task RequestWorldState()
    {
        if (!ConnectionWorldMap.TryGetValue(Context.ConnectionId, out var worldId)) return;
        var state = await BuildWorldStateAsync(worldId);
        await Clients.Caller.OnWorldState(state);
    }

    // ─── Connection Lifecycle ───────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ConnectionGodMap.Remove(Context.ConnectionId);
        ConnectionWorldMap.Remove(Context.ConnectionId, out var worldId);
        if (worldId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, worldId);

        await base.OnDisconnectedAsync(exception);
    }

    // ─── Helpers ────────────────────────────────────────────

    private async Task<WorldStateDto> BuildWorldStateAsync(string worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);

        return new WorldStateDto
        {
            WorldId = worldId,
            Cycle = world?.Cycle ?? 0,
            Tick = world?.Tick ?? 0,
            Width = world?.Width ?? 64,
            Height = world?.Height ?? 64,
            Gods = gods.Select(MapGodToDto).ToList(),
            Civilizations = civs.Select(c => new CivilizationDto
            {
                Id = c.Id,
                Name = c.Name,
                Personality = c.Personality,
                Population = c.Population,
                Economy = c.Economy,
                Military = c.Military,
                RulingReligionId = c.RulingReligionId,
                ReligionIds = c.ReligionIds,
                ControlledTiles = c.ControlledTiles.Select(t => new int[] { t.X, t.Y }).ToList(),
                IsAtWar = c.IsAtWar,
                State = c.State
            }).ToList(),
            Religions = religions.Select(r => new ReligionDto
            {
                Id = r.Id,
                Name = r.Name,
                GodId = r.GodId,
                FollowerCount = r.FollowerCount,
                TempleCount = r.TempleCount,
                DevotionLevel = r.DevotionLevel,
                IsHidden = r.IsHidden,
                CivilizationIds = r.CivilizationIds,
                SchismIds = r.SchismIds
            }).ToList(),
            ChangedTiles = world?.Tiles.Select(t => new WorldTileDto
            {
                X = t.X,
                Y = t.Y,
                Type = t.Type,
                Fertility = t.Fertility,
                CivilizationId = t.CivilizationId,
                ReligionId = t.ReligionId,
                HasTemple = t.HasTemple,
                Population = t.Population
            }).ToList() ?? new()
        };
    }

    private static GodDto MapGodToDto(GodDocument g) => new()
    {
        Id = g.Id,
        PlayerId = g.PlayerId,
        Name = g.Name,
        Archetype = g.Archetype,
        Faith = g.Faith,
        Trust = g.Trust,
        Fear = g.Fear,
        FollowerCount = g.FollowerCount,
        UnlockedMiracles = g.UnlockedMiracles.Select(m => m.ToString()).ToList(),
        IsAlive = g.IsAlive,
        LastActionAt = new DateTimeOffset(g.LastActionAt).ToUnixTimeSeconds()
    };
}
