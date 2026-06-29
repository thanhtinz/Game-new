using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// World Map UI — overlay 2D minimap + tile info popup.
    /// Hiển thị: tiles (màu theo biome), civ territories, religion spread, entity positions.
    /// Click tile → popup info → quick actions (miracle tại đây, xây temple...).
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        [Header("Map Panel")]
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private Button toggleMapBtn;
        [SerializeField] private Button closeMapBtn;
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private GameObject tilePixelPrefab;   // 1x1 Image pixel
        [SerializeField] private int mapDisplaySize = 256;     // px

        [Header("Legend")]
        [SerializeField] private GameObject legendPanel;
        [SerializeField] private Button legendToggleBtn;

        [Header("Layer Toggles")]
        [SerializeField] private Toggle showCivLayerToggle;
        [SerializeField] private Toggle showReligionLayerToggle;
        [SerializeField] private Toggle showEntityLayerToggle;

        [Header("Tile Info Popup")]
        [SerializeField] private GameObject tileInfoPopup;
        [SerializeField] private TextMeshProUGUI tileTypeText;
        [SerializeField] private TextMeshProUGUI tileCivText;
        [SerializeField] private TextMeshProUGUI tileReligionText;
        [SerializeField] private TextMeshProUGUI tilePopText;
        [SerializeField] private Button miracleHereBtn;
        [SerializeField] private Button closePopupBtn;

        [Header("Entity Markers")]
        [SerializeField] private Transform entityMarkerContainer;
        [SerializeField] private GameObject entityMarkerPrefab;

        // Map state
        private int _worldWidth, _worldHeight;
        private readonly Dictionary<string, Image> _tileImages = new();
        private readonly List<GameObject> _entityMarkers = new();
        private WorldTileDto _selectedTile;
        private bool _showCiv = true, _showReligion = true, _showEntity = true;

        // Biome colors
        private static readonly Dictionary<TileType, Color> BiomeColors = new()
        {
            { TileType.Grassland, new Color(0.4f, 0.7f, 0.3f) },
            { TileType.Forest,    new Color(0.2f, 0.5f, 0.2f) },
            { TileType.Mountain,  new Color(0.6f, 0.6f, 0.6f) },
            { TileType.Desert,    new Color(0.9f, 0.8f, 0.4f) },
            { TileType.Tundra,    new Color(0.8f, 0.9f, 0.95f) },
            { TileType.Water,     new Color(0.2f, 0.5f, 0.8f) },
            { TileType.Volcano,   new Color(0.8f, 0.2f, 0.1f) },
            { TileType.Sacred,    new Color(0.9f, 0.8f, 0.2f) },
        };

        // Civ colors (phân biệt theo state)
        private static readonly Dictionary<CivilizationState, Color> CivStateColors = new()
        {
            { CivilizationState.Tribal,    new Color(0.6f, 0.4f, 0.2f, 0.6f) },
            { CivilizationState.Kingdom,   new Color(0.2f, 0.5f, 0.8f, 0.6f) },
            { CivilizationState.Empire,    new Color(0.8f, 0.7f, 0.1f, 0.6f) },
            { CivilizationState.Collapsing,new Color(0.8f, 0.2f, 0.2f, 0.6f) },
            { CivilizationState.Fallen,    new Color(0.3f, 0.3f, 0.3f, 0.4f) },
        };

        private void Start()
        {
            toggleMapBtn?.onClick.AddListener(ToggleMap);
            closeMapBtn?.onClick.AddListener(() => mapPanel?.SetActive(false));
            legendToggleBtn?.onClick.AddListener(() =>
                legendPanel?.SetActive(!(legendPanel?.activeSelf ?? false)));
            closePopupBtn?.onClick.AddListener(() => tileInfoPopup?.SetActive(false));

            showCivLayerToggle?.onValueChanged.AddListener(v => { _showCiv = v; RefreshLayers(); });
            showReligionLayerToggle?.onValueChanged.AddListener(v => { _showReligion = v; RefreshLayers(); });
            showEntityLayerToggle?.onValueChanged.AddListener(v => { _showEntity = v; RefreshEntityMarkers(); });

            mapPanel?.SetActive(false);
            tileInfoPopup?.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded += BuildMap;
                gm.OnTick += (_, _) => RefreshLayers();
                gm.OnCivilizationUpdate += _ => RefreshLayers();
                gm.OnReligionUpdate += _ => RefreshLayers();
            }
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded -= BuildMap;
                gm.OnCivilizationUpdate -= _ => RefreshLayers();
                gm.OnReligionUpdate -= _ => RefreshLayers();
            }
        }

        // ─── Map Build ───────────────────────────────────────────

        private void BuildMap(WorldStateDto state)
        {
            _worldWidth  = state.Width;
            _worldHeight = state.Height;

            // Clear old tiles
            foreach (var img in _tileImages.Values)
                if (img != null) Destroy(img.gameObject);
            _tileImages.Clear();

            if (mapContainer == null || tilePixelPrefab == null) return;

            float tileW = (float)mapDisplaySize / _worldWidth;
            float tileH = (float)mapDisplaySize / _worldHeight;

            foreach (var tile in state.ChangedTiles)
            {
                var go  = Instantiate(tilePixelPrefab, mapContainer);
                var img = go.GetComponent<Image>();
                if (img == null) continue;

                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(tile.X * tileW, tile.Y * tileH);
                rt.sizeDelta        = new Vector2(tileW, tileH);

                img.color = BiomeColors.TryGetValue(tile.Type, out var c) ? c : Color.gray;

                // Click handler
                var capturedTile = tile;
                var btn = go.AddComponent<Button>();
                btn.onClick.AddListener(() => OnTileClicked(capturedTile));

                _tileImages[$"{tile.X}_{tile.Y}"] = img;
            }

            RefreshLayers();
            RefreshEntityMarkers();
        }

        // ─── Layer Refresh ───────────────────────────────────────

        private void RefreshLayers()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Build lookup: tileKey → civState, tileKey → religionGodId
            var civTiles      = new Dictionary<string, (CivilizationState state, string rulingReligionId)>();
            var religionTiles = new Dictionary<string, string>(); // tileKey → godId

            if (_showCiv)
            {
                foreach (var civ in gm.Civilizations.Values)
                    foreach (var coords in civ.ControlledTiles)
                        if (coords.Length >= 2)
                            civTiles[$"{coords[0]}_{coords[1]}"] = (civ.State, civ.RulingReligionId ?? "");
            }

            if (_showReligion)
            {
                foreach (var rel in gm.Religions.Values)
                    if (!rel.IsHidden)
                        foreach (var civId in rel.CivilizationIds)
                            if (gm.Civilizations.TryGetValue(civId, out var civ))
                                foreach (var coords in civ.ControlledTiles)
                                    if (coords.Length >= 2)
                                        religionTiles[$"{coords[0]}_{coords[1]}"] = rel.GodId;
            }

            // Apply colors
            foreach (var (key, img) in _tileImages)
            {
                if (img == null) continue;

                Color baseColor = img.color;
                // Reset to biome color then blend
                if (civTiles.TryGetValue(key, out var civInfo))
                {
                    var civColor = CivStateColors.TryGetValue(civInfo.state, out var cc)
                        ? cc : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    img.color = Color.Lerp(baseColor, civColor, 0.5f);
                }

                if (religionTiles.TryGetValue(key, out var godId))
                {
                    // Màu dựa theo god archetype
                    var relColor = GetGodColor(godId, gm);
                    img.color = Color.Lerp(img.color, relColor, 0.3f);
                }
            }
        }

        private void RefreshEntityMarkers()
        {
            foreach (var m in _entityMarkers) Destroy(m);
            _entityMarkers.Clear();

            if (!_showEntity || entityMarkerContainer == null || entityMarkerPrefab == null) return;

            var gm = GameManager.Instance;
            if (gm?.CurrentWorldState == null) return;

            float tileW = (float)mapDisplaySize / _worldWidth;
            float tileH = (float)mapDisplaySize / _worldHeight;

            foreach (var entity in gm.CurrentWorldState.Entities)
            {
                var marker = Instantiate(entityMarkerPrefab, entityMarkerContainer);
                var rt     = marker.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(entity.X * tileW, entity.Y * tileH);

                var texts = marker.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = GetEntityIcon(entity.Stage);

                _entityMarkers.Add(marker);
            }
        }

        // ─── Tile Click ──────────────────────────────────────────

        private void OnTileClicked(WorldTileDto tile)
        {
            _selectedTile = tile;
            tileInfoPopup?.SetActive(true);

            var gm = GameManager.Instance;
            string civName      = "—";
            string religionName = "—";
            int population      = tile.Population;

            if (tile.CivilizationId != null && gm?.Civilizations.TryGetValue(tile.CivilizationId, out var civ) == true)
                civName = $"{civ.Name} ({civ.State})";

            if (tile.ReligionId != null && gm?.Religions.TryGetValue(tile.ReligionId, out var rel) == true)
                religionName = rel.Name;

            if (tileTypeText)   tileTypeText.text   = $"Địa hình: {tile.Type}";
            if (tileCivText)    tileCivText.text     = $"Civ: {civName}";
            if (tileReligionText) tileReligionText.text = $"Tôn giáo: {religionName}";
            if (tilePopText)    tilePopText.text     = $"Dân số: {population:N0}";

            if (miracleHereBtn)
            {
                miracleHereBtn.onClick.RemoveAllListeners();
                miracleHereBtn.onClick.AddListener(() => OpenMiracleForTile(tile.X, tile.Y, tile.CivilizationId));
            }
        }

        private void OpenMiracleForTile(int x, int y, string? civId)
        {
            // Thông báo cho MiraclePanel target tile
            var miraclePanel = FindObjectOfType<UI.Lobby.MiraclePanel>();
            miraclePanel?.SetTarget(x, y, civId);
            tileInfoPopup?.SetActive(false);
        }

        // ─── Map Toggle ──────────────────────────────────────────

        private void ToggleMap()
        {
            bool active = !(mapPanel?.activeSelf ?? false);
            mapPanel?.SetActive(active);
            if (active && _tileImages.Count == 0)
            {
                var state = GameManager.Instance?.CurrentWorldState;
                if (state != null) BuildMap(state);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static Color GetGodColor(string godId, GameManager gm)
        {
            if (!gm.Gods.TryGetValue(godId, out var god))
                return new Color(1f, 1f, 1f, 0.3f);

            return god.Archetype switch
            {
                GodArchetype.Order     => new Color(0.8f, 0.9f, 1.0f, 0.4f),
                GodArchetype.Chaos     => new Color(1.0f, 0.3f, 0.8f, 0.4f),
                GodArchetype.Light     => new Color(1.0f, 1.0f, 0.5f, 0.4f),
                GodArchetype.Darkness  => new Color(0.4f, 0.1f, 0.6f, 0.4f),
                GodArchetype.Nature    => new Color(0.3f, 0.8f, 0.3f, 0.4f),
                GodArchetype.Death     => new Color(0.5f, 0.5f, 0.5f, 0.4f),
                GodArchetype.Knowledge => new Color(0.3f, 0.7f, 1.0f, 0.4f),
                GodArchetype.War       => new Color(1.0f, 0.2f, 0.2f, 0.4f),
                _ => new Color(1f, 1f, 1f, 0.3f)
            };
        }

        private static string GetEntityIcon(string stage) => stage switch
        {
            "WildAnimal"         => "🐾",
            "DivineBeast"        => "🐉",
            "CelestialGuardian"  => "⭐",
            "HumanHero"          => "⚔️",
            "Saint"              => "✨",
            "FallenDemonLord"    => "💀",
            "Monster"            => "👾",
            "Titan"              => "🗿",
            "ApocalypticEntity"  => "☄️",
            _ => "?"
        };
    }
}
