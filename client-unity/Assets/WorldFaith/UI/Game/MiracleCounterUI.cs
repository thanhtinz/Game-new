using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Counter UI — hiển thị miracle của rival gods đang pending.
    /// God có N giây để phản phép bằng cách chọn counter miracle.
    /// Countdown bar chạy realtime.
    /// </summary>
    public class MiracleCounterUI : MonoBehaviour
    {
        [Header("Counter Panel")]
        [SerializeField] private GameObject counterPanel;
        [SerializeField] private Transform pendingContainer;
        [SerializeField] private GameObject pendingItemPrefab;

        [Header("Counter Selection")]
        [SerializeField] private GameObject counterSelectPanel;
        [SerializeField] private Transform counterBtnContainer;
        [SerializeField] private GameObject counterBtnPrefab;
        [SerializeField] private TextMeshProUGUI counterTargetText;
        [SerializeField] private TextMeshProUGUI counterFaithText;
        [SerializeField] private Button cancelCounterBtn;

        private class PendingMiracleEntry
        {
            public string EventId;
            public string GodName;
            public MiracleType Miracle;
            public float RemainingSeconds;
            public GameObject ItemGo;
            public Slider TimerSlider;
            public float TotalSeconds;
        }

        private readonly List<PendingMiracleEntry> _pending = new();
        private string _counteringEventId;

        // Counter window từ config (default 5s)
        private float _counterWindow = 5f;

        // Miracles có thể counter với (không thể counter bằng T3 nếu chưa có)
        private static readonly List<(MiracleType miracle, float cost)> CounterOptions = new()
        {
            (MiracleType.Rain,          10f),
            (MiracleType.BlessHarvest,  15f),
            (MiracleType.Storm,         30f),
            (MiracleType.Earthquake,    40f),
            (MiracleType.Curse,         25f),
            (MiracleType.DivineVoice,   20f),
            (MiracleType.Revelation,    60f),
        };

        private void Start()
        {
            cancelCounterBtn?.onClick.AddListener(() =>
            {
                counterSelectPanel?.SetActive(false);
                _counteringEventId = null;
            });

            counterPanel?.SetActive(false);
            counterSelectPanel?.SetActive(false);

            // Subscribe miracle events
            var client = WorldFaithClient.Instance;
            if (client != null)
                client.OnMiracleResult += OnMiracleResult;
        }

        private void OnDestroy()
        {
            var client = WorldFaithClient.Instance;
            if (client != null)
                client.OnMiracleResult -= OnMiracleResult;
        }

        private void Update()
        {
            if (_pending.Count == 0) return;

            float dt = Time.deltaTime;
            var toRemove = new List<PendingMiracleEntry>();

            foreach (var entry in _pending)
            {
                entry.RemainingSeconds -= dt;
                if (entry.TimerSlider != null)
                    entry.TimerSlider.value = entry.RemainingSeconds / entry.TotalSeconds;

                // Update countdown text
                var texts = entry.ItemGo?.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts != null && texts.Length > 2)
                    texts[2].text = $"{entry.RemainingSeconds:F1}s";

                if (entry.RemainingSeconds <= 0f)
                    toRemove.Add(entry);
            }

            foreach (var expired in toRemove)
            {
                if (expired.ItemGo != null) Destroy(expired.ItemGo);
                _pending.Remove(expired);
                if (_counteringEventId == expired.EventId)
                {
                    counterSelectPanel?.SetActive(false);
                    _counteringEventId = null;
                }
            }

            counterPanel?.SetActive(_pending.Count > 0);
        }

        // ─── Event Handler ───────────────────────────────────────

        private void OnMiracleResult(MiracleResultEvent evt)
        {
            // Chỉ hiển thị miracle của rival gods (không phải của mình)
            var myGodId = GameManager.Instance?.MyGod?.Id ?? "";
            if (evt.GodId == myGodId) return;
            if (!evt.Success || evt.WasCountered) return;
            if (string.IsNullOrEmpty(evt.MiracleEventId)) return;

            // Kiểm tra đây có phải miracle có thể counter không
            var unlockedMiracles = GameManager.Instance?.MyGod?.UnlockedMiracles ?? new List<string>();
            bool canCounter = CounterOptions.Any(c => unlockedMiracles.Contains(c.miracle.ToString()));
            if (!canCounter) return;

            var godName = GameManager.Instance?.Gods.TryGetValue(evt.GodId, out var god) == true
                ? god.Name : "Rival God";

            AddPendingMiracle(evt.MiracleEventId, godName, evt.Miracle);
        }

        // ─── Pending Display ─────────────────────────────────────

        private void AddPendingMiracle(string eventId, string godName, MiracleType miracle)
        {
            if (pendingContainer == null || pendingItemPrefab == null) return;

            var item  = Instantiate(pendingItemPrefab, pendingContainer);
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            var slider = item.GetComponentInChildren<Slider>();
            var btn    = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>();

            if (texts.Length > 0) texts[0].text = $"⚡ {godName}";
            if (texts.Length > 1) texts[1].text = GetMiracleName(miracle);
            if (texts.Length > 2) texts[2].text = $"{_counterWindow:F0}s";

            if (slider != null) { slider.value = 1f; slider.maxValue = 1f; }

            btn?.onClick.AddListener(() => OpenCounterSelect(eventId, godName, miracle));

            _pending.Add(new PendingMiracleEntry
            {
                EventId          = eventId,
                GodName          = godName,
                Miracle          = miracle,
                RemainingSeconds = _counterWindow,
                TotalSeconds     = _counterWindow,
                ItemGo           = item,
                TimerSlider      = slider
            });
        }

        // ─── Counter Selection ───────────────────────────────────

        private void OpenCounterSelect(string eventId, string godName, MiracleType targetMiracle)
        {
            _counteringEventId = eventId;
            counterSelectPanel?.SetActive(true);

            if (counterTargetText)
                counterTargetText.text = $"Phản phép: {godName} dùng {GetMiracleName(targetMiracle)}";

            float myFaith = GameManager.Instance?.MyGod?.Faith ?? 0f;
            if (counterFaithText) counterFaithText.text = $"Faith hiện tại: {myFaith:F0}";

            // Clear old buttons
            if (counterBtnContainer != null)
                foreach (Transform child in counterBtnContainer)
                    Destroy(child.gameObject);

            var unlockedMiracles = GameManager.Instance?.MyGod?.UnlockedMiracles ?? new List<string>();

            foreach (var (miracle, cost) in CounterOptions)
            {
                if (!unlockedMiracles.Contains(miracle.ToString())) continue;

                var btn = Instantiate(counterBtnPrefab, counterBtnContainer);
                var texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
                bool canAfford = myFaith >= cost;

                if (texts.Length > 0) texts[0].text = GetMiracleName(miracle);
                if (texts.Length > 1) texts[1].text = $"{cost} Faith";

                var image = btn.GetComponent<Image>();
                if (image) image.color = canAfford
                    ? new Color(0.2f, 0.5f, 0.8f)
                    : new Color(0.4f, 0.4f, 0.4f);

                var capturedMiracle = miracle;
                var capturedEventId = eventId;
                btn.GetComponent<Button>()?.onClick.AddListener(() =>
                {
                    if (canAfford) PerformCounter(capturedEventId, capturedMiracle);
                });
            }
        }

        private async void PerformCounter(string eventId, MiracleType counterMiracle)
        {
            counterSelectPanel?.SetActive(false);
            _counteringEventId = null;

            await WorldFaithClient.Instance.CounterMiracleAsync(new CounterMiracleRequest
            {
                MiracleEventId = eventId,
                CounterMiracle = counterMiracle
            });

            // Remove from pending
            var entry = _pending.Find(p => p.EventId == eventId);
            if (entry != null)
            {
                if (entry.ItemGo != null) Destroy(entry.ItemGo);
                _pending.Remove(entry);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static string GetMiracleName(MiracleType m) => m switch
        {
            MiracleType.Rain               => "☔ Mưa",
            MiracleType.Dream              => "💭 Giấc Mơ",
            MiracleType.BlessHarvest       => "🌾 Ban Phước",
            MiracleType.HealFollower       => "💚 Chữa Lành",
            MiracleType.Omen               => "🔮 Điềm Báo",
            MiracleType.Storm              => "⛈ Bão",
            MiracleType.Earthquake         => "🌋 Động Đất",
            MiracleType.Curse              => "💀 Lời Nguyền",
            MiracleType.Portal             => "🌀 Cổng Thần",
            MiracleType.DivineVoice        => "📢 Tiếng Thần",
            MiracleType.Volcano            => "🌋 Núi Lửa",
            MiracleType.DemonInvasion      => "😈 Quỷ Xâm Chiếm",
            MiracleType.DivineBeastCreation=> "🐉 Thần Thú",
            MiracleType.Revelation         => "✨ Khải Thị",
            MiracleType.HolyWar            => "⚔️ Thánh Chiến",
            _ => m.ToString()
        };
    }
}
