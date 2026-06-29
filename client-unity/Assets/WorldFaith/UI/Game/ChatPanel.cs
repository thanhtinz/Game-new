using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Network;
using WorldFaith.Server.Services.Chat;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Chat panel in-game giữa các Gods.
    /// Features: message history, quick reactions, whisper mode, emote.
    /// Mobile-optimized: full-width bottom panel, collapsible.
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private Button toggleChatBtn;
        [SerializeField] private TextMeshProUGUI unreadBadge;

        [Header("Message List")]
        [SerializeField] private Transform messageContainer;
        [SerializeField] private GameObject messagePrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int maxMessages = 60;

        [Header("Input")]
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendBtn;
        [SerializeField] private Button emoteToggleBtn;

        [Header("Quick Reactions")]
        [SerializeField] private GameObject reactionsPanel;
        [SerializeField] private Button[] reactionBtns; // 😂 ⚡ 🔥 💀 🙏 😈 👑 🌍

        [Header("Whisper")]
        [SerializeField] private GameObject whisperIndicator;
        [SerializeField] private TextMeshProUGUI whisperTargetText;
        [SerializeField] private Button cancelWhisperBtn;

        private readonly List<GameObject> _messages = new();
        private bool _isPanelOpen = true;
        private int _unreadCount;
        private string _whisperTargetId;
        private string _whisperTargetName;

        // Màu theo archetype
        private static readonly Dictionary<string, Color> ArchetypeColors = new()
        {
            { "Order",     new Color(0.9f, 0.9f, 1.0f) },
            { "Chaos",     new Color(1.0f, 0.4f, 0.8f) },
            { "Light",     new Color(1.0f, 1.0f, 0.6f) },
            { "Darkness",  new Color(0.6f, 0.2f, 0.8f) },
            { "Nature",    new Color(0.4f, 0.9f, 0.4f) },
            { "Death",     new Color(0.7f, 0.7f, 0.7f) },
            { "Knowledge", new Color(0.4f, 0.8f, 1.0f) },
            { "War",       new Color(1.0f, 0.3f, 0.3f) },
            { "System",    new Color(0.8f, 0.8f, 0.2f) },
        };

        private static readonly string[] Reactions = { "😂", "⚡", "🔥", "💀", "🙏", "😈", "👑", "🌍" };

        private void Start()
        {
            toggleChatBtn?.onClick.AddListener(TogglePanel);
            sendBtn?.onClick.AddListener(OnSendClick);
            emoteToggleBtn?.onClick.AddListener(ToggleReactions);
            cancelWhisperBtn?.onClick.AddListener(CancelWhisper);

            chatInput?.onSubmit.AddListener(_ => OnSendClick());

            // Setup reaction buttons
            for (int i = 0; i < reactionBtns?.Length; i++)
            {
                int idx = i;
                if (idx < Reactions.Length)
                    reactionBtns[idx]?.onClick.AddListener(() => SendReaction(Reactions[idx]));
            }

            reactionsPanel?.SetActive(false);
            whisperIndicator?.SetActive(false);
            unreadBadge?.gameObject.SetActive(false);

            // Subscribe chat events
            var client = ChatClient.Instance;
            if (client != null)
            {
                client.OnMessage += AddMessage;
                client.OnHistory += LoadHistory;
                client.OnWhisper += AddWhisperMessage;
            }
        }

        private void OnDestroy()
        {
            var client = ChatClient.Instance;
            if (client != null)
            {
                client.OnMessage -= AddMessage;
                client.OnHistory -= LoadHistory;
                client.OnWhisper -= AddWhisperMessage;
            }
        }

        // ─── Toggle ──────────────────────────────────────────────

        private void TogglePanel()
        {
            _isPanelOpen = !_isPanelOpen;
            chatPanel?.SetActive(_isPanelOpen);
            if (_isPanelOpen)
            {
                _unreadCount = 0;
                UpdateUnreadBadge();
                ScrollToBottom();
            }
        }

        private void ToggleReactions()
            => reactionsPanel?.SetActive(!(reactionsPanel?.activeSelf ?? false));

        // ─── Messages ────────────────────────────────────────────

        private void LoadHistory(List<ChatMessageDto> history)
        {
            foreach (var msg in history)
                AddMessage(msg);
        }

        private void AddMessage(ChatMessageDto msg)
        {
            if (messageContainer == null || messagePrefab == null) return;

            var item = Instantiate(messagePrefab, messageContainer);
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            bool isSystem = msg.Type == "System";
            bool isEmote  = msg.Type == "Emote";
            bool isMyMsg  = msg.GodId == Managers.GameManager.Instance?.MyGod?.Id;

            // Format
            string displayText = isSystem
                ? msg.Message
                : isEmote
                    ? $"✨ {msg.GodName} {msg.Message}"
                    : $"[{msg.GodArchetype}] {msg.GodName}: {msg.Message}";

            if (texts.Length > 0)
            {
                texts[0].text = displayText;
                texts[0].color = isSystem
                    ? new Color(1f, 0.9f, 0.3f)
                    : ArchetypeColors.TryGetValue(msg.GodArchetype, out var c)
                        ? c : Color.white;
                texts[0].fontStyle = isMyMsg ? FontStyles.Bold : FontStyles.Normal;
            }

            _messages.Add(item);
            while (_messages.Count > maxMessages)
            {
                Destroy(_messages[0]);
                _messages.RemoveAt(0);
            }

            // Unread badge nếu panel đóng
            if (!_isPanelOpen && !isSystem)
            {
                _unreadCount++;
                UpdateUnreadBadge();
            }

            if (_isPanelOpen)
                Invoke(nameof(ScrollToBottom), 0.05f);
        }

        private void AddWhisperMessage(ChatMessageDto msg)
        {
            if (messageContainer == null || messagePrefab == null) return;

            var item = Instantiate(messagePrefab, messageContainer);
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            bool isFromMe = msg.GodId == Managers.GameManager.Instance?.MyGod?.Id;
            string prefix = isFromMe ? "→ Whisper đến" : "← Whisper từ";

            if (texts.Length > 0)
            {
                texts[0].text = $"[{prefix} {msg.GodName}]: {msg.Message}";
                texts[0].color = new Color(0.8f, 0.6f, 1f);
                texts[0].fontStyle = FontStyles.Italic;
            }

            _messages.Add(item);
            if (!_isPanelOpen) { _unreadCount++; UpdateUnreadBadge(); }
            if (_isPanelOpen) Invoke(nameof(ScrollToBottom), 0.05f);
        }

        // ─── Send ────────────────────────────────────────────────

        private async void OnSendClick()
        {
            var text = chatInput?.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (_whisperTargetId != null)
                await ChatClient.Instance.SendWhisperAsync(_whisperTargetId, text);
            else
                await ChatClient.Instance.SendMessageAsync(text);

            if (chatInput != null) chatInput.text = "";
        }

        private async void SendReaction(string reaction)
        {
            await ChatClient.Instance.SendReactionAsync(reaction);
            reactionsPanel?.SetActive(false);
        }

        // ─── Whisper ─────────────────────────────────────────────

        public void StartWhisper(string targetGodId, string targetGodName)
        {
            _whisperTargetId = targetGodId;
            _whisperTargetName = targetGodName;
            whisperIndicator?.SetActive(true);
            if (whisperTargetText) whisperTargetText.text = $"→ {targetGodName}";
            chatInput?.Select();
        }

        private void CancelWhisper()
        {
            _whisperTargetId = null;
            _whisperTargetName = null;
            whisperIndicator?.SetActive(false);
        }

        // ─── Helpers ─────────────────────────────────────────────

        private void ScrollToBottom()
        {
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void UpdateUnreadBadge()
        {
            if (unreadBadge == null) return;
            unreadBadge.gameObject.SetActive(_unreadCount > 0);
            unreadBadge.text = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
        }
    }
}
