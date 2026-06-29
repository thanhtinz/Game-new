using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Contracts.Auth;

namespace WorldFaith.Client.Network
{
    /// <summary>
    /// Quản lý kết nối SignalR đến LobbyHub.
    /// Tách riêng với WorldFaithClient để dễ quản lý lifecycle.
    /// </summary>
    public class LobbyClient : MonoBehaviour
    {
        public static LobbyClient Instance { get; private set; }

        [SerializeField] private string serverUrl = "http://localhost:5000/hubs/lobby";

        private HubConnection _connection;

        public event Action<RoomDto> OnRoomUpdated;
        public event Action<RoomDto> OnRoomListUpdated;
        public event Action OnRoomDisbanded;
        public event Action<PlayerReadyEvent> OnPlayerReady;
        public event Action<GameStartingEvent> OnGameStarting;
        public event Action<RoomChatEvent> OnRoomChat;
        public event Action<KickedFromRoomEvent> OnKicked;
        public event Action<string> OnError;
        public event Action OnConnected;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task ConnectAsync()
        {
            var token = Managers.AuthManager.Instance?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[LobbyClient] Chưa đăng nhập");
                return;
            }

            // JWT qua query string (SignalR chuẩn)
            var urlWithToken = $"{serverUrl}?access_token={token}";

            _connection = new HubConnectionBuilder()
                .WithUrl(urlWithToken)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();

            try
            {
                await _connection.StartAsync();
                Debug.Log("[LobbyClient] Kết nối Lobby thành công");
                MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyClient] Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
        }

        // ─── Client → Server ────────────────────────────────────

        public async Task JoinLobbyBrowserAsync()
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("JoinLobbyBrowser");
        }

        public async Task CreateRoomAsync(CreateRoomRequest request)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("CreateRoom", request);
        }

        public async Task JoinRoomAsync(string roomId, string password = "")
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("JoinRoom", new JoinRoomRequest { RoomId = roomId, Password = password });
        }

        public async Task LeaveRoomAsync()
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("LeaveRoom");
        }

        public async Task SetReadyAsync(bool isReady, string godName = "", string archetype = "")
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("SetReady", isReady, godName, archetype);
        }

        public async Task StartGameAsync()
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("StartGame");
        }

        public async Task SendChatAsync(string message)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("SendChat", message);
        }

        public async Task KickPlayerAsync(string playerId)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("KickPlayer", playerId);
        }

        // ─── Server → Client Handlers ───────────────────────────

        private void RegisterHandlers()
        {
            _connection.On<RoomUpdatedEvent>("OnRoomUpdated", evt =>
                MainThreadDispatcher.Enqueue(() => OnRoomUpdated?.Invoke(evt.Room)));

            _connection.On<RoomDto>("OnRoomListUpdated", room =>
                MainThreadDispatcher.Enqueue(() => OnRoomListUpdated?.Invoke(room)));

            _connection.On("OnRoomDisbanded", () =>
                MainThreadDispatcher.Enqueue(() => OnRoomDisbanded?.Invoke()));

            _connection.On<PlayerReadyEvent>("OnPlayerReady", evt =>
                MainThreadDispatcher.Enqueue(() => OnPlayerReady?.Invoke(evt)));

            _connection.On<GameStartingEvent>("OnGameStarting", evt =>
                MainThreadDispatcher.Enqueue(() => OnGameStarting?.Invoke(evt)));

            _connection.On<RoomChatEvent>("OnRoomChat", evt =>
                MainThreadDispatcher.Enqueue(() => OnRoomChat?.Invoke(evt)));

            _connection.On<KickedFromRoomEvent>("OnKicked", evt =>
                MainThreadDispatcher.Enqueue(() => OnKicked?.Invoke(evt)));

            _connection.On<string>("OnError", msg =>
                MainThreadDispatcher.Enqueue(() => OnError?.Invoke(msg)));
        }

        private void OnDestroy()
        {
            _ = DisconnectAsync();
        }
    }
}
