using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Minimap nhỏ ở góc màn hình — luôn hiển thị, không che gameplay.
    /// Click minimap → camera nhảy đến vị trí đó.
    /// Hiển thị: terrain màu biome, civ markers, entity dots.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        [Header("Minimap RenderTexture")]
        [SerializeField] private RawImage minimapImage;     // Hiển thị RenderTexture
        [SerializeField] private RectTransform minimapRect;
        [SerializeField] private int textureSize = 128;

        [Header("Camera Indicator")]
        [SerializeField] private RectTransform cameraIndicator; // Hình chữ nhật trắng trên minimap

        [Header("Markers")]
        [SerializeField] private Transform markerContainer;
        [SerializeField] private GameObject godMarkerPrefab;    // Dot cho god position
        [SerializeField] private GameObject entityMarkerPrefab;

        [Header("Controls")]
        [SerializeField] private Button minimapBtn;  // Click để toggle size
        [SerializeField] private bool allowClick = true;

        private Texture2D _minimapTex;
        private int _worldW, _worldH;
        private Camera _mainCam;
        private readonly List<GameObject> _markers = new();
        private bool _isExpanded = false;
        private Vector2 _normalSize = new(128, 128);
        private Vector2 _expandedSize = new(256, 256);

        // Biome colors (simplified, low resolution)
        private static readonly Dictionary<TileType, Color32> BiomePixels = new()
        {
            { TileType.Grassland, new Color32(80,  160, 60,  255) },
            { TileType.Forest,    new Color32(30,  100, 30,  255) },
            { TileType.Mountain,  new Color32(140, 140, 140, 255) },
            { TileType.Desert,    new Color32(220, 200, 100, 255) },
            { TileType.Tundra,    new Color32(200, 220, 240, 255) },
            { TileType.Water,     new Color32(40,  100, 200, 255) },
            { TileType.Volcano,   new Color32(200, 50,  30,  255) },
            { TileType.Sacred,    new Color32(230, 200, 50,  255) },
            { TileType.Beach,     new Color32(230, 215, 160, 255) },
            { TileType.River,     new Color32(70,  140, 220, 255) },
        };

        private void Start()
        {
            _mainCam = Camera.main;
            minimapBtn?.onClick.AddListener(ToggleSize);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded += BuildMinimap;
                gm.OnTick        += (_, _) => UpdateMarkers();
                gm.OnCivilizationUpdate += _ => RepaintCivLayer();
            }
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded        -= BuildMinimap;
                gm.OnCivilizationUpdate -= _ => RepaintCivLayer();
            }
            if (_minimapTex != null) Destroy(_minimapTex);
        }

        private void LateUpdate()
        {
            UpdateCameraIndicator();
        }

        // ─── Build Minimap Texture ───────────────────────────────

        private void BuildMinimap(WorldStateDto state)
        {
            _worldW = state.Width;
            _worldH = state.Height;

            if (_minimapTex != null) Destroy(_minimapTex);
            _minimapTex = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point
            };

            var pixels = new Color32[textureSize * textureSize];
            float scaleX = (float)textureSize / _worldW;
            float scaleY = (float)textureSize / _worldH;

            foreach (var tile in state.ChangedTiles)
            {
                int px = Mathf.FloorToInt(tile.X * scaleX);
                int py = Mathf.FloorToInt(tile.Y * scaleY);
                if (px < 0 || px >= textureSize || py < 0 || py >= textureSize) continue;

                var color = BiomePixels.TryGetValue(tile.Type, out var c)
                    ? c : new Color32(100, 100, 100, 255);
                pixels[py * textureSize + px] = color;
            }

            _minimapTex.SetPixels32(pixels);
            _minimapTex.Apply();

            if (minimapImage != null) minimapImage.texture = _minimapTex;
            UpdateMarkers();
        }

        // ─── Civ Layer ───────────────────────────────────────────

        private void RepaintCivLayer()
        {
            if (_minimapTex == null || _worldW == 0) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            float scaleX = (float)textureSize / _worldW;
            float scaleY = (float)textureSize / _worldH;

            foreach (var civ in gm.Civilizations.Values)
            {
                Color32 civColor = civ.State switch
                {
                    CivilizationState.Empire    => new Color32(200, 170, 30, 180),
                    CivilizationState.Kingdom   => new Color32(50,  120, 200, 180),
                    CivilizationState.Tribal    => new Color32(150, 100, 50,  180),
                    CivilizationState.Collapsing=> new Color32(200, 50,  50,  180),
                    _ => new Color32(80, 80, 80, 150)
                };

                foreach (var coords in civ.ControlledTiles)
                {
                    if (coords.Length < 2) continue;
                    int px = Mathf.FloorToInt(coords[0] * scaleX);
                    int py = Mathf.FloorToInt(coords[1] * scaleY);
                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        _minimapTex.SetPixel(px, py, civColor);
                }
            }

            _minimapTex.Apply();
        }

        // ─── Markers ─────────────────────────────────────────────

        private void UpdateMarkers()
        {
            foreach (var m in _markers) Destroy(m);
            _markers.Clear();

            if (markerContainer == null || _worldW == 0) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            var minimapSize = minimapRect?.sizeDelta ?? _normalSize;
            float scaleX = minimapSize.x / _worldW;
            float scaleY = minimapSize.y / _worldH;

            // God markers
            if (godMarkerPrefab != null)
            {
                foreach (var god in gm.Gods.Values)
                {
                    if (!god.IsAlive) continue;
                    // Gods không có position, dùng center placeholder
                    var marker = Instantiate(godMarkerPrefab, markerContainer);
                    var rt     = marker.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(minimapSize.x / 2f, minimapSize.y / 2f);

                    var img = marker.GetComponent<Image>();
                    if (img != null && god.Id == gm.MyGod?.Id)
                        img.color = Color.yellow;

                    _markers.Add(marker);
                }
            }

            // Entity markers
            if (entityMarkerPrefab != null && gm.CurrentWorldState != null)
            {
                foreach (var entity in gm.CurrentWorldState.Entities)
                {
                    // Chỉ hiển thị apex entities
                    bool isApex = entity.Stage is "CelestialGuardian" or "ApocalypticEntity" or "FallenDemonLord";
                    if (!isApex) continue;

                    var marker = Instantiate(entityMarkerPrefab, markerContainer);
                    var rt     = marker.GetComponent<RectTransform>();
                    float px   = entity.X * scaleX;
                    float py   = entity.Y * scaleY;
                    rt.anchoredPosition = new Vector2(px, py);

                    _markers.Add(marker);
                }
            }
        }

        // ─── Camera Indicator ────────────────────────────────────

        private void UpdateCameraIndicator()
        {
            if (cameraIndicator == null || _mainCam == null || _worldW == 0) return;

            var minimapSize = minimapRect?.sizeDelta ?? _normalSize;
            float scaleX = minimapSize.x / _worldW;
            float scaleY = minimapSize.y / _worldH;

            // 2D: camera is on XY plane, use X and Y position
            float camX = _mainCam.transform.position.x;
            float camY = _mainCam.transform.position.y;

            float mmX = camX * scaleX;
            float mmY = camY * scaleY;

            cameraIndicator.anchoredPosition = new Vector2(
                Mathf.Clamp(mmX, 0, minimapSize.x),
                Mathf.Clamp(mmY, 0, minimapSize.y));
        }

        // ─── Click to move camera ────────────────────────────────

        public void OnMinimapClick(Vector2 localPoint)
        {
            if (!allowClick || _worldW == 0) return;

            var minimapSize = minimapRect?.sizeDelta ?? _normalSize;

            // 2D: map localPoint to world XY
            float worldX = (localPoint.x / minimapSize.x) * _worldW;
            float worldY = (localPoint.y / minimapSize.y) * _worldH;

            var camCtrl = _mainCam?.GetComponent<CameraController>();
            camCtrl?.CenterOn(new Vector2(worldX, worldY));
        }

        // ─── Toggle Size ─────────────────────────────────────────

        private void ToggleSize()
        {
            _isExpanded = !_isExpanded;
            if (minimapRect != null)
                minimapRect.sizeDelta = _isExpanded ? _expandedSize : _normalSize;
        }
    }
}
