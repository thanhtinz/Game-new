using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Models.Auth;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Server.Services.Race;
using WorldFaith.Server.Services.Simulation;
using WorldFaith.Server.Services.WorldGen;
using WorldFaith.Shared.Contracts.Auth;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Services.Lobby;

public interface ILobbyService
{
    Task<(RoomDto? room, string? error)> CreateRoomAsync(string playerId, string displayName, CreateRoomRequest request);
    Task<(RoomDto? room, string? error)> JoinRoomAsync(string playerId, string displayName, JoinRoomRequest request);
    Task<bool> LeaveRoomAsync(string playerId);
    Task<bool> SetReadyAsync(string playerId, bool isReady, string? godName = null, string? archetype = null);
    Task<(bool success, string? error)> StartGameAsync(string playerId);
    Task<LobbyListResponse> GetLobbyListAsync();
    Task<RoomDto?> GetRoomByPlayerAsync(string playerId);
    Task KickPlayerAsync(string hostPlayerId, string targetPlayerId);
    Task SendChatAsync(string playerId, string displayName, string roomId, string message);
}

public class LobbyService : ILobbyService
{
    private readonly IRoomRepository _roomRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly ICivilizationSimulationService _civSim;
    private readonly IWorldGeneratorService _worldGen;
    private readonly IHubContext<LobbyHub, ILobbyHubClient> _lobbyHub;
    private readonly IHubContext<WorldHub, IWorldHubClient> _worldHub;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LobbyService> _logger;

    public LobbyService(
        IRoomRepository roomRepo,
        IWorldRepository worldRepo,
        ICivilizationRepository civRepo,
        ICivilizationSimulationService civSim,
        IWorldGeneratorService worldGen,
        IHubContext<LobbyHub, ILobbyHubClient> lobbyHub,
        IHubContext<WorldHub, IWorldHubClient> worldHub,
        IServiceProvider serviceProvider,
        ILogger<LobbyService> logger)
    {
        _roomRepo = roomRepo;
        _worldRepo = worldRepo;
        _civRepo = civRepo;
        _civSim = civSim;
        _worldGen = worldGen;
        _lobbyHub = lobbyHub;
        _worldHub = worldHub;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<(RoomDto? room, string? error)> CreateRoomAsync(
        string playerId, string displayName, CreateRoomRequest request)
    {
        // Player không được ở 2 phòng cùng lúc
        var existing = await _roomRepo.GetByPlayerIdAsync(playerId);
        if (existing != null)
            return (null, "Bạn đang ở trong phòng khác");

        string? passwordHash = null;
        if (request.IsPrivate && !string.IsNullOrEmpty(request.Password))
            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var room = new RoomDocument
        {
            Name = request.RoomName,
            HostPlayerId = playerId,
            HostDisplayName = displayName,
            MaxPlayers = Math.Clamp(request.MaxPlayers, 2, 8),
            GameMode = request.GameMode,
            VictoryCondition = request.VictoryCondition,
            ScenarioType = request.ScenarioType,
            WorldWidth = request.WorldWidth,
            WorldHeight = request.WorldHeight,
            IsPrivate = request.IsPrivate,
            PasswordHash = passwordHash,
            Players = new List<RoomPlayerEntry>
            {
                new() { PlayerId = playerId, DisplayName = displayName, IsHost = true }
            }
        };

        await _roomRepo.CreateAsync(room);

        var dto = MapToDto(room);
        // Broadcast danh sách phòng cập nhật cho lobby
        await _lobbyHub.Clients.Group("lobby").OnRoomListUpdated(dto);

        _logger.LogInformation("Room tạo: {RoomName} bởi {DisplayName}", room.Name, displayName);
        return (dto, null);
    }

    public async Task<(RoomDto? room, string? error)> JoinRoomAsync(
        string playerId, string displayName, JoinRoomRequest request)
    {
        var existing = await _roomRepo.GetByPlayerIdAsync(playerId);
        if (existing != null)
            return (null, "Bạn đang ở trong phòng khác");

        var room = await _roomRepo.GetByIdAsync(request.RoomId);
        if (room == null)
            return (null, "Phòng không tồn tại");

        if (room.Status != RoomStatus.Waiting)
            return (null, "Phòng đã bắt đầu hoặc đã kết thúc");

        if (room.Players.Count >= room.MaxPlayers)
            return (null, "Phòng đã đầy");

        if (room.IsPrivate && !string.IsNullOrEmpty(room.PasswordHash))
        {
            if (string.IsNullOrEmpty(request.Password) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, room.PasswordHash))
                return (null, "Mật khẩu không đúng");
        }

        var entry = new RoomPlayerEntry { PlayerId = playerId, DisplayName = displayName };
        await _roomRepo.AddPlayerAsync(room.Id, entry);

        var updatedRoom = await _roomRepo.GetByIdAsync(room.Id);
        var dto = MapToDto(updatedRoom!);

        // Notify tất cả player trong phòng
        await _lobbyHub.Clients.Group(room.Id).OnRoomUpdated(new RoomUpdatedEvent { Room = dto });

        _logger.LogInformation("Player {DisplayName} vào phòng {RoomName}", displayName, room.Name);
        return (dto, null);
    }

    public async Task<bool> LeaveRoomAsync(string playerId)
    {
        var room = await _roomRepo.GetByPlayerIdAsync(playerId);
        if (room == null) return false;

        await _roomRepo.RemovePlayerAsync(room.Id, playerId);

        // Nếu host rời thì chuyển host hoặc giải tán phòng
        if (room.HostPlayerId == playerId)
        {
            var remaining = room.Players.Where(p => p.PlayerId != playerId).ToList();
            if (!remaining.Any())
            {
                await _roomRepo.DisbandAsync(room.Id);
                await _lobbyHub.Clients.Group(room.Id).OnRoomDisbanded();
            }
            else
            {
                // Chuyển host cho player tiếp theo
                var newHost = remaining.First();
                room.HostPlayerId = newHost.PlayerId;
                room.HostDisplayName = newHost.DisplayName;
                newHost.IsHost = true;
                await _roomRepo.UpdateAsync(room);

                var updatedRoom = await _roomRepo.GetByIdAsync(room.Id);
                await _lobbyHub.Clients.Group(room.Id).OnRoomUpdated(
                    new RoomUpdatedEvent { Room = MapToDto(updatedRoom!) });
            }
        }
        else
        {
            var updatedRoom = await _roomRepo.GetByIdAsync(room.Id);
            if (updatedRoom != null)
                await _lobbyHub.Clients.Group(room.Id).OnRoomUpdated(
                    new RoomUpdatedEvent { Room = MapToDto(updatedRoom) });
        }

        return true;
    }

    public async Task<bool> SetReadyAsync(string playerId, bool isReady, string? godName = null, string? archetype = null)
    {
        var room = await _roomRepo.GetByPlayerIdAsync(playerId);
        if (room == null) return false;

        await _roomRepo.SetPlayerReadyAsync(room.Id, playerId, isReady);

        var updatedRoom = await _roomRepo.GetByIdAsync(room.Id);
        if (updatedRoom == null) return false;

        await _lobbyHub.Clients.Group(room.Id).OnPlayerReady(new PlayerReadyEvent
        {
            PlayerId = playerId,
            IsReady = isReady
        });

        return true;
    }

    public async Task<(bool success, string? error)> StartGameAsync(string playerId)
    {
        var room = await _roomRepo.GetByPlayerIdAsync(playerId);
        if (room == null) return (false, "Không tìm thấy phòng");

        if (room.HostPlayerId != playerId)
            return (false, "Chỉ host mới có thể bắt đầu game");

        if (room.Players.Count < 2)
            return (false, "Cần ít nhất 2 người chơi");

        var notReady = room.Players.Where(p => !p.IsReady && !p.IsHost).ToList();
        if (notReady.Any())
            return (false, $"{notReady.Count} người chơi chưa sẵn sàng");

        // Countdown
        await _lobbyHub.Clients.Group(room.Id).OnGameStarting(new GameStartingEvent
        {
            CountdownSeconds = 3
        });

        await Task.Delay(3000);

        // Tạo world
        if (!Enum.TryParse<GameMode>(room.GameMode, out var mode)) mode = GameMode.Sandbox;
        if (!Enum.TryParse<VictoryCondition>(room.VictoryCondition, out var victory))
            victory = VictoryCondition.LastSurvivingGod;

        var world = new WorldDocument
        {
            Name = room.Name,
            Mode = mode,
            MaxGods = room.Players.Count,
            Width = room.WorldWidth,
            Height = room.WorldHeight,
            VictoryCondition = victory,
            ScenarioType = room.ScenarioType,
            IsActive = true
        };
        await _worldRepo.CreateAsync(world);
        // WorldGenerator sinh map procedural (tiles + civilizations)
        await _worldGen.GenerateAsync(world.Id, world.Width, world.Height);

        // Spawn NPC Tier 2-5 cho mỗi civilization (v3)
        var npcSpawn = _serviceProvider?.GetService<INpcSpawnService>();
        if (npcSpawn != null)
        {
            var civs = await _civRepo.GetByWorldAsync(world.Id);
            foreach (var civ in civs)
                await npcSpawn.SpawnForCivilizationAsync(world.Id, civ);
            _logger.LogInformation("Spawned NPCs for {Count} civilizations in world {WorldId}", civs.Count, world.Id);
        }

        // Seed race affinity data (v1.0 GDD)
        var raceService = _serviceProvider?.GetService<IRaceAffinityService>();
        if (raceService != null)
            await raceService.SeedRaceDataAsync(world.Id);

        await _roomRepo.SetStatusAsync(room.Id, RoomStatus.InGame, world.Id);

        // Notify world id để client switch scene
        await _lobbyHub.Clients.Group(room.Id).OnGameStarting(new GameStartingEvent
        {
            WorldId = world.Id,
            CountdownSeconds = 0
        });

        _logger.LogInformation("Game bắt đầu: World {WorldId} từ Room {RoomId}", world.Id, room.Id);
        return (true, null);
    }

    public async Task<LobbyListResponse> GetLobbyListAsync()
    {
        var rooms = await _roomRepo.GetPublicWaitingRoomsAsync();
        return new LobbyListResponse
        {
            Rooms = rooms.Select(MapToDto).ToList(),
            TotalOnline = rooms.Sum(r => r.Players.Count)
        };
    }

    public async Task<RoomDto?> GetRoomByPlayerAsync(string playerId)
    {
        var room = await _roomRepo.GetByPlayerIdAsync(playerId);
        return room == null ? null : MapToDto(room);
    }

    public async Task KickPlayerAsync(string hostPlayerId, string targetPlayerId)
    {
        var room = await _roomRepo.GetByPlayerIdAsync(hostPlayerId);
        if (room == null || room.HostPlayerId != hostPlayerId) return;

        await _roomRepo.RemovePlayerAsync(room.Id, targetPlayerId);
        await _lobbyHub.Clients.User(targetPlayerId).OnKicked(new KickedFromRoomEvent
        {
            Reason = "Bạn đã bị kick khỏi phòng"
        });

        var updatedRoom = await _roomRepo.GetByIdAsync(room.Id);
        if (updatedRoom != null)
            await _lobbyHub.Clients.Group(room.Id).OnRoomUpdated(
                new RoomUpdatedEvent { Room = MapToDto(updatedRoom) });
    }

    public async Task SendChatAsync(string playerId, string displayName, string roomId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > 200) return;

        await _lobbyHub.Clients.Group(roomId).OnRoomChat(new RoomChatEvent
        {
            PlayerId = playerId,
            DisplayName = displayName,
            Message = message.Trim(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }

    // ─── Helpers ────────────────────────────────────────────

    private static RoomDto MapToDto(RoomDocument r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        HostPlayerId = r.HostPlayerId,
        HostDisplayName = r.HostDisplayName,
        MaxPlayers = r.MaxPlayers,
        CurrentPlayers = r.Players.Count,
        GameMode = r.GameMode,
        Status = r.Status.ToString(),
        IsPrivate = r.IsPrivate,
        Players = r.Players.Select(p => new RoomPlayerDto
        {
            PlayerId = p.PlayerId,
            DisplayName = p.DisplayName,
            IsReady = p.IsReady,
            IsHost = p.IsHost,
            SelectedGodName = p.SelectedGodName,
            SelectedArchetype = p.SelectedArchetype
        }).ToList(),
        CreatedAt = new DateTimeOffset(r.CreatedAt).ToUnixTimeSeconds()
    };
}
