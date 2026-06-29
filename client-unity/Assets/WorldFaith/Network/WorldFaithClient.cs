using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.Network
{
    /// <summary>
    /// Quản lý kết nối SignalR đến WorldFaith server.
    /// Singleton - attach vào GameObject không bị destroy.
    /// </summary>
    public class WorldFaithClient : MonoBehaviour
    {
        public static WorldFaithClient Instance { get; private set; }

        [Header("Server Config")]
        [SerializeField] private string serverUrl = "http://localhost:5000/hubs/world";

        private HubConnection _connection;
        private bool _isConnecting;

        // ─── Events (UI và Manager subscribe) ──────────────────
        public event Action<WorldStateDto> OnWorldStateReceived;
        public event Action<WorldTickEvent> OnWorldTick;
        public event Action<MiracleResultEvent> OnMiracleResult;
        public event Action<GodUpdateEvent> OnGodUpdate;
        public event Action<CivilizationUpdateEvent> OnCivilizationUpdate;
        public event Action<ReligionUpdateEvent> OnReligionUpdate;
        public event Action<WorldRebirthEvent> OnWorldRebirth;
        public event Action<GameEndEvent> OnGameEnd;
        public event Action<GodDto> OnJoinedWorld;
        public event Action<ErrorEvent> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        // ─── Unity Lifecycle ────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await ConnectAsync();
        }

        private void OnDestroy()
        {
            _ = DisconnectAsync();
        }

        // ─── Connection ─────────────────────────────────────────

        public async Task ConnectAsync()
        {
            if (_isConnecting || IsConnected) return;
            _isConnecting = true;

            var token = Managers.AuthManager.Instance?.AccessToken ?? "";
            var urlWithToken = string.IsNullOrEmpty(token)
                ? serverUrl
                : $"{serverUrl}?access_token={token}";

            _connection = new HubConnectionBuilder()
                .WithUrl(urlWithToken)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            RegisterHandlers();

            _connection.Reconnecting += error =>
            {
                Debug.LogWarning($"[WorldFaith] Đang kết nối lại: {error?.Message}");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Debug.Log($"[WorldFaith] Kết nối lại thành công: {connectionId}");
                MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                Debug.LogWarning($"[WorldFaith] Mất kết nối: {error?.Message}");
                MainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke());
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                Debug.Log("[WorldFaith] Kết nối thành công");
                MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldFaith] Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
        }

        // ─── Server Method Invocations ──────────────────────────

        public async Task JoinWorldAsync(JoinWorldRequest request)
        {
            if (!IsConnected) { Debug.LogWarning("[WorldFaith] Chưa kết nối server"); return; }
            try { await _connection.InvokeAsync("JoinWorld", request); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] JoinWorld lỗi: {ex.Message}"); }
        }

        public async Task PerformMiracleAsync(PerformMiracleRequest request)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("PerformMiracle", request); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] PerformMiracle lỗi: {ex.Message}"); }
        }

        public async Task CounterMiracleAsync(CounterMiracleRequest request)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("CounterMiracle", request); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] CounterMiracle lỗi: {ex.Message}"); }
        }

        public async Task CreateWorldAsync(CreateWorldRequest request)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("CreateWorld", request); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] CreateWorld lỗi: {ex.Message}"); }
        }

        public async Task RequestWorldStateAsync()
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("RequestWorldState"); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] RequestWorldState lỗi: {ex.Message}"); }
        }

        public async Task EvolveEntityAsync(string entityId)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("EvolveEntity", entityId); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] EvolveEntity lỗi: {ex.Message}"); }
        }

        public async Task FoundReligionAsync(string religionName, bool isHidden = false)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("FoundReligion", religionName, isHidden); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] FoundReligion lỗi: {ex.Message}"); }
        }

        public async Task BuildTempleAsync(string religionId, string civId)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("BuildTemple", religionId, civId); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] BuildTemple lỗi: {ex.Message}"); }
        }

        public async Task IssueCommandmentAsync(string civId, string commandmentType, string? message = null)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("IssueCommandment", civId, commandmentType, message); }
            catch (Exception ex) { Debug.LogError($"[WorldFaith] IssueCommandment lỗi: {ex.Message}"); }
        }

        // ─── Event Handlers (server → client) ──────────────────

        private void RegisterHandlers()
        {
            _connection.On<WorldStateDto>("OnWorldState", state =>
                MainThreadDispatcher.Enqueue(() => OnWorldStateReceived?.Invoke(state)));

            _connection.On<WorldTickEvent>("OnWorldTick", evt =>
                MainThreadDispatcher.Enqueue(() => OnWorldTick?.Invoke(evt)));

            _connection.On<MiracleResultEvent>("OnMiracleResult", evt =>
                MainThreadDispatcher.Enqueue(() => OnMiracleResult?.Invoke(evt)));

            _connection.On<GodUpdateEvent>("OnGodUpdate", evt =>
                MainThreadDispatcher.Enqueue(() => OnGodUpdate?.Invoke(evt)));

            _connection.On<CivilizationUpdateEvent>("OnCivilizationUpdate", evt =>
                MainThreadDispatcher.Enqueue(() => OnCivilizationUpdate?.Invoke(evt)));

            _connection.On<ReligionUpdateEvent>("OnReligionUpdate", evt =>
                MainThreadDispatcher.Enqueue(() => OnReligionUpdate?.Invoke(evt)));

            _connection.On<WorldRebirthEvent>("OnWorldRebirth", evt =>
                MainThreadDispatcher.Enqueue(() => OnWorldRebirth?.Invoke(evt)));

            _connection.On<GameEndEvent>("OnGameEnd", evt =>
                MainThreadDispatcher.Enqueue(() => OnGameEnd?.Invoke(evt)));

            _connection.On<GodDto>("OnJoinedWorld", god =>
                MainThreadDispatcher.Enqueue(() => OnJoinedWorld?.Invoke(god)));

            _connection.On<ErrorEvent>("OnError", err =>
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.LogError($"[WorldFaith] Server error [{err.Code}]: {err.Message}");
                    OnError?.Invoke(err);
                }));
        }
    }
}
