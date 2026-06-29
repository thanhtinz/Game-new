using System.Collections.Generic;
using UnityEngine;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client
{
    /// <summary>
    /// Render world tiles, civilizations, temples lên scene.
    /// Dùng tile-based rendering với tilemaps hoặc simple plane meshes.
    /// </summary>
    public class WorldRenderer : MonoBehaviour
    {
        [Header("Tile Prefabs")]
        [SerializeField] private GameObject grasslandPrefab;
        [SerializeField] private GameObject forestPrefab;
        [SerializeField] private GameObject mountainPrefab;
        [SerializeField] private GameObject desertPrefab;
        [SerializeField] private GameObject waterPrefab;
        [SerializeField] private GameObject volcanoPrefab;
        [SerializeField] private GameObject sacredPrefab;

        [Header("Marker Prefabs")]
        [SerializeField] private GameObject templePrefab;
        [SerializeField] private GameObject cityMarkerPrefab;

        [Header("Render Settings")]
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private int maxTilesPerFrame = 50;

        private readonly Dictionary<string, GameObject> _tileObjects = new();
        private readonly Dictionary<string, GameObject> _markers = new();
        private Transform _worldRoot;
        private int _worldWidth;
        private int _worldHeight;

        private void Start()
        {
            _worldRoot = new GameObject("WorldRoot").transform;
            _worldRoot.SetParent(transform);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWorldLoaded += HandleWorldLoaded;
                GameManager.Instance.OnTick += HandleTick;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWorldLoaded -= HandleWorldLoaded;
                GameManager.Instance.OnTick -= HandleTick;
            }
        }

        // ─── World Load ─────────────────────────────────────────

        private void HandleWorldLoaded(WorldStateDto state)
        {
            _worldWidth = state.Width;
            _worldHeight = state.Height;

            ClearWorld();
            StartCoroutine(RenderTilesCoroutine(state.ChangedTiles));
            RenderCivilizations(state.Civilizations);
        }

        private System.Collections.IEnumerator RenderTilesCoroutine(List<WorldTileDto> tiles)
        {
            int count = 0;
            foreach (var tile in tiles)
            {
                RenderTile(tile);
                count++;
                if (count >= maxTilesPerFrame)
                {
                    count = 0;
                    yield return null; // Yield một frame để tránh lag
                }
            }
        }

        private void RenderTile(WorldTileDto tile)
        {
            string key = $"{tile.X}_{tile.Y}";

            if (_tileObjects.TryGetValue(key, out var existing))
                Destroy(existing);

            var prefab = GetTilePrefab(tile.Type);
            if (prefab == null) return;

            var position = new Vector3(tile.X * tileSize, 0, tile.Y * tileSize);
            var obj = Instantiate(prefab, position, Quaternion.identity, _worldRoot);
            obj.name = key;
            _tileObjects[key] = obj;

            // Render temple nếu có
            if (tile.HasTemple && templePrefab != null)
            {
                var temple = Instantiate(templePrefab, position + Vector3.up * 0.5f, Quaternion.identity, obj.transform);
                _markers[$"temple_{key}"] = temple;
            }
        }

        private void RenderCivilizations(List<CivilizationDto> civs)
        {
            foreach (var civ in civs)
            {
                foreach (var tileCoords in civ.ControlledTiles)
                {
                    if (tileCoords.Length < 2) continue;
                    string key = $"civ_{tileCoords[0]}_{tileCoords[1]}";

                    if (cityMarkerPrefab == null) continue;

                    var position = new Vector3(tileCoords[0] * tileSize, 0.5f, tileCoords[1] * tileSize);
                    var marker = Instantiate(cityMarkerPrefab, position, Quaternion.identity, _worldRoot);
                    marker.name = key;

                    // Màu theo civ state
                    var renderer = marker.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material.color = GetCivColor(civ.State);

                    _markers[key] = marker;
                }
            }
        }

        // ─── Tick Updates ───────────────────────────────────────

        private void HandleTick(long tick, int cycle)
        {
            // Cập nhật visual nhẹ mỗi tick (thay màu, animation...)
            // Các cập nhật lớn hơn xử lý qua events riêng biệt
        }

        // ─── Camera Control ─────────────────────────────────────

        public void CenterCamera()
        {
            if (Camera.main == null) return;
            Camera.main.transform.position = new Vector3(
                _worldWidth * tileSize * 0.5f,
                Mathf.Max(_worldWidth, _worldHeight) * 0.8f,
                _worldHeight * tileSize * 0.5f
            );
            Camera.main.transform.LookAt(new Vector3(
                _worldWidth * tileSize * 0.5f, 0,
                _worldHeight * tileSize * 0.5f
            ));
        }

        // ─── Helpers ────────────────────────────────────────────

        private void ClearWorld()
        {
            foreach (var obj in _tileObjects.Values)
                if (obj != null) Destroy(obj);
            _tileObjects.Clear();

            foreach (var m in _markers.Values)
                if (m != null) Destroy(m);
            _markers.Clear();
        }

        private GameObject GetTilePrefab(TileType type) => type switch
        {
            TileType.Grassland => grasslandPrefab,
            TileType.Forest => forestPrefab,
            TileType.Mountain => mountainPrefab,
            TileType.Desert => desertPrefab,
            TileType.Water => waterPrefab,
            TileType.Volcano => volcanoPrefab,
            TileType.Sacred => sacredPrefab,
            _ => grasslandPrefab
        };

        private static Color GetCivColor(CivilizationState state) => state switch
        {
            CivilizationState.Tribal => new Color(0.6f, 0.4f, 0.2f),
            CivilizationState.Kingdom => new Color(0.2f, 0.5f, 0.8f),
            CivilizationState.Empire => new Color(0.8f, 0.7f, 0.1f),
            CivilizationState.Collapsing => new Color(0.8f, 0.2f, 0.2f),
            CivilizationState.Fallen => new Color(0.3f, 0.3f, 0.3f),
            _ => Color.white
        };
    }
}
