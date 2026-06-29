using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// HUD tổng hợp cho GameScene.
    /// Quản lý layout, event log, tab switching, và mobile-friendly buttons.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI faithText;
        [SerializeField] private TextMeshProUGUI followersText;
        [SerializeField] private TextMeshProUGUI tickText;
        [SerializeField] private TextMeshProUGUI godNameText;

        [Header("Bottom Tab Bar (Mobile)")]
        [SerializeField] private Button tabMiraclesBtn;
        [SerializeField] private Button tabReligionBtn;
        [SerializeField] private Button tabEvolutionBtn;
        [SerializeField] private Button tabMapBtn;
        [SerializeField] private Button tabChatBtn;
        [SerializeField] private Button tabCommandmentBtn;

        [Header("Panels")]
        [SerializeField] private GameObject miraclePanel;
        [SerializeField] private GameObject religionPanel;
        [SerializeField] private GameObject evolutionPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private GameObject commandmentPanel;

        [Header("Event Log")]
        [SerializeField] private Transform eventLogContainer;
        [SerializeField] private GameObject eventLogItemPrefab;
        [SerializeField] private ScrollRect eventLogScrollRect;
        [SerializeField] private int maxLogEntries = 30;

        [Header("Notification Toast")]
        [SerializeField] private GameObject toastObject;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private float toastDuration = 3f;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button returnToLobbyBtn;

        private readonly Queue<GameObject> _logItems = new();
        private Coroutine _toastCoroutine;
        private GameObject _activePanel;

        private void Start()
        {
            // Tab buttons
            tabMiraclesBtn?.onClick.AddListener(() => ShowPanel(miraclePanel));
            tabReligionBtn?.onClick.AddListener(() => ShowPanel(religionPanel));
            tabEvolutionBtn?.onClick.AddListener(() => ShowPanel(evolutionPanel));
            tabMapBtn?.onClick.AddListener(() => ShowPanel(mapPanel));
            tabChatBtn?.onClick.AddListener(() => ShowPanel(chatPanel));
            tabCommandmentBtn?.onClick.AddListener(() => ShowPanel(commandmentPanel));

            returnToLobbyBtn?.onClick.AddListener(OnReturnToLobby);

            // Subscribe GameManager events
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnTick += OnTick;
                gm.OnMiracleResult += OnMiracleResult;
                gm.OnNotification += ShowToast;
                gm.OnRebirth += OnRebirth;
                gm.OnGameOver += OnGameOver;
            }

            // Subscribe world tick deltas cho event log
            var client = Network.WorldFaithClient.Instance;
            if (client != null)
                client.OnWorldTick += OnWorldTick;

            // Default: show miracle panel
            ShowPanel(miraclePanel);
            toastObject?.SetActive(false);
            gameOverPanel?.SetActive(false);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnTick -= OnTick;
                gm.OnMiracleResult -= OnMiracleResult;
                gm.OnNotification -= ShowToast;
                gm.OnRebirth -= OnRebirth;
                gm.OnGameOver -= OnGameOver;
            }

            var client = Network.WorldFaithClient.Instance;
            if (client != null)
                client.OnWorldTick -= OnWorldTick;
        }

        // ─── Tick Update ─────────────────────────────────────────

        private void OnTick(long tick, int cycle)
        {
            if (tickText) tickText.text = $"Tick {tick} | Chu kỳ {cycle}";

            var god = GameManager.Instance?.MyGod;
            if (god == null) return;

            if (faithText) faithText.text = $"⚡ {god.Faith:F0}";
            if (followersText) followersText.text = $"👥 {god.FollowerCount:N0}";
            if (godNameText) godNameText.text = god.Name;
        }

        // ─── Event Log ───────────────────────────────────────────

        private void OnWorldTick(WorldTickEvent evt)
        {
            foreach (var delta in evt.Deltas)
            {
                if (!string.IsNullOrEmpty(delta.Description))
                    AddLogEntry(delta.Description, GetDeltaColor(delta.Type));
            }
        }

        private void OnMiracleResult(MiracleResultEvent evt)
        {
            var color = evt.Success ? Color.cyan : Color.red;
            var prefix = evt.WasCountered ? "🛡 [Phản phép] " : "✨ ";
            AddLogEntry(prefix + evt.Description, color);
        }

        private void OnRebirth(WorldRebirthEvent evt)
        {
            AddLogEntry($"🌍 Thế giới tái sinh! Chu kỳ {evt.NewCycle}. {evt.FadedGodIds.Count} thần đã biến mất.",
                new Color(1f, 0.8f, 0.2f));
        }

        private void AddLogEntry(string message, Color color = default)
        {
            if (eventLogContainer == null || eventLogItemPrefab == null) return;
            if (color == default) color = Color.white;

            var item = Instantiate(eventLogItemPrefab, eventLogContainer);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
                text.color = color;
            }

            _logItems.Enqueue(item);

            // Giới hạn số lượng log entries
            while (_logItems.Count > maxLogEntries)
            {
                var old = _logItems.Dequeue();
                if (old != null) Destroy(old);
            }

            // Auto scroll xuống dưới
            if (eventLogScrollRect != null)
                StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (eventLogScrollRect != null)
                eventLogScrollRect.verticalNormalizedPosition = 0f;
        }

        // ─── Toast Notification ──────────────────────────────────

        public void ShowToast(string message)
        {
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastCoroutine(message));
        }

        private IEnumerator ToastCoroutine(string message)
        {
            if (toastText) toastText.text = message;
            toastObject?.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastObject?.SetActive(false);
        }

        // ─── Game Over ───────────────────────────────────────────

        private void OnGameOver(GameEndEvent evt)
        {
            gameOverPanel?.SetActive(true);
            if (gameOverText)
            {
                var myId = GameManager.Instance?.MyGod?.Id;
                gameOverText.text = evt.WinnerGodId == myId
                    ? "🏆 Chiến Thắng!\nĐức tin của bạn đã chinh phục thế giới."
                    : "💀 Thất Bại\nTên bạn đã bị lãng quên.";
            }
        }

        private void OnReturnToLobby()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        // ─── Panel Management ────────────────────────────────────

        private void ShowPanel(GameObject panel)
        {
            _activePanel?.SetActive(false);
            _activePanel = panel;
            _activePanel?.SetActive(true);
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static Color GetDeltaColor(WorldEventType type) => type switch
        {
            WorldEventType.CivilizationCollapsed  => new Color(1f, 0.3f, 0.3f),
            WorldEventType.EvolutionOccurred      => new Color(0.3f, 1f, 0.8f),
            WorldEventType.ReligionEvent          => new Color(1f, 0.9f, 0.3f),
            WorldEventType.HolyWar                => new Color(1f, 0.5f, 0.1f),
            WorldEventType.WorldRebirth           => new Color(0.8f, 0.6f, 1f),
            WorldEventType.GodFaded               => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.white
        };
    }
}
