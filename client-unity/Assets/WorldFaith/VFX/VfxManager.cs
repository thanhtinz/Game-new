using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Client.VFX
{
    // ─── VFX Catalog ─────────────────────────────────────────
    public enum VfxId
    {
        // Miracles
        Rain,
        Storm,
        Earthquake,
        Volcano,
        BlessHarvest,
        Curse,
        HolyLight,
        DarkExplosion,
        DivineBeam,
        DemonPortal,

        // Evolution
        EvolveBasic,
        EvolveApex,
        DivineBeastSpawn,
        ApocalypticSpawn,

        // Religion
        TempleBuilt,
        ReligionSpread,
        Crusade,
        Schism,

        // God
        FaithGain,
        FaithLost,
        GodFaded,

        // World
        WorldRebirth,
        CivCollapse,
        CivFounded,

        // UI feedback
        MiracleCountered,
        SacredGlow,
    }

    /// <summary>
    /// Quản lý VFX với object pooling.
    /// Mỗi VfxId có một prefab particle system.
    /// Pool size tự động mở rộng khi cần.
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        public static VfxManager Instance { get; private set; }

        [System.Serializable]
        public class VfxEntry
        {
            public VfxId id;
            public GameObject prefab;
            public int poolSize = 3;
            public float lifetime = 3f;
        }

        [Header("VFX Catalog")]
        [SerializeField] private VfxEntry[] catalog;

        [Header("Quality")]
        [SerializeField] private bool reduceVfxOnMobile = true;
        [SerializeField] private int mobileMaxActiveVfx = 8;

        private readonly Dictionary<VfxId, Queue<GameObject>> _pool = new();
        private readonly Dictionary<VfxId, VfxEntry>         _catalog = new();
        private readonly List<GameObject>                     _active = new();

        private bool _isMobile;
        private Transform _poolRoot;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _isMobile = Application.isMobilePlatform;
            _poolRoot = new GameObject("VFX_Pool").transform;
            _poolRoot.SetParent(transform);

            InitPools();
        }

        // ─── Play ─────────────────────────────────────────────

        public void Play(VfxId id, Vector3 position, float scale = 1f, Transform parent = null)
        {
            // Mobile: giới hạn số lượng VFX đồng thời
            if (_isMobile && reduceVfxOnMobile && _active.Count >= mobileMaxActiveVfx)
                return;

            if (!_catalog.TryGetValue(id, out var entry)) return;
            if (entry.prefab == null) return;

            var go = GetFromPool(id);
            if (go == null) return;

            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            if (parent != null) go.transform.SetParent(parent);

            go.SetActive(true);
            _active.Add(go);

            // Kích hoạt tất cả particle systems
            var systems = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems) ps.Play();

            StartCoroutine(ReturnToPool(go, id, entry.lifetime));
        }

        public void PlayAtTile(VfxId id, int tileX, int tileY, float tileSize = 1f, float scale = 1f)
            => Play(id, new Vector3(tileX * tileSize, 0f, tileY * tileSize), scale);

        // ─── Miracle VFX Mapping ──────────────────────────────

        public void PlayMiracleVfx(MiracleType miracle, int x, int y)
        {
            var vfxId = miracle switch
            {
                MiracleType.Rain              => VfxId.Rain,
                MiracleType.Storm             => VfxId.Storm,
                MiracleType.Earthquake        => VfxId.Earthquake,
                MiracleType.Volcano           => VfxId.Volcano,
                MiracleType.BlessHarvest      => VfxId.BlessHarvest,
                MiracleType.Curse             => VfxId.Curse,
                MiracleType.Portal            => VfxId.DemonPortal,
                MiracleType.DivineVoice       => VfxId.HolyLight,
                MiracleType.DemonInvasion     => VfxId.DemonPortal,
                MiracleType.DivineBeastCreation => VfxId.DivineBeastSpawn,
                MiracleType.Revelation        => VfxId.DivineBeam,
                MiracleType.HolyWar           => VfxId.Crusade,
                MiracleType.HealFollower      => VfxId.HolyLight,
                MiracleType.Dream             => VfxId.FaithGain,
                MiracleType.Omen              => VfxId.SacredGlow,
                _ => VfxId.HolyLight
            };
            PlayAtTile(vfxId, x, y);
        }

        public void PlayCounterVfx(int x, int y)
            => PlayAtTile(VfxId.MiracleCountered, x, y, scale: 1.5f);

        // ─── Persistent VFX ──────────────────────────────────

        /// <summary>
        /// VFX liên tục (Sacred glow, civ aura...) - không auto-return về pool.
        /// Caller phải gọi StopPersistent() để tắt.
        /// </summary>
        public GameObject PlayPersistent(VfxId id, Vector3 position, Transform parent = null)
        {
            if (!_catalog.TryGetValue(id, out var entry)) return null;
            if (entry.prefab == null) return null;

            var go = Instantiate(entry.prefab, position, Quaternion.identity);
            if (parent != null) go.transform.SetParent(parent);
            go.SetActive(true);

            var systems = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems) ps.Play();

            return go;
        }

        public void StopPersistent(GameObject go)
        {
            if (go == null) return;
            var systems = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems) ps.Stop();
            StartCoroutine(DelayedDestroy(go, 2f));
        }

        // ─── World Rebirth VFX ────────────────────────────────

        public IEnumerator PlayWorldRebirthSequence(Vector3 center)
        {
            // Wave 1: flash
            Play(VfxId.WorldRebirth, center, scale: 5f);
            yield return new WaitForSeconds(0.5f);

            // Wave 2: beams
            for (int i = 0; i < 4; i++)
            {
                var offset = Quaternion.Euler(0, i * 90f, 0) * Vector3.forward * 10f;
                Play(VfxId.DivineBeam, center + offset);
            }
            yield return new WaitForSeconds(0.8f);

            // Wave 3: sacred glow trên toàn map
            Play(VfxId.SacredGlow, center, scale: 20f);
        }

        // ─── Pool Management ──────────────────────────────────

        private void InitPools()
        {
            if (catalog == null) return;

            foreach (var entry in catalog)
            {
                _catalog[entry.id] = entry;
                _pool[entry.id] = new Queue<GameObject>();

                if (entry.prefab == null) continue;

                int size = _isMobile ? Mathf.Min(entry.poolSize, 2) : entry.poolSize;
                for (int i = 0; i < size; i++)
                {
                    var go = Instantiate(entry.prefab, _poolRoot);
                    go.SetActive(false);
                    _pool[entry.id].Enqueue(go);
                }
            }
        }

        private GameObject GetFromPool(VfxId id)
        {
            if (!_pool.TryGetValue(id, out var queue)) return null;

            if (queue.Count > 0)
                return queue.Dequeue();

            // Pool cạn: instantiate thêm
            if (!_catalog.TryGetValue(id, out var entry) || entry.prefab == null) return null;
            return Instantiate(entry.prefab, _poolRoot);
        }

        private IEnumerator ReturnToPool(GameObject go, VfxId id, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go == null) yield break;

            var systems = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems) ps.Stop();

            go.SetActive(false);
            go.transform.SetParent(_poolRoot);
            _active.Remove(go);
            _pool[id].Enqueue(go);
        }

        private IEnumerator DelayedDestroy(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(go);
        }
    }
}
