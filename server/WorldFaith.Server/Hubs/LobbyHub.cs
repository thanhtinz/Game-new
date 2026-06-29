using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Services.Lobby;
using WorldFaith.Shared.Contracts.Auth;

namespace WorldFaith.Server.Hubs;

// Interface strongly-typed cho lobby clients
public interface ILobbyHubClient
{
    Task OnRoomUpdated(RoomUpdatedEvent evt);
    Task OnRoomListUpdated(RoomDto room);
    Task OnRoomDisbanded();
    Task OnPlayerReady(PlayerReadyEvent evt);
    Task OnGameStarting(GameStartingEvent evt);
    Task OnRoomChat(RoomChatEvent evt);
    Task OnKicked(KickedFromRoomEvent evt);
    Task OnError(string message);
}

[Authorize]
public class LobbyHub : Hub<ILobbyHubClient>
{
    private readonly ILobbyService _lobbyService;
    private readonly ILogger<LobbyHub> _logger;

    public LobbyHub(ILobbyService lobbyService, ILogger<LobbyHub> logger)
    {
        _lobbyService = lobbyService;
        _logger = logger;
    }

    private string PlayerId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value
        ?? string.Empty;

    private string DisplayName => Context.User?.FindFirst("displayName")?.Value
        ?? "Unknown";

    // ─── Lobby Browser ──────────────────────────────────────

    public async Task JoinLobbyBrowser()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        var list = await _lobbyService.GetLobbyListAsync();
        await Clients.Caller.OnRoomListUpdated(list.Rooms.FirstOrDefault() ?? new RoomDto());
    }

    public async Task LeaveLobbyBrowser()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
    }

    // ─── Room Actions ───────────────────────────────────────

    public async Task CreateRoom(CreateRoomRequest request)
    {
        var (room, error) = await _lobbyService.CreateRoomAsync(PlayerId, DisplayName, request);
        if (error != null) { await Clients.Caller.OnError(error); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, room!.Id);
        await Clients.Caller.OnRoomUpdated(new RoomUpdatedEvent { Room = room });
    }

    public async Task JoinRoom(JoinRoomRequest request)
    {
        var (room, error) = await _lobbyService.JoinRoomAsync(PlayerId, DisplayName, request);
        if (error != null) { await Clients.Caller.OnError(error); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, room!.Id);
        await Clients.Caller.OnRoomUpdated(new RoomUpdatedEvent { Room = room });
    }

    public async Task LeaveRoom()
    {
        var room = await _lobbyService.GetRoomByPlayerAsync(PlayerId);
        if (room != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id);

        await _lobbyService.LeaveRoomAsync(PlayerId);
    }

    public async Task SetReady(bool isReady, string? godName = null, string? archetype = null)
    {
        await _lobbyService.SetReadyAsync(PlayerId, isReady, godName, archetype);
    }

    public async Task StartGame()
    {
        var (success, error) = await _lobbyService.StartGameAsync(PlayerId);
        if (!success && error != null)
            await Clients.Caller.OnError(error);
    }

    public async Task KickPlayer(string targetPlayerId)
    {
        await _lobbyService.KickPlayerAsync(PlayerId, targetPlayerId);
    }

    public async Task SendChat(string message)
    {
        var room = await _lobbyService.GetRoomByPlayerAsync(PlayerId);
        if (room == null) return;
        await _lobbyService.SendChatAsync(PlayerId, DisplayName, room.Id, message);
    }

    // ─── Connection Lifecycle ───────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _lobbyService.LeaveRoomAsync(PlayerId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
        await base.OnDisconnectedAsync(exception);
    }
}
