using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorldFaith.Server.Models.Auth;

// ─── Player (Account) ───────────────────────────────────
public class PlayerDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Stats
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }

    // Refresh tokens (hỗ trợ multi-device)
    public List<RefreshTokenEntry> RefreshTokens { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}

public class RefreshTokenEntry
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
}

// ─── Room (Lobby) ────────────────────────────────────────
public class RoomDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;
    public string HostPlayerId { get; set; } = string.Empty;
    public string HostDisplayName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
    public string GameMode { get; set; } = "Sandbox";
    public string VictoryCondition { get; set; } = "LastSurvivingGod";
    public int WorldWidth { get; set; } = 64;
    public int WorldHeight { get; set; } = 64;
    public bool IsPrivate { get; set; }
    public string? PasswordHash { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public string? WorldId { get; set; }

    public List<RoomPlayerEntry> Players { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RoomPlayerEntry
{
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsHost { get; set; }
    public string? SelectedGodName { get; set; }
    public string? SelectedArchetype { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum RoomStatus
{
    Waiting,
    Starting,
    InGame,
    Finished,
    Disbanded
}
