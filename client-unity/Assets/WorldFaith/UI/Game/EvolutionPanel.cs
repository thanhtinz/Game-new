using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Panel hiển thị tất cả evolution entities trong world.
    /// Cho phép god chọn và evolve entities bằng Faith.
    /// </summary>
    public class EvolutionPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleBtn;
        [SerializeField] private Button closeBtn;

        [Header("Entity List")]
        [SerializeField] private Transform entityListContainer;
        [SerializeField] private GameObject entityItemPrefab;
        [SerializeField] private TMP_Dropdown stageFilter;

        [Header("Entity Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TextMeshProUGUI entityNameText;
        [SerializeField] private TextMeshProUGUI entityStageText;
        [SerializeField] private TextMeshProUGUI entityPowerText;
        [SerializeField] private TextMeshProUGUI evolvePointsText;
        [SerializeField] private TextMeshProUGUI evolveCostText;
        [SerializeField] private Button evolveBtn;
        [SerializeField] private Button trackBtn;
        [SerializeField] private Button closeDetailBtn;

        [Header("World Entities (runtime)")]
        private readonly List<EvolutionEntityDto> _entities = new();
        private EvolutionEntityDto _selectedEntity;
        private readonly List<GameObject> _entityItems = new();

        // Stage icons Unicode (dùng khi không có sprite)
        private static readonly Dictionary<EvolutionStage, string> StageIcons = new()
        {
            { EvolutionStage.WildAnimal,        "🐾" },
            { EvolutionStage.DivineBeast,        "🐉" },
            { EvolutionStage.CelestialGuardian,  "⭐" },
            { EvolutionStage.HumanHero,          "⚔️" },
            { EvolutionStage.Saint,              "✨" },
            { EvolutionStage.FallenDemonLord,    "💀" },
            { EvolutionStage.Monster,            "👾" },
            { EvolutionStage.Titan,              "🗿" },
            { EvolutionStage.ApocalypticEntity,  "☄️" },
        };

        private static readonly Dictionary<EvolutionStage, Color> StageColors = new()
        {
            { EvolutionStage.WildAnimal,        new Color(0.6f, 0.8f, 0.3f) },
            { EvolutionStage.DivineBeast,        new Color(0.2f, 0.7f, 1f) },
            { EvolutionStage.CelestialGuardian,  new Color(1f, 0.9f, 0.2f) },
            { EvolutionStage.HumanHero,          new Color(0.9f, 0.6f, 0.2f) },
            { EvolutionStage.Saint,              new Color(1f, 1f, 0.8f) },
            { EvolutionStage.FallenDemonLord,    new Color(0.6f, 0.1f, 0.8f) },
            { EvolutionStage.Monster,            new Color(0.7f, 0.2f, 0.2f) },
            { EvolutionStage.Titan,              new Color(0.5f, 0.3f, 0.1f) },
            { EvolutionStage.ApocalypticEntity,  new Color(0.9f, 0.1f, 0.1f) },
        };

        private void Start()
        {
            toggleBtn?.onClick.AddListener(TogglePanel);
            closeBtn?.onClick.AddListener(() => panel?.SetActive(false));
            closeDetailBtn?.onClick.AddListener(() => detailPanel?.SetActive(false));
            evolveBtn?.onClick.AddListener(OnEvolveClick);
            trackBtn?.onClick.AddListener(OnTrackClick);
            stageFilter?.onValueChanged.AddListener(_ => RefreshEntityList());

            panel?.SetActive(false);
            detailPanel?.SetActive(false);

            // Subscribe world state để lấy entities
            if (GameManager.Instance != null)
                GameManager.Instance.OnWorldLoaded += OnWorldLoaded;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnWorldLoaded -= OnWorldLoaded;
        }

        private void OnWorldLoaded(WorldStateDto state)
        {
            _entities.Clear();
            _entities.AddRange(state.Entities);
            RefreshEntityList();
        }

        // ─── Panel Toggle ────────────────────────────────────────

        private void TogglePanel()
        {
            bool active = !(panel?.activeSelf ?? false);
            panel?.SetActive(active);
            if (active) RefreshEntityList();
        }

        // ─── Entity List ─────────────────────────────────────────

        private void RefreshEntityList()
        {
            foreach (var item in _entityItems) Destroy(item);
            _entityItems.Clear();

            if (entityListContainer == null || entityItemPrefab == null) return;

            // Filter theo stage
            var filtered = _entities;
            if (stageFilter != null && stageFilter.value > 0)
            {
                var targetStage = (EvolutionStage)(stageFilter.value - 1);
                filtered = _entities.FindAll(e => e.Stage == targetStage.ToString());
            }

            // Sort: apex trước
            filtered.Sort((a, b) => string.Compare(b.Stage, a.Stage));

            foreach (var entity in filtered)
            {
                var item = Instantiate(entityItemPrefab, entityListContainer);
                item.name = entity.Id;

                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                {
                    var icon = StageIcons.TryGetValue(ParseStage(entity.Stage), out var ic) ? ic : "?";
                    texts[0].text = $"{icon} {entity.Name}";
                }
                if (texts.Length > 1) texts[1].text = entity.Stage;
                if (texts.Length > 2) texts[2].text = $"⚡{entity.Power:F0}";

                // Màu theo stage
                var img = item.GetComponent<Image>();
                if (img != null && StageColors.TryGetValue(ParseStage(entity.Stage), out var color))
                    img.color = new Color(color.r, color.g, color.b, 0.3f);

                var btn = item.GetComponent<Button>();
                var capturedEntity = entity;
                btn?.onClick.AddListener(() => ShowEntityDetail(capturedEntity));

                _entityItems.Add(item);
            }
        }

        // ─── Entity Detail ───────────────────────────────────────

        private void ShowEntityDetail(EvolutionEntityDto entity)
        {
            _selectedEntity = entity;
            detailPanel?.SetActive(true);

            if (entityNameText) entityNameText.text = entity.Name;
            if (entityStageText)
            {
                var icon = StageIcons.TryGetValue(ParseStage(entity.Stage), out var ic) ? ic : "";
                entityStageText.text = $"{icon} {entity.Stage}";
                if (StageColors.TryGetValue(ParseStage(entity.Stage), out var color))
                    entityStageText.color = color;
            }
            if (entityPowerText) entityPowerText.text = $"Sức mạnh: {entity.Power:F0}";

            // Kiểm tra god influence
            var myGodId = GameManager.Instance?.MyGod?.Id ?? "";
            bool isMyEntity = entity.GodInfluenceId == myGodId;

            // Evolve cost = 50 Faith
            float cost = 50f;
            float myFaith = GameManager.Instance?.MyGod?.Faith ?? 0f;
            bool canEvolve = myFaith >= cost && IsEvolvable(entity.Stage);

            if (evolveCostText) evolveCostText.text = $"Chi phí: 50 Faith";
            if (evolveBtn)
            {
                evolveBtn.interactable = canEvolve;
                var btnText = evolveBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText)
                    btnText.text = !IsEvolvable(entity.Stage) ? "Đã Max" :
                                   !canEvolve ? "Không đủ Faith" : "Tiến Hóa";
            }

            if (trackBtn)
            {
                var btnText = trackBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText) btnText.text = "Theo dõi";
            }
        }

        // ─── Actions ─────────────────────────────────────────────

        private async void OnEvolveClick()
        {
            if (_selectedEntity == null) return;
            await WorldFaithClient.Instance.EvolveEntityAsync(_selectedEntity.Id);
            detailPanel?.SetActive(false);
        }

        private void OnTrackClick()
        {
            if (_selectedEntity == null) return;
            // Di chuyển camera đến entity
            var worldPos = new Vector3(_selectedEntity.X, 0f, _selectedEntity.Y);
            Camera.main?.GetComponent<CameraController>()?.CenterOn(worldPos);
            detailPanel?.SetActive(false);
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static EvolutionStage ParseStage(string stageName)
            => System.Enum.TryParse<EvolutionStage>(stageName, out var s) ? s : EvolutionStage.WildAnimal;

        private static bool IsEvolvable(string stageName)
        {
            var stage = ParseStage(stageName);
            return stage is not (EvolutionStage.CelestialGuardian
                or EvolutionStage.FallenDemonLord
                or EvolutionStage.ApocalypticEntity);
        }
    }
}
