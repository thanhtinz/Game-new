using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace WorldFaith.Client.UI.Game
{
    // ══════════════════════════════════════════════
    //  LEADERBOARD PANEL
    // ══════════════════════════════════════════════

    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerId;
        public string displayName;
        public int rating;
        public int totalWins;
        public int totalGames;
        public float winRate;
        public string favoriteArchetype;
        public long totalFollowers;
    }

    public class LeaderboardPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleBtn;
        [SerializeField] private Button closeBtn;

        [Header("Tabs")]
        [SerializeField] private Button tabRatingBtn;
        [SerializeField] private Button tabWinsBtn;
        [SerializeField] private Button tabFollowersBtn;

        [Header("List")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject entryPrefab;

        [Header("My Rank")]
        [SerializeField] private TextMeshProUGUI myRankText;
        [SerializeField] private TextMeshProUGUI myRatingText;
        [SerializeField] private TextMeshProUGUI myWinRateText;

        [Header("Loading")]
        [SerializeField] private GameObject loadingIndicator;

        private string _serverUrl = "http://localhost:5000";
        private string _currentStat = "rating";
        private readonly List<GameObject> _entries = new();

        private void Start()
        {
            _serverUrl = PlayerPrefs.GetString("wf_server_url", _serverUrl);

            toggleBtn?.onClick.AddListener(TogglePanel);
            closeBtn?.onClick.AddListener(() => panel?.SetActive(false));
            tabRatingBtn?.onClick.AddListener(() => LoadLeaderboard("rating"));
            tabWinsBtn?.onClick.AddListener(() => LoadLeaderboard("wins"));
            tabFollowersBtn?.onClick.AddListener(() => LoadLeaderboard("followers"));

            panel?.SetActive(false);
        }

        private void TogglePanel()
        {
            bool active = !(panel?.activeSelf ?? false);
            panel?.SetActive(active);
            if (active) LoadLeaderboard(_currentStat);
        }

        private void LoadLeaderboard(string stat)
        {
            _currentStat = stat;
            StartCoroutine(FetchLeaderboard(stat));

            // My stats
            var myId = Managers.AuthManager.Instance?.PlayerId;
            if (!string.IsNullOrEmpty(myId))
                StartCoroutine(FetchMyStats(myId));
        }

        private IEnumerator FetchLeaderboard(string stat)
        {
            loadingIndicator?.SetActive(true);

            using var req = UnityWebRequest.Get($"{_serverUrl}/api/leaderboard/by/{stat}?limit=20");
            var token = Managers.AuthManager.Instance?.AccessToken;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return req.SendWebRequest();
            loadingIndicator?.SetActive(false);

            if (req.result != UnityWebRequest.Result.Success) yield break;

            var entries = JsonUtility.FromJson<LeaderboardEntryList>($"{{\"items\":{req.downloadHandler.text}}}");
            RefreshList(entries?.items ?? new List<LeaderboardEntry>());
        }

        private IEnumerator FetchMyStats(string playerId)
        {
            using var req = UnityWebRequest.Get($"{_serverUrl}/api/leaderboard/player/{playerId}");
            var token = Managers.AuthManager.Instance?.AccessToken;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            var stats = JsonUtility.FromJson<LeaderboardEntry>(req.downloadHandler.text);
            if (stats == null) yield break;

            if (myRankText)    myRankText.text    = $"Rank #{stats.rank}";
            if (myRatingText)  myRatingText.text  = $"⭐ {stats.rating} ELO";
            if (myWinRateText) myWinRateText.text = $"Win rate: {stats.winRate:P0}";
        }

        private void RefreshList(List<LeaderboardEntry> entries)
        {
            foreach (var go in _entries) Destroy(go);
            _entries.Clear();

            if (listContainer == null || entryPrefab == null) return;

            string myId = Managers.AuthManager.Instance?.PlayerId ?? "";

            foreach (var entry in entries)
            {
                var item = Instantiate(entryPrefab, listContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

                bool isMe = entry.playerId == myId;

                string rankIcon = entry.rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{entry.rank}" };

                if (texts.Length > 0) texts[0].text = rankIcon;
                if (texts.Length > 1)
                {
                    texts[1].text = entry.displayName;
                    if (isMe) texts[1].fontStyle = TMPro.FontStyles.Bold;
                }
                if (texts.Length > 2) texts[2].text = _currentStat switch
                {
                    "wins"      => $"{entry.totalWins}W",
                    "followers" => $"{entry.totalFollowers:N0}",
                    _           => $"{entry.rating} ELO"
                };
                if (texts.Length > 3) texts[3].text = $"[{entry.favoriteArchetype}]";

                // Highlight bản thân
                var img = item.GetComponent<Image>();
                if (img != null && isMe)
                    img.color = new Color(0.3f, 0.2f, 0.5f, 0.5f);

                _entries.Add(item);
            }
        }

        [Serializable]
        private class LeaderboardEntryList { public List<LeaderboardEntry> items; }
    }

    // ══════════════════════════════════════════════
    //  PUSH NOTIFICATION MANAGER (Mobile)
    // ══════════════════════════════════════════════

    /// <summary>
    /// Quản lý local notifications cho mobile.
    /// Dùng Unity's LocalNotification system (không cần thư viện ngoài).
    /// Khi app vào background: schedule notifications cho các sự kiện quan trọng.
    /// </summary>
    public class PushNotificationManager : MonoBehaviour
    {
        public static PushNotificationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableNotifications = true;
        [SerializeField] private int rebirthReminderMinutes = 5;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!enableNotifications) return;
            RequestPermission();

            // Subscribe game events
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnRebirth += OnWorldRebirth;
                Managers.GameManager.Instance.OnGameOver += _ => OnGameOver();
            }
        }

        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnRebirth -= OnWorldRebirth;
            }
        }

        // ─── Permission ──────────────────────────────────────────

        private void RequestPermission()
        {
#if UNITY_IOS
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(
                UnityEngine.iOS.NotificationType.Alert |
                UnityEngine.iOS.NotificationType.Badge |
                UnityEngine.iOS.NotificationType.Sound);
#elif UNITY_ANDROID
            // Android 13+ cần runtime permission
            // Unity 2022+ có Permission.RequestUserPermission
            // Đây là placeholder - implement theo Unity version
            Debug.Log("[Push] Android notification permission requested");
#endif
        }

        // ─── Schedule Notifications ──────────────────────────────

        private void OnWorldRebirth(WorldFaith.Shared.Contracts.WorldRebirthEvent evt)
        {
            // Khi rebirth: nhắc người chơi quay lại sau X phút
            ScheduleLocalNotification(
                title: "⚡ WorldFaith - Thế giới tái sinh!",
                body: $"Chu kỳ {evt.NewCycle} bắt đầu. Quay lại để bảo vệ đức tin của bạn!",
                delayMinutes: rebirthReminderMinutes
            );
        }

        private void OnGameOver()
        {
            ScheduleLocalNotification(
                title: "🌍 WorldFaith - Ván đấu kết thúc",
                body: "Tham gia ván đấu mới ngay bây giờ!",
                delayMinutes: 2
            );
        }

        public void NotifyFaithLow()
        {
            // Gọi khi faith xuống thấp
            ScheduleLocalNotification(
                title: "⚠️ WorldFaith - Faith cạn kiệt!",
                body: "Tín đồ của bạn đang mất đức tin. Hãy thực hiện miracle ngay!",
                delayMinutes: 0
            );
        }

        public void NotifyFollowerAttacked(string civName)
        {
            ScheduleLocalNotification(
                title: "🔥 WorldFaith - Tín đồ bị tấn công!",
                body: $"{civName} đang bị tấn công. Bảo vệ tín đồ của bạn!",
                delayMinutes: 0
            );
        }

        // ─── Platform Implementation ─────────────────────────────

        private void ScheduleLocalNotification(string title, string body, int delayMinutes)
        {
            if (!enableNotifications) return;

#if UNITY_IOS && !UNITY_EDITOR
            var notification = new UnityEngine.iOS.LocalNotification
            {
                alertTitle = title,
                alertBody = body,
                fireDate = DateTime.Now.AddMinutes(delayMinutes),
                applicationIconBadgeNumber = 1
            };
            UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(notification);
            Debug.Log($"[Push] iOS notification scheduled: {title}");

#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android local notification via Unity Notifications package
            // Requires: com.unity.mobile.notifications
            // Đây là stub - cần install package
            Debug.Log($"[Push] Android notification would schedule: {title} in {delayMinutes}m");

#else
            // Editor: chỉ log
            Debug.Log($"[Push] [Editor] Notification: {title} | {body} | in {delayMinutes}m");
#endif
        }

        // ─── App Lifecycle ───────────────────────────────────────

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused) return;

            // App vào background: schedule reminder sau 1 giờ nếu đang in-game
            if (Managers.GameManager.Instance?.MyGod != null)
            {
                ScheduleLocalNotification(
                    title: "⚡ WorldFaith đang chờ bạn",
                    body: "Tín đồ của bạn cần được dẫn dắt. Quay lại ngay!",
                    delayMinutes: 60
                );
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
#if UNITY_IOS && !UNITY_EDITOR
            // Clear badge khi mở app
            UnityEngine.iOS.NotificationServices.ClearLocalNotifications();
            UnityEngine.iOS.NotificationServices.applicationIconBadgeNumber = 0;
#endif
        }
    }
}
