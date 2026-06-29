using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using WorldFaith.Server.Services.Chat;

namespace WorldFaith.Client.Network
{
    /// <summary>
    /// SignalR client cho ChatHub.
    /// Connect sau khi join world, auto reconnect.
    /// </summary>
    public class ChatClient : MonoBehaviour
    {
        public static ChatClient Instance { get; private set; }

        [SerializeField] private string serverUrl = "http://localhost:5000/hubs/chat";

        private HubConnection _connection;

        public event Action<ChatMessageDto> OnMessage;
        public event Action<List<ChatMessageDto>> OnHistory;
        public event Action<ChatMessageDto> OnWhisper;
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
            if (string.IsNullOrEmpty(token)) return;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}?access_token={token}")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<ChatMessageDto>("OnChatMessage", msg =>
                MainThreadDispatcher.Enqueue(() => OnMessage?.Invoke(msg)));

            _connection.On<List<ChatMessageDto>>("OnChatHistory", history =>
                MainThreadDispatcher.Enqueue(() => OnHistory?.Invoke(history)));

            _connection.On<ChatMessageDto>("OnWhisper", msg =>
                MainThreadDispatcher.Enqueue(() => OnWhisper?.Invoke(msg)));

            _connection.On<string>("OnChatError", err =>
                MainThreadDispatcher.Enqueue(() => OnError?.Invoke(err)));

            try
            {
                await _connection.StartAsync();
                MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatClient] Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task JoinWorldChatAsync(string worldId)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("JoinWorldChat", worldId); }
            catch (Exception ex) { Debug.LogError($"[ChatClient] JoinWorldChat: {ex.Message}"); }
        }

        public async Task SendMessageAsync(string message, string type = "Normal")
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(message)) return;
            try { await _connection.InvokeAsync("SendMessage", message, type); }
            catch (Exception ex) { Debug.LogError($"[ChatClient] SendMessage: {ex.Message}"); }
        }

        public async Task SendWhisperAsync(string targetGodId, string message)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("SendWhisper", targetGodId, message); }
            catch (Exception ex) { Debug.LogError($"[ChatClient] SendWhisper: {ex.Message}"); }
        }

        public async Task SendReactionAsync(string reaction)
        {
            if (!IsConnected) return;
            try { await _connection.InvokeAsync("SendReaction", reaction); }
            catch (Exception ex) { Debug.LogError($"[ChatClient] SendReaction: {ex.Message}"); }
        }

        private void OnDestroy()
        {
            _ = _connection?.DisposeAsync().AsTask();
        }
    }
}
