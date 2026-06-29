using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Contracts.Auth;

namespace WorldFaith.Client.UI.Lobby
{
    /// <summary>
    /// Lobby UI: danh sách phòng, tạo phòng, hiển thị player trong phòng.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject roomListPanel;
        [SerializeField] private GameObject roomDetailPanel;
        [SerializeField] private GameObject createRoomPanel;

        [Header("Room List")]
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomItemPrefab;
        [SerializeField] private Button createRoomBtn;
        [SerializeField] private Button refreshBtn;
        [SerializeField] private TextMeshProUGUI onlineCountText;

        [Header("Create Room")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_Dropdown maxPlayersDropdown;
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown scenarioDropdown;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button confirmCreateBtn;
        [SerializeField] private Button cancelCreateBtn;

        [Header("Room Detail (inside room)")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerItemPrefab;
        [SerializeField] private Button readyBtn;
        [SerializeField] private Button startGameBtn;
        [SerializeField] private Button leaveRoomBtn;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendChatBtn;
        [SerializeField] private Transform chatContainer;
        [SerializeField] private GameObject chatMessagePrefab;

        [Header("God Selection")]
        [SerializeField] private TMP_InputField godNameInput;
        [SerializeField] private TMP_Dropdown archetypeDropdown;

        private RoomDto _currentRoom;
        private bool _isReady;
        private readonly List<GameObject> _roomItems = new();
        private readonly List<GameObject> _playerItems = new();

        private async void Start()
        {
            // Setup buttons
            createRoomBtn?.onClick.AddListener(ShowCreateRoomPanel);
            refreshBtn?.onClick.AddListener(async () => await LobbyClient.Instance.JoinLobbyBrowserAsync());
            confirmCreateBtn?.onClick.AddListener(OnCreateRoom);
            cancelCreateBtn?.onClick.AddListener(ShowRoomListPanel);
            readyBtn?.onClick.AddListener(OnToggleReady);
            startGameBtn?.onClick.AddListener(OnStartGame);
            leaveRoomBtn?.onClick.AddListener(OnLeaveRoom);
            sendChatBtn?.onClick.AddListener(OnSendChat);

            privateToggle?.onValueChanged.AddListener(v =>
                passwordInput?.gameObject.SetActive(v));
            passwordInput?.gameObject.SetActive(false);

            // Subscribe lobby events
            SubscribeEvents();

            // Kết nối lobby hub
            if (LobbyClient.Instance != null)
            {
                await LobbyClient.Instance.ConnectAsync();
                await LobbyClient.Instance.JoinLobbyBrowserAsync();
            }

            ShowRoomListPanel();
        }

        private void OnDestroy() => UnsubscribeEvents();

        // ─── Event Subscriptions ─────────────────────────────────

        private void SubscribeEvents()
        {
            var client = LobbyClient.Instance;
            if (client == null) return;

            client.OnRoomListUpdated += HandleRoomListUpdated;
            client.OnRoomUpdated += HandleRoomUpdated;
            client.OnRoomDisbanded += HandleRoomDisbanded;
            client.OnPlayerReady += HandlePlayerReady;
            client.OnGameStarting += HandleGameStarting;
            client.OnRoomChat += HandleRoomChat;
            client.OnKicked += HandleKicked;
            client.OnError += HandleError;
        }

        private void UnsubscribeEvents()
        {
            var client = LobbyClient.Instance;
            if (client == null) return;

            client.OnRoomListUpdated -= HandleRoomListUpdated;
            client.OnRoomUpdated -= HandleRoomUpdated;
            client.OnRoomDisbanded -= HandleRoomDisbanded;
            client.OnPlayerReady -= HandlePlayerReady;
            client.OnGameStarting -= HandleGameStarting;
            client.OnRoomChat -= HandleRoomChat;
            client.OnKicked -= HandleKicked;
            client.OnError -= HandleError;
        }

        // ─── Event Handlers ──────────────────────────────────────

        private void HandleRoomListUpdated(RoomDto room)
        {
            // Thêm hoặc cập nhật room item trong list
            RefreshRoomItem(room);
            if (onlineCountText != null)
                onlineCountText.text = $"Online: {room.CurrentPlayers}";
        }

        private void HandleRoomUpdated(RoomDto room)
        {
            _currentRoom = room;
            RefreshRoomDetail();
        }

        private void HandleRoomDisbanded()
        {
            _currentRoom = null;
            ShowRoomListPanel();
            Debug.Log("[LobbyUI] Phòng đã bị giải tán");
        }

        private void HandlePlayerReady(PlayerReadyEvent evt)
        {
            RefreshRoomDetail();
        }

        private void HandleGameStarting(GameStartingEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.WorldId))
            {
                // Lưu worldId và chuyển scene
                PlayerPrefs.SetString("wf_world_id", evt.WorldId);
                PlayerPrefs.Save();
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                // Countdown
                Debug.Log($"[LobbyUI] Game bắt đầu trong {evt.CountdownSeconds}s");
            }
        }

        private void HandleRoomChat(RoomChatEvent evt)
        {
            if (chatContainer == null || chatMessagePrefab == null) return;
            var msg = Instantiate(chatMessagePrefab, chatContainer);
            var text = msg.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"[{evt.DisplayName}]: {evt.Message}";
        }

        private void HandleKicked(KickedFromRoomEvent evt)
        {
            _currentRoom = null;
            ShowRoomListPanel();
            Debug.Log($"[LobbyUI] Bị kick: {evt.Reason}");
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[LobbyUI] Lỗi: {error}");
        }

        // ─── Actions ─────────────────────────────────────────────

        private void ShowRoomListPanel()
        {
            roomListPanel?.SetActive(true);
            roomDetailPanel?.SetActive(false);
            createRoomPanel?.SetActive(false);
        }

        private void ShowCreateRoomPanel()
        {
            roomListPanel?.SetActive(false);
            createRoomPanel?.SetActive(true);
        }

        private async void OnCreateRoom()
        {
            var roomName = roomNameInput?.text?.Trim();
            if (string.IsNullOrEmpty(roomName)) return;

            // Scenario options: Standard, TheLastLight, ReligionWars, EvolutionRace, FaithCrisis, Apocalypse
            var scenarios = new[] { "Standard", "TheLastLight", "ReligionWars", "EvolutionRace", "FaithCrisis", "Apocalypse" };
            int scenarioIdx = scenarioDropdown?.value ?? 0;
            string scenarioType = scenarioIdx < scenarios.Length ? scenarios[scenarioIdx] : "Standard";

            var request = new CreateRoomRequest
            {
                RoomName      = roomName,
                MaxPlayers    = (maxPlayersDropdown?.value ?? 0) + 2,
                GameMode      = gameModeDropdown?.options[gameModeDropdown.value].text ?? "Sandbox",
                ScenarioType  = scenarioType,
                IsPrivate     = privateToggle?.isOn ?? false,
                Password      = privateToggle?.isOn == true ? passwordInput?.text : null
            };

            await LobbyClient.Instance.CreateRoomAsync(request);
            roomDetailPanel?.SetActive(true);
            createRoomPanel?.SetActive(false);
            roomListPanel?.SetActive(false);
        }

        private async void OnToggleReady()
        {
            _isReady = !_isReady;
            var godName = godNameInput?.text?.Trim() ?? "My God";
            var archetype = archetypeDropdown?.options[archetypeDropdown?.value ?? 0].text ?? "Order";
            await LobbyClient.Instance.SetReadyAsync(_isReady, godName, archetype);

            if (readyBtn != null)
            {
                var text = readyBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = _isReady ? "Hủy sẵn sàng" : "Sẵn sàng";
            }
        }

        private async void OnStartGame() => await LobbyClient.Instance.StartGameAsync();

        private async void OnLeaveRoom()
        {
            await LobbyClient.Instance.LeaveRoomAsync();
            _currentRoom = null;
            _isReady = false;
            ShowRoomListPanel();
        }

        private async void OnSendChat()
        {
            var msg = chatInput?.text?.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            await LobbyClient.Instance.SendChatAsync(msg);
            if (chatInput != null) chatInput.text = "";
        }

        // ─── UI Refresh ──────────────────────────────────────────

        private void RefreshRoomItem(RoomDto room)
        {
            if (roomListContainer == null || roomItemPrefab == null) return;

            var existing = roomListContainer.Find(room.Id);
            GameObject item;
            if (existing != null)
                item = existing.gameObject;
            else
            {
                item = Instantiate(roomItemPrefab, roomListContainer);
                item.name = room.Id;
            }

            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = room.Name;
            if (texts.Length > 1) texts[1].text = $"{room.CurrentPlayers}/{room.MaxPlayers}";
            if (texts.Length > 2) texts[2].text = room.GameMode;

            var btn = item.GetComponent<Button>();
            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(async () =>
            {
                await LobbyClient.Instance.JoinRoomAsync(room.Id);
                roomDetailPanel?.SetActive(true);
                roomListPanel?.SetActive(false);
            });
        }

        private void RefreshRoomDetail()
        {
            if (_currentRoom == null) return;

            if (roomNameText != null)
                roomNameText.text = _currentRoom.Name;

            // Xóa và rebuild player list
            foreach (var item in _playerItems) Destroy(item);
            _playerItems.Clear();

            if (playerListContainer == null || playerItemPrefab == null) return;

            var myId = Managers.AuthManager.Instance?.PlayerId ?? "";
            bool isHost = _currentRoom.HostPlayerId == myId;

            foreach (var player in _currentRoom.Players)
            {
                var item = Instantiate(playerItemPrefab, playerListContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = player.DisplayName + (player.IsHost ? " [Host]" : "");
                if (texts.Length > 1) texts[1].text = player.IsReady ? "Sẵn sàng" : "Chờ...";
                _playerItems.Add(item);
            }

            // Chỉ host mới thấy nút Start
            startGameBtn?.gameObject.SetActive(isHost);
        }
    }
}
