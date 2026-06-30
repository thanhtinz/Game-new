using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client
{
    /// <summary>
    /// Renders the world map using Unity 2D sprites on the XY plane.
    /// Camera is orthographic looking down the -Z axis.
    /// Tiles placed at Vector3(x * tileSize, y * tileSize, 0).
    /// </summary>
    public class WorldRenderer : MonoBehaviour
    {
        [Header("Tile Sprites")]
        [SerializeField] private Sprite grasslandSprite;
        [SerializeField] private Sprite forestSprite;
        [SerializeField] private Sprite mountainSprite;
        [SerializeField] private Sprite desertSprite;
        [SerializeField] private Sprite tundraSprite;
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite volcanoSprite;
        [SerializeField] private Sprite sacredSprite;
        [SerializeField] private Sprite beachSprite;
        [SerializeField] private Sprite riverSprite;

        [Header("Marker Sprites")]
        [SerializeField] private Sprite templeSprite;
        [SerializeField] private Sprite cityMarkerSprite;

        [Header("Render Settings")]
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private int maxTilesPerFrame = 100;
        [SerializeField] private int tilesPerUnit = 1;           // Pixels Per Unit of your sprites

        // Optional: sort-order layers
        private const int LayerTile   = 0;
        private const int LayerMarker = 1;
        private const int LayerTemple = 2;

        private readonly Dictionary<string, SpriteRenderer> _tileRenderers = new();
        private readonly Dictionary<string, SpriteRenderer> _markers       = new();
        private Transform _worldRoot;
        private int _worldWidth;
        private int _worldHeight;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Start()
        {
            _worldRoot = new GameObject("WorldRoot").transform;
            _worldRoot.SetParent(transform);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWorldLoaded += HandleWorldLoaded;
                GameManager.Instance.OnTick        += HandleTick;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWorldLoaded -= HandleWorldLoaded;
                GameManager.Instance.OnTick        -= HandleTick;
            }
        }

        // ─── World Load ───────────────────────────────────────

        private void HandleWorldLoaded(WorldStateDto state)
        {
            _worldWidth  = state.Width;
            _worldHeight = state.Height;

            ClearWorld();
            StartCoroutine(RenderTilesCoroutine(state.ChangedTiles));
            RenderCivilizations(state.Civilizations);
        }

        private IEnumerator RenderTilesCoroutine(List<WorldTileDto> tiles)
        {
            int count = 0;
            foreach (var tile in tiles)
            {
                RenderTile(tile);
                count++;
                if (count >= maxTilesPerFrame)
                {
                    count = 0;
                    yield return null;  // yield one frame to prevent lag spike
                }
            }
        }

        private void RenderTile(WorldTileDto tile)
        {
            string key = $"{tile.X}_{tile.Y}";

            // Reuse existing SpriteRenderer if tile already exists
            if (!_tileRenderers.TryGetValue(key, out var sr))
            {
                var go = new GameObject(key);
                go.transform.SetParent(_worldRoot);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = LayerTile;
                _tileRenderers[key] = sr;
            }

            // Position: XY plane, Z=0
            sr.transform.position = TileToWorld(tile.X, tile.Y);
            sr.sprite = GetTileSprite(tile.Type);

            // Temple marker — placed at Z = -0.1 to render on top
            string templeKey = $"temple_{key}";
            if (tile.HasTemple)
            {
                if (!_markers.TryGetValue(templeKey, out var tsr))
                {
                    var tgo = new GameObject(templeKey);
                    tgo.transform.SetParent(_worldRoot);
                    tsr = tgo.AddComponent<SpriteRenderer>();
                    tsr.sortingOrder = LayerTemple;
                    _markers[templeKey] = tsr;
                }
                tsr.transform.position = TileToWorld(tile.X, tile.Y, zOffset: -0.1f);
                tsr.sprite = templeSprite;
            }
            else if (_markers.TryGetValue(templeKey, out var oldTemple))
            {
                Destroy(oldTemple.gameObject);
                _markers.Remove(templeKey);
            }
        }

        private void RenderCivilizations(List<CivilizationDto> civs)
        {
            // Clear old city markers
            var toRemove = new List<string>();
            foreach (var kv in _markers)
                if (kv.Key.StartsWith("civ_")) toRemove.Add(kv.Key);
            foreach (var k in toRemove)
            {
                Destroy(_markers[k].gameObject);
                _markers.Remove(k);
            }

            if (cityMarkerSprite == null) return;

            foreach (var civ in civs)
            {
                foreach (var coords in civ.ControlledTiles)
                {
                    if (coords.Length < 2) continue;
                    string key = $"civ_{coords[0]}_{coords[1]}";

                    var go  = new GameObject(key);
                    go.transform.SetParent(_worldRoot);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite       = cityMarkerSprite;
                    sr.sortingOrder = LayerMarker;
                    sr.color        = GetCivColor(civ.State);
                    sr.transform.position = TileToWorld(coords[0], coords[1], zOffset: -0.05f);
                    _markers[key] = sr;
                }
            }
        }

        // ─── Tick ─────────────────────────────────────────────

        private void HandleTick(long tick, int cycle)
        {
            // Lightweight per-tick visual update (colour pulses, etc.)
            // Heavy updates are handled via specific events in GameManager
        }

        // ─── Public API ───────────────────────────────────────

        /// <summary>
        /// Converts tile grid coordinates to Unity world position (XY plane).
        /// </summary>
        public Vector3 TileToWorld(int tileX, int tileY, float zOffset = 0f)
            => new(tileX * tileSize, tileY * tileSize, zOffset);

        /// <summary>
        /// Converts a Unity world position back to the nearest tile coordinate.
        /// </summary>
        public Vector2Int WorldToTile(Vector2 worldPos)
            => new(Mathf.RoundToInt(worldPos.x / tileSize),
                   Mathf.RoundToInt(worldPos.y / tileSize));

        /// <summary>
        /// Centers the main camera on the middle of the loaded world.
        /// Call after world load or on first play.
        /// </summary>
        public void CenterCamera()
        {
            if (Camera.main == null) return;
            Camera.main.transform.position = new Vector3(
                _worldWidth  * tileSize * 0.5f,
                _worldHeight * tileSize * 0.5f,
                -10f                            // Z must be negative so camera looks at Z=0 tiles
            );
        }

        // ─── Helpers ──────────────────────────────────────────

        private void ClearWorld()
        {
            foreach (var sr in _tileRenderers.Values)
                if (sr) Destroy(sr.gameObject);
            _tileRenderers.Clear();

            foreach (var sr in _markers.Values)
                if (sr) Destroy(sr.gameObject);
            _markers.Clear();
        }

        private Sprite GetTileSprite(TileType type) => type switch
        {
            TileType.Grassland => grasslandSprite,
            TileType.Forest    => forestSprite,
            TileType.Mountain  => mountainSprite,
            TileType.Desert    => desertSprite,
            TileType.Tundra    => tundraSprite,
            TileType.Water     => waterSprite,
            TileType.Volcano   => volcanoSprite,
            TileType.Sacred    => sacredSprite,
            TileType.Beach     => beachSprite,
            TileType.River     => riverSprite,
            _                  => grasslandSprite,
        };

        private static Color GetCivColor(CivilizationState state) => state switch
        {
            CivilizationState.Tribal     => new Color(0.6f, 0.4f, 0.2f),
            CivilizationState.Kingdom    => new Color(0.2f, 0.5f, 0.8f),
            CivilizationState.Empire     => new Color(0.8f, 0.7f, 0.1f),
            CivilizationState.Collapsing => new Color(0.8f, 0.2f, 0.2f),
            CivilizationState.Fallen     => new Color(0.3f, 0.3f, 0.3f),
            _                            => Color.white,
        };
    }
}
