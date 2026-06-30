using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace WorldFaith.Server.Services.Leaderboard;

// ─── Document ────────────────────────────────────────────
public class PlayerStatsDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Game stats
    public int TotalGames { get; set; }
    public int TotalWins { get; set; }
    public int TotalCycles { get; set; }       // Tổng số cycles sống sót
    public long TotalFollowers { get; set; }    // Tổng followers accumulated
    public int ReligionsErased { get; set; }    // Số religion đối thủ was xóa
    public int MiraclesPerformed { get; set; }
    public int MiraclesCountered { get; set; }
    public int EntitiesEvolved { get; set; }
    public int CrusadesTriggered { get; set; }
    public int SchismsTriggered { get; set; }

    // Rating (ELO-style)
    public int Rating { get; set; } = 1000;
    public int PeakRating { get; set; } = 1000;

    // Preferred archetype
    public string FavoriteArchetype { get; set; } = string.Empty;
    public Dictionary<string, int> ArchetypeWins { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ─── DTOs ────────────────────────────────────────────────
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }
    public float WinRate { get; set; }
    public string FavoriteArchetype { get; set; } = string.Empty;
    public long TotalFollowers { get; set; }
}

public class PlayerDetailStatsDto : LeaderboardEntryDto
{
    public int PeakRating { get; set; }
    public int TotalCycles { get; set; }
    public int MiraclesPerformed { get; set; }
    public int MiraclesCountered { get; set; }
    public int EntitiesEvolved { get; set; }
    public int CrusadesTriggered { get; set; }
    public int SchismsTriggered { get; set; }
    public int ReligionsErased { get; set; }
    public Dictionary<string, int> ArchetypeWins { get; set; } = new();
}

public class WorldEndResultDto
{
    public string WorldId { get; set; } = string.Empty;
    public string WinnerPlayerId { get; set; } = string.Empty;
    public string WinnerGodName { get; set; } = string.Empty;
    public string VictoryCondition { get; set; } = string.Empty;
    public Dictionary<string, int> PlayerRankings { get; set; } = new(); // playerId → rank
    public Dictionary<string, long> FollowerCounts { get; set; } = new();
    public int TotalCycles { get; set; }
}

// ─── Service ─────────────────────────────────────────────
public interface ILeaderboardService
{
    Task RecordGameEndAsync(WorldEndResultDto result);
    Task RecordMiracleAsync(string playerId, bool wasCountered);
    Task RecordEvolutionAsync(string playerId);
    Task RecordCrusadeAsync(string playerId);
    Task RecordSchismAsync(string playerId);
    Task<List<LeaderboardEntryDto>> GetTopPlayersAsync(int limit = 50);
    Task<PlayerDetailStatsDto?> GetPlayerStatsAsync(string playerId);
    Task<List<LeaderboardEntryDto>> GetLeaderboardByStatAsync(string stat, int limit = 20);
    Task ResetAllAsync();
}

public class LeaderboardService : ILeaderboardService
{
    private readonly IMongoCollection<PlayerStatsDocument> _collection;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LeaderboardService> _logger;

    private const string LeaderboardKey = "worldfaith:leaderboard:rating";
    private const string LeaderboardFollowersKey = "worldfaith:leaderboard:followers";
    private const string LeaderboardWinsKey = "worldfaith:leaderboard:wins";

    public LeaderboardService(IMongoDatabase db, IConnectionMultiplexer redis, ILogger<LeaderboardService> logger)
    {
        _collection = db.GetCollection<PlayerStatsDocument>("player_stats");
        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerStatsDocument>(
                Builders<PlayerStatsDocument>.IndexKeys.Ascending(s => s.PlayerId),
                new CreateIndexOptions { Unique = true }));
        _redis = redis;
        _logger = logger;
    }

    // ─── Record Events ───────────────────────────────────────

    public async Task RecordGameEndAsync(WorldEndResultDto result)
    {
        var db = _redis.GetDatabase();
        int playerCount = result.PlayerRankings.Count;

        foreach (var (playerId, rank) in result.PlayerRankings)
        {
            bool won = rank == 1;
            var stats = await GetOrCreateStatsAsync(playerId);

            // Update basic stats
            stats.TotalGames++;
            if (won) stats.TotalWins++;
            stats.TotalCycles += result.TotalCycles;

            if (result.FollowerCounts.TryGetValue(playerId, out var followers))
                stats.TotalFollowers += followers;

            // ELO rating update
            int ratingChange = CalculateRatingChange(stats.Rating, rank, playerCount);
            stats.Rating = Math.Max(100, stats.Rating + ratingChange);
            if (stats.Rating > stats.PeakRating)
                stats.PeakRating = stats.Rating;

            await UpsertStatsAsync(stats);

            // Redis sorted sets for leaderboard
            await db.SortedSetAddAsync(LeaderboardKey, playerId, stats.Rating);
            await db.SortedSetAddAsync(LeaderboardWinsKey, playerId, stats.TotalWins);
            await db.SortedSetAddAsync(LeaderboardFollowersKey, playerId, stats.TotalFollowers);
        }

        _logger.LogInformation("Game end recorded: World {WorldId}, Winner {Winner}",
            result.WorldId, result.WinnerPlayerId);
    }

    public async Task RecordMiracleAsync(string playerId, bool wasCountered)
    {
        var update = Builders<PlayerStatsDocument>.Update
            .Inc(s => s.MiraclesPerformed, 1)
            .Inc(s => s.MiraclesCountered, wasCountered ? 1 : 0)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(s => s.PlayerId == playerId, update);
    }

    public async Task RecordEvolutionAsync(string playerId)
    {
        var update = Builders<PlayerStatsDocument>.Update
            .Inc(s => s.EntitiesEvolved, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(s => s.PlayerId == playerId, update);
    }

    public async Task RecordCrusadeAsync(string playerId)
    {
        var update = Builders<PlayerStatsDocument>.Update
            .Inc(s => s.CrusadesTriggered, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(s => s.PlayerId == playerId, update);
    }

    public async Task RecordSchismAsync(string playerId)
    {
        var update = Builders<PlayerStatsDocument>.Update
            .Inc(s => s.SchismsTriggered, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(s => s.PlayerId == playerId, update);
    }

    // ─── Read Leaderboard ────────────────────────────────────

    public async Task<List<LeaderboardEntryDto>> GetTopPlayersAsync(int limit = 50)
    {
        var db = _redis.GetDatabase();

        // Get từ Redis sorted set (nhanh)
        var entries = await db.SortedSetRangeByRankWithScoresAsync(
            LeaderboardKey, 0, limit - 1, Order.Descending);

        if (entries.Length == 0)
        {
            // Fallback: query MongoDB
            return await GetFromMongoAsync(limit);
        }

        var result = new List<LeaderboardEntryDto>();
        int rank = 1;

        foreach (var entry in entries)
        {
            var playerId = entry.Element.ToString();
            var stats = await _collection.Find(s => s.PlayerId == playerId).FirstOrDefaultAsync();
            if (stats == null) continue;

            result.Add(MapToEntry(stats, rank++));
        }

        return result;
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardByStatAsync(string stat, int limit = 20)
    {
        var db = _redis.GetDatabase();
        string key = stat switch
        {
            "wins"      => LeaderboardWinsKey,
            "followers" => LeaderboardFollowersKey,
            _           => LeaderboardKey
        };

        var entries = await db.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1, Order.Descending);
        var result = new List<LeaderboardEntryDto>();
        int rank = 1;

        foreach (var entry in entries)
        {
            var playerId = entry.Element.ToString();
            var stats = await _collection.Find(s => s.PlayerId == playerId).FirstOrDefaultAsync();
            if (stats == null) continue;
            result.Add(MapToEntry(stats, rank++));
        }

        return result;
    }

    public async Task<PlayerDetailStatsDto?> GetPlayerStatsAsync(string playerId)
    {
        var stats = await _collection.Find(s => s.PlayerId == playerId).FirstOrDefaultAsync();
        if (stats == null) return null;

        // Tính rank hiện tại
        var db = _redis.GetDatabase();
        var rank = await db.SortedSetRankAsync(LeaderboardKey, playerId, Order.Descending);

        return new PlayerDetailStatsDto
        {
            Rank = (int)(rank ?? 999) + 1,
            PlayerId = stats.PlayerId,
            DisplayName = stats.DisplayName,
            Rating = stats.Rating,
            PeakRating = stats.PeakRating,
            TotalWins = stats.TotalWins,
            TotalGames = stats.TotalGames,
            WinRate = stats.TotalGames > 0 ? (float)stats.TotalWins / stats.TotalGames : 0f,
            FavoriteArchetype = stats.FavoriteArchetype,
            TotalFollowers = stats.TotalFollowers,
            TotalCycles = stats.TotalCycles,
            MiraclesPerformed = stats.MiraclesPerformed,
            MiraclesCountered = stats.MiraclesCountered,
            EntitiesEvolved = stats.EntitiesEvolved,
            CrusadesTriggered = stats.CrusadesTriggered,
            SchismsTriggered = stats.SchismsTriggered,
            ReligionsErased = stats.ReligionsErased,
            ArchetypeWins = stats.ArchetypeWins
        };
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static int CalculateRatingChange(int currentRating, int rank, int totalPlayers)
    {
        // ELO đơn giản: top half thắng, bottom half thua
        float expected = 1f / totalPlayers;
        float actual = rank == 1 ? 1f : rank <= totalPlayers / 2 ? 0.5f : 0f;
        return (int)(32 * (actual - expected));
    }

    private async Task<PlayerStatsDocument> GetOrCreateStatsAsync(string playerId)
    {
        var existing = await _collection.Find(s => s.PlayerId == playerId).FirstOrDefaultAsync();
        if (existing != null) return existing;

        var newStats = new PlayerStatsDocument { PlayerId = playerId, DisplayName = playerId };
        await _collection.InsertOneAsync(newStats);
        return newStats;
    }

    private async Task UpsertStatsAsync(PlayerStatsDocument stats)
    {
        stats.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(
            s => s.PlayerId == stats.PlayerId, stats,
            new ReplaceOptions { IsUpsert = true });
    }

    private async Task<List<LeaderboardEntryDto>> GetFromMongoAsync(int limit)
    {
        var stats = await _collection.Find(_ => true)
            .SortByDescending(s => s.Rating)
            .Limit(limit)
            .ToListAsync();

        return stats.Select((s, i) => MapToEntry(s, i + 1)).ToList();
    }

    private static LeaderboardEntryDto MapToEntry(PlayerStatsDocument s, int rank) => new()
    {
        Rank = rank,
        PlayerId = s.PlayerId,
        DisplayName = s.DisplayName,
        Rating = s.Rating,
        TotalWins = s.TotalWins,
        TotalGames = s.TotalGames,
        WinRate = s.TotalGames > 0 ? (float)s.TotalWins / s.TotalGames : 0f,
        FavoriteArchetype = s.FavoriteArchetype,
        TotalFollowers = s.TotalFollowers
    };

    public async Task ResetAllAsync()
    {
        // Wipe the Mongo source of truth for all player stats...
        await _collection.DeleteManyAsync(Builders<PlayerStatsDocument>.Filter.Empty);

        // ...and the three Redis sorted sets that back the fast leaderboard reads.
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(LeaderboardKey);
        await db.KeyDeleteAsync(LeaderboardWinsKey);
        await db.KeyDeleteAsync(LeaderboardFollowersKey);

        _logger.LogWarning("Admin reset the entire leaderboard (Mongo + Redis cleared)");
    }
}
