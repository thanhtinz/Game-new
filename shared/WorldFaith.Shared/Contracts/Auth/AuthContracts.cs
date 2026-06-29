namespace WorldFaith.Shared.Contracts.Auth;

// ══════════════════════════════════════════════
//  AUTH REQUESTS
// ══════════════════════════════════════════════

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════
//  AUTH RESPONSES
// ══════════════════════════════════════════════

public class AuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long ExpiresAt { get; set; }
    public PlayerProfileDto? Player { get; set; }
    public string? Error { get; set; }
}

public class PlayerProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }
    public long CreatedAt { get; set; }
    public long LastLoginAt { get; set; }
}

// ══════════════════════════════════════════════
//  LOBBY CONTRACTS
// ══════════════════════════════════════════════

public class CreateRoomRequest
{
    public string RoomName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
    public string GameMode { get; set; } = "Sandbox";
    public string VictoryCondition { get; set; } = "LastSurvivingGod";
    public int WorldWidth { get; set; } = 64;
    public int WorldHeight { get; set; } = 64;
    public bool IsPrivate { get; set; }
    public string? Password { get; set; }
}

public class JoinRoomRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class RoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HostPlayerId { get; set; } = string.Empty;
    public string HostDisplayName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Waiting, Starting, InGame
    public bool IsPrivate { get; set; }
    public List<RoomPlayerDto> Players { get; set; } = new();
    public long CreatedAt { get; set; }
}

public class RoomPlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsHost { get; set; }
    public string? SelectedGodName { get; set; }
    public string? SelectedArchetype { get; set; }
}

public class LobbyListResponse
{
    public List<RoomDto> Rooms { get; set; } = new();
    public int TotalOnline { get; set; }
}

// ══════════════════════════════════════════════
//  LOBBY SIGNALR EVENTS
// ══════════════════════════════════════════════

public class RoomUpdatedEvent
{
    public RoomDto Room { get; set; } = new();
}

public class PlayerReadyEvent
{
    public string PlayerId { get; set; } = string.Empty;
    public bool IsReady { get; set; }
}

public class GameStartingEvent
{
    public string WorldId { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; } = 3;
}

public class RoomChatEvent
{
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

public class KickedFromRoomEvent
{
    public string Reason { get; set; } = string.Empty;
}
