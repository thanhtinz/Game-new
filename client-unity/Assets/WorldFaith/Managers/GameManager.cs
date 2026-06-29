using System.Collections.Generic;
using UnityEngine;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.Managers
{
    /// <summary>
    /// Quản lý game state phía client. Subscribe events từ WorldFaithClient
    /// và thông báo cho các Manager khác (WorldRenderer, UIManager...).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ─── State ──────────────────────────────────────────────
        public GodDto MyGod { get; private set; }
        public WorldStateDto CurrentWorldState { get; private set; }
        public Dictionary<string, GodDto> Gods { get; } = new();
        public Dictionary<string, CivilizationDto> Civilizations { get; } = new();
        public Dictionary<string, ReligionDto> Religions { get; } = new();
        public long CurrentTick { get; private set; }
        public int CurrentCycle { get; private set; }

        // ─── Events cho UI ─────────────────────────────────────
        public event System.Action<WorldStateDto> OnWorldLoaded;
        public event System.Action<long, int> OnTick;
        public event System.Action<MiracleResultEvent> OnMiracleResult;
        public event System.Action<WorldRebirthEvent> OnRebirth;
        public event System.Action<GameEndEvent> OnGameOver;
        public event System.Action<string> OnNotification;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SubscribeToNetworkEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
        }

        // ─── Network Event Subscriptions ────────────────────────

        private void SubscribeToNetworkEvents()
        {
            var client = WorldFaithClient.Instance;
            if (client == null) return;

            client.OnWorldStateReceived += HandleWorldState;
            client.OnWorldTick += HandleWorldTick;
            client.OnMiracleResult += HandleMiracleResult;
            client.OnGodUpdate += HandleGodUpdate;
            client.OnCivilizationUpdate += HandleCivilizationUpdate;
            client.OnReligionUpdate += HandleReligionUpdate;
            client.OnWorldRebirth += HandleWorldRebirth;
            client.OnGameEnd += HandleGameEnd;
            client.OnJoinedWorld += HandleJoinedWorld;
            client.OnError += HandleError;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            var client = WorldFaithClient.Instance;
            if (client == null) return;

            client.OnWorldStateReceived -= HandleWorldState;
            client.OnWorldTick -= HandleWorldTick;
            client.OnMiracleResult -= HandleMiracleResult;
            client.OnGodUpdate -= HandleGodUpdate;
            client.OnCivilizationUpdate -= HandleCivilizationUpdate;
            client.OnReligionUpdate -= HandleReligionUpdate;
            client.OnWorldRebirth -= HandleWorldRebirth;
            client.OnGameEnd -= HandleGameEnd;
            client.OnJoinedWorld -= HandleJoinedWorld;
            client.OnError -= HandleError;
        }

        // ─── Handlers ───────────────────────────────────────────

        private void HandleWorldState(WorldStateDto state)
        {
            CurrentWorldState = state;
            CurrentTick = state.Tick;
            CurrentCycle = state.Cycle;

            Gods.Clear();
            foreach (var g in state.Gods) Gods[g.Id] = g;

            Civilizations.Clear();
            foreach (var c in state.Civilizations) Civilizations[c.Id] = c;

            Religions.Clear();
            foreach (var r in state.Religions) Religions[r.Id] = r;

            OnWorldLoaded?.Invoke(state);
            Debug.Log($"[GameManager] World state tải xong. Tick={state.Tick} Cycle={state.Cycle}");
        }

        private void HandleWorldTick(WorldTickEvent evt)
        {
            CurrentTick = evt.Tick;
            CurrentCycle = evt.Cycle;
            OnTick?.Invoke(evt.Tick, evt.Cycle);
        }

        private void HandleMiracleResult(MiracleResultEvent evt)
        {
            OnMiracleResult?.Invoke(evt);

            if (evt.Success)
            {
                string msg = evt.WasCountered
                    ? $"Miracle bị phản! {evt.Description}"
                    : evt.Description;
                OnNotification?.Invoke(msg);
            }
            else
            {
                OnNotification?.Invoke($"Miracle thất bại: {evt.Description}");
            }
        }

        private void HandleGodUpdate(GodUpdateEvent evt)
        {
            if (Gods.TryGetValue(evt.GodId, out var god))
            {
                god.Faith = evt.Faith;
                god.Trust = evt.Trust;
                god.Fear = evt.Fear;
                god.FollowerCount = evt.FollowerCount;
                god.IsAlive = evt.IsAlive;
            }

            // Cập nhật MyGod nếu là god của mình
            if (MyGod != null && MyGod.Id == evt.GodId)
            {
                MyGod.Faith = evt.Faith;
                MyGod.Trust = evt.Trust;
                MyGod.Fear = evt.Fear;
                MyGod.FollowerCount = evt.FollowerCount;
            }
        }

        private void HandleCivilizationUpdate(CivilizationUpdateEvent evt)
        {
            if (Civilizations.TryGetValue(evt.CivilizationId, out var civ))
            {
                civ.Population = evt.Population;
                civ.State = evt.State;
                if (evt.NewRulingReligionId != null)
                    civ.RulingReligionId = evt.NewRulingReligionId;
            }

            if (evt.Collapsed)
                OnNotification?.Invoke($"Civilization {evt.Name} đã sụp đổ!");
        }

        private void HandleReligionUpdate(ReligionUpdateEvent evt)
        {
            if (Religions.TryGetValue(evt.ReligionId, out var religion))
            {
                religion.FollowerCount = evt.FollowerCount;
                if (evt.Erased)
                {
                    Religions.Remove(evt.ReligionId);
                    OnNotification?.Invoke("Một tôn giáo đã bị xóa sổ khỏi thế giới!");
                }
            }
        }

        private void HandleWorldRebirth(WorldRebirthEvent evt)
        {
            CurrentCycle = evt.NewCycle;
            OnRebirth?.Invoke(evt);
            OnNotification?.Invoke($"Thế giới tái sinh! Chu kỳ {evt.NewCycle}. {evt.FadedGodIds.Count} thần đã biến mất.");
        }

        private void HandleGameEnd(GameEndEvent evt)
        {
            OnGameOver?.Invoke(evt);
            string result = evt.WinnerGodId == MyGod?.Id ? "Bạn đã chiến thắng!" : "Bạn đã thua!";
            OnNotification?.Invoke(result);
        }

        private void HandleJoinedWorld(GodDto god)
        {
            MyGod = god;
            Gods[god.Id] = god;
            Debug.Log($"[GameManager] Đã vào world với god: {god.Name} ({god.Archetype})");
        }

        private void HandleError(ErrorEvent err)
        {
            OnNotification?.Invoke($"Lỗi: {err.Message}");
        }

        // ─── Public Actions (UI gọi) ─────────────────────────────

        public async void PerformMiracle(MiracleType miracle, int x, int y, string targetCivId = null)
        {
            await WorldFaithClient.Instance.PerformMiracleAsync(new PerformMiracleRequest
            {
                Miracle = miracle,
                TargetX = x,
                TargetY = y,
                TargetCivilizationId = targetCivId
            });
        }

        public async void JoinWorld(string worldId, string godName, GodArchetype archetype)
        {
            await WorldFaithClient.Instance.JoinWorldAsync(new JoinWorldRequest
            {
                WorldId = worldId,
                GodName = godName,
                Archetype = archetype
            });
        }

        public async void CreateWorld(string worldName, GameMode mode = GameMode.Sandbox)
        {
            await WorldFaithClient.Instance.CreateWorldAsync(new CreateWorldRequest
            {
                WorldName = worldName,
                Mode = mode,
                MaxGods = 4,
                WorldWidth = 64,
                WorldHeight = 64,
                VictoryCondition = VictoryCondition.LastSurvivingGod
            });
        }
    }
}
