using WorldFaith.Server.Models;
using WorldFaith.Server.Models.Auth;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Admin;

// ─── DTOs ────────────────────────────────────────────────
public class ServerStatsDto
{
    public int ActiveWorlds { get; set; }
    public int TotalPlayers { get; set; }
    public int OnlinePlayers { get; set; }
    public int TotalRooms { get; set; }
    public int TotalGods { get; set; }
    public int TotalCivilizations { get; set; }
    public int TotalReligions { get; set; }
    public int TotalEvolutionEntities { get; set; }
    public long UptimeSeconds { get; set; }
    public DateTime ServerTime { get; set; }
}

public class WorldAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public long Tick { get; set; }
    public int Cycle { get; set; }
    public int GodCount { get; set; }
    public int CivCount { get; set; }
    public int ReligionCount { get; set; }
    public int EntityCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlayerAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public int TotalGames { get; set; }
    public int TotalWins { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Admin Service ───────────────────────────────────────
public interface IAdminService
{
    Task<ServerStatsDto> GetServerStatsAsync();
    Task<List<WorldAdminDto>> GetAllWorldsAsync();
    Task<bool> ForceEndWorldAsync(string worldId);
    Task<bool> ForceRebirthAsync(string worldId);
    Task<List<PlayerAdminDto>> GetPlayersAsync(int page, int pageSize, string? search = null);
    Task<bool> BanPlayerAsync(string playerId, string reason);
    Task<bool> UnbanPlayerAsync(string playerId);
    Task<Dictionary<string, object>> GetWorldSnapshotAsync(string worldId);
}

public class AdminService : IAdminService
{
    private readonly IWorldRepository _worldRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IEvolutionEntityRepository _entityRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly IRoomRepository _roomRepo;
    private readonly ILogger<AdminService> _logger;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public AdminService(
        IWorldRepository worldRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IEvolutionEntityRepository entityRepo,
        IPlayerRepository playerRepo,
        IRoomRepository roomRepo,
        ILogger<AdminService> logger)
    {
        _worldRepo = worldRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _entityRepo = entityRepo;
        _playerRepo = playerRepo;
        _roomRepo = roomRepo;
        _logger = logger;
    }

    public async Task<ServerStatsDto> GetServerStatsAsync()
    {
        var worlds = await _worldRepo.GetActiveWorldsAsync();
        var allPlayers = await _playerRepo.GetAllAsync();
        var rooms = await _roomRepo.GetPublicWaitingRoomsAsync();

        int totalGods = 0, totalCivs = 0, totalReligions = 0, totalEntities = 0;
        foreach (var w in worlds)
        {
            var gods = await _godRepo.GetByWorldAsync(w.Id);
            var civs = await _civRepo.GetByWorldAsync(w.Id);
            var religions = await _religionRepo.GetByWorldAsync(w.Id);
            var entities = await _entityRepo.GetByWorldAsync(w.Id);
            totalGods += gods.Count;
            totalCivs += civs.Count;
            totalReligions += religions.Count;
            totalEntities += entities.Count;
        }

        return new ServerStatsDto
        {
            ActiveWorlds = worlds.Count,
            TotalPlayers = allPlayers.Count,
            OnlinePlayers = rooms.Sum(r => r.Players.Count),
            TotalRooms = rooms.Count,
            TotalGods = totalGods,
            TotalCivilizations = totalCivs,
            TotalReligions = totalReligions,
            TotalEvolutionEntities = totalEntities,
            UptimeSeconds = (long)(DateTime.UtcNow - StartTime).TotalSeconds,
            ServerTime = DateTime.UtcNow
        };
    }

    public async Task<List<WorldAdminDto>> GetAllWorldsAsync()
    {
        var worlds = await _worldRepo.GetAllAsync();
        var result = new List<WorldAdminDto>();

        foreach (var w in worlds)
        {
            var gods = await _godRepo.GetByWorldAsync(w.Id);
            var civs = await _civRepo.GetByWorldAsync(w.Id);
            var religions = await _religionRepo.GetByWorldAsync(w.Id);
            var entities = await _entityRepo.GetByWorldAsync(w.Id);

            result.Add(new WorldAdminDto
            {
                Id = w.Id,
                Name = w.Name,
                Mode = w.Mode.ToString(),
                Tick = w.Tick,
                Cycle = w.Cycle,
                GodCount = gods.Count,
                CivCount = civs.Count,
                ReligionCount = religions.Count,
                EntityCount = entities.Count,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt
            });
        }

        return result;
    }

    public async Task<bool> ForceEndWorldAsync(string worldId)
    {
        await _worldRepo.DeactivateAsync(worldId);
        _logger.LogWarning("Admin force-ended world {WorldId}", worldId);
        return true;
    }

    public async Task<bool> ForceRebirthAsync(string worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return false;

        await _worldRepo.UpdateTickAsync(worldId, world.Tick, world.Cycle + 1);
        await _civRepo.DeleteByWorldAsync(worldId);
        await _entityRepo.DeleteByWorldAsync(worldId);

        _logger.LogWarning("Admin force-rebirth world {WorldId}", worldId);
        return true;
    }

    public async Task<List<PlayerAdminDto>> GetPlayersAsync(int page, int pageSize, string? search = null)
    {
        var players = await _playerRepo.GetAllAsync(page, pageSize, search);
        return players.Select(p => new PlayerAdminDto
        {
            Id = p.Id,
            Username = p.Username,
            DisplayName = p.DisplayName,
            Email = p.Email,
            Level = p.Level,
            TotalGames = p.TotalGames,
            TotalWins = p.TotalWins,
            IsActive = p.IsActive,
            LastLoginAt = p.LastLoginAt,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<bool> BanPlayerAsync(string playerId, string reason)
    {
        await _playerRepo.SetActiveAsync(playerId, false);
        _logger.LogWarning("Admin banned player {PlayerId}: {Reason}", playerId, reason);
        return true;
    }

    public async Task<bool> UnbanPlayerAsync(string playerId)
    {
        await _playerRepo.SetActiveAsync(playerId, true);
        _logger.LogInformation("Admin unbanned player {PlayerId}", playerId);
        return true;
    }

    public async Task<Dictionary<string, object>> GetWorldSnapshotAsync(string worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return new();

        var gods = await _godRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        var entities = await _entityRepo.GetByWorldAsync(worldId);

        return new Dictionary<string, object>
        {
            ["world"] = new { world.Id, world.Name, world.Tick, world.Cycle, world.IsActive },
            ["gods"] = gods.Select(g => new { g.Id, g.Name, g.Archetype, g.Faith, g.FollowerCount, g.IsAlive }),
            ["civilizations"] = civs.Select(c => new { c.Id, c.Name, c.Population, c.State, c.Personality }),
            ["religions"] = religions.Select(r => new { r.Id, r.Name, r.FollowerCount, r.TempleCount, r.DevotionLevel }),
            ["entities"] = entities.Select(e => new { e.Id, e.Name, e.Stage, e.Power, e.X, e.Y })
        };
    }
}
