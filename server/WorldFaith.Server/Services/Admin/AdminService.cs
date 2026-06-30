using BCrypt.Net;
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
    public int Width { get; set; }
    public int Height { get; set; }
    public int Seed { get; set; }
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
    public bool IsAdmin { get; set; }
    public string? BanReason { get; set; }
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
    Task<WorldAdminDto> CreateWorldAsync(CreateWorldAdminRequest req);
    Task<List<PlayerAdminDto>> GetPlayersAsync(int page, int pageSize, string? search = null);
    Task<PlayerAdminDto?> GetPlayerByIdAsync(string playerId);
    Task<bool> BanPlayerAsync(string playerId, string reason);
    Task<bool> UnbanPlayerAsync(string playerId);
    Task<bool> PromotePlayerAsync(string playerId);
    Task<bool> DemotePlayerAsync(string playerId);
    Task<bool> ResetPasswordAsync(string playerId, string newPassword);
    Task<Dictionary<string, object>> GetWorldSnapshotAsync(string worldId);

    // Map operations
    Task<object?> GetMapTilesAsync(string worldId);
    Task<bool> UpdateTileAsync(string worldId, int x, int y, UpdateTileRequest req);
    Task<bool> PlaceSacredAsync(string worldId, int x, int y);
    Task<int> RegenerateMapAsync(string worldId, int? seed);
}

public class UpdateTileRequest
{
    public string? Type { get; set; }
    public float? Fertility { get; set; }
    public bool? HasTemple { get; set; }
}

public class CreateWorldAdminRequest
{
    public string Name { get; set; } = "Admin World";
    public string? Mode { get; set; }
    public string? VictoryCondition { get; set; }
    public string ScenarioType { get; set; } = "Standard";
    public int Width { get; set; } = 128;
    public int Height { get; set; } = 128;
    public int Seed { get; set; }
    public int MaxGods { get; set; } = 4;
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
    private readonly Services.WorldGen.IWorldGeneratorService _worldGen;
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
        Services.WorldGen.IWorldGeneratorService worldGen,
        ILogger<AdminService> logger)
    {
        _worldRepo = worldRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _entityRepo = entityRepo;
        _playerRepo = playerRepo;
        _roomRepo = roomRepo;
        _worldGen = worldGen;
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
                Width = w.Width,
                Height = w.Height,
                Seed = w.Seed,
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

    public async Task<WorldAdminDto> CreateWorldAsync(CreateWorldAdminRequest req)
    {
        if (!Enum.TryParse<GameMode>(req.Mode, out var mode)) mode = GameMode.Sandbox;
        if (!Enum.TryParse<VictoryCondition>(req.VictoryCondition, out var victory))
            victory = VictoryCondition.LastSurvivingGod;

        var world = new WorldDocument
        {
            Name = req.Name,
            Mode = mode,
            MaxGods = req.MaxGods,
            Width = req.Width,
            Height = req.Height,
            VictoryCondition = victory,
            ScenarioType = req.ScenarioType,
            IsActive = true,
        };
        await _worldRepo.CreateAsync(world);

        var usedSeed = await _worldGen.GenerateAsync(world.Id, world.Width, world.Height, req.Seed);
        world.Seed = usedSeed;
        await _worldRepo.UpdateAsync(world);

        _logger.LogWarning("Admin created world {WorldId} ({Name}) directly, seed={Seed}", world.Id, world.Name, usedSeed);

        return new WorldAdminDto
        {
            Id = world.Id,
            Name = world.Name,
            Mode = world.Mode.ToString(),
            Tick = world.Tick,
            Cycle = world.Cycle,
            Width = world.Width,
            Height = world.Height,
            Seed = world.Seed,
            IsActive = world.IsActive,
            CreatedAt = world.CreatedAt,
        };
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
            IsAdmin = p.IsAdmin,
            BanReason = p.BanReason,
            LastLoginAt = p.LastLoginAt,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<PlayerAdminDto?> GetPlayerByIdAsync(string playerId)
    {
        var p = await _playerRepo.GetByIdAsync(playerId);
        if (p == null) return null;

        return new PlayerAdminDto
        {
            Id = p.Id,
            Username = p.Username,
            DisplayName = p.DisplayName,
            Email = p.Email,
            Level = p.Level,
            TotalGames = p.TotalGames,
            TotalWins = p.TotalWins,
            IsActive = p.IsActive,
            IsAdmin = p.IsAdmin,
            BanReason = p.BanReason,
            LastLoginAt = p.LastLoginAt,
            CreatedAt = p.CreatedAt
        };
    }

    public async Task<bool> BanPlayerAsync(string playerId, string reason)
    {
        await _playerRepo.SetActiveAsync(playerId, false);
        await _playerRepo.SetBanReasonAsync(playerId, reason);
        await _playerRepo.RevokeAllRefreshTokensAsync(playerId);
        _logger.LogWarning("Admin banned player {PlayerId}: {Reason}", playerId, reason);
        return true;
    }

    public async Task<bool> UnbanPlayerAsync(string playerId)
    {
        await _playerRepo.SetActiveAsync(playerId, true);
        await _playerRepo.SetBanReasonAsync(playerId, null);
        _logger.LogInformation("Admin unbanned player {PlayerId}", playerId);
        return true;
    }

    public async Task<bool> PromotePlayerAsync(string playerId)
    {
        await _playerRepo.SetAdminAsync(playerId, true);
        _logger.LogWarning("Admin promoted player {PlayerId} to Admin", playerId);
        return true;
    }

    public async Task<bool> DemotePlayerAsync(string playerId)
    {
        await _playerRepo.SetAdminAsync(playerId, false);
        _logger.LogWarning("Admin demoted player {PlayerId} from Admin", playerId);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string playerId, string newPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _playerRepo.SetPasswordHashAsync(playerId, hash);
        await _playerRepo.RevokeAllRefreshTokensAsync(playerId);
        _logger.LogWarning("Admin reset password for player {PlayerId}", playerId);
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

    // ─── Map Operations ───────────────────────────────────────

    public async Task<object?> GetMapTilesAsync(string worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return null;

        return new
        {
            width = world.Width,
            height = world.Height,
            seed = world.Seed,
            tiles = world.Tiles.Select(t => new
            {
                x = t.X,
                y = t.Y,
                type = t.Type.ToString(),
                fertility = t.Fertility,
                civilizationId = t.CivilizationId,
                religionId = t.ReligionId,
                hasTemple = t.HasTemple,
                population = t.Population,
                elevation = t.Elevation,
                moisture = t.Moisture,
                isCoast = t.IsCoast,
            })
        };
    }

    public async Task<bool> UpdateTileAsync(string worldId, int x, int y, UpdateTileRequest req)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return false;

        var tile = world.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);
        if (tile == null) return false;

        if (req.Type != null && Enum.TryParse<TileType>(req.Type, out var parsedType))
            tile.Type = parsedType;
        if (req.Fertility.HasValue)
            tile.Fertility = Math.Clamp(req.Fertility.Value, 0f, 1f);
        if (req.HasTemple.HasValue)
            tile.HasTemple = req.HasTemple.Value;

        await _worldRepo.UpdateTilesAsync(worldId, world.Tiles);
        _logger.LogInformation("Admin updated tile ({X},{Y}) in world {WorldId}", x, y, worldId);
        return true;
    }

    public async Task<bool> PlaceSacredAsync(string worldId, int x, int y)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return false;

        var tile = world.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);
        if (tile == null) return false;

        tile.Type = TileType.Sacred;
        tile.Fertility = 1f;

        await _worldRepo.UpdateTilesAsync(worldId, world.Tiles);
        _logger.LogInformation("Admin placed Sacred site at ({X},{Y}) in world {WorldId}", x, y, worldId);
        return true;
    }

    public async Task<int> RegenerateMapAsync(string worldId, int? seed)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world == null) return 0;

        var usedSeed = await _worldGen.GenerateAsync(worldId, world.Width, world.Height, seed ?? 0);

        world.Seed = usedSeed;
        await _worldRepo.UpdateAsync(world);

        _logger.LogWarning("Admin regenerated map for world {WorldId} with seed {Seed}", worldId, usedSeed);
        return usedSeed;
    }
}
