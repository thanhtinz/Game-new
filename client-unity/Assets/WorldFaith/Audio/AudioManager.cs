using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldFaith.Client.Audio
{
    // ─── SFX Catalog ─────────────────────────────────────────
    public enum SfxId
    {
        // UI
        ButtonClick,
        ButtonHover,
        PanelOpen,
        PanelClose,
        TabSwitch,
        Notification,
        Error,
        Success,

        // Miracles - Tier 1
        MiracleRain,
        MiracleDream,
        MiracleBlessHarvest,
        MiracleHeal,
        MiracleOmen,

        // Miracles - Tier 2
        MiracleStorm,
        MiracleEarthquake,
        MiracleCurse,
        MiraclePortal,
        MiracleDivineVoice,

        // Miracles - Tier 3
        MiracleVolcano,
        MiracleDemonInvasion,
        MiracleDivineBeast,
        MiracleRevelation,
        MiracleHolyWar,

        // Miracle Counter
        MiracleCountered,

        // Religion
        ReligionFounded,
        TempleBuilt,
        Conversion,
        Schism,
        Heresy,
        Crusade,

        // Evolution
        EntityEvolve,
        EntityApex,
        EntityAttack,

        // Civilization
        CivFounded,
        CivCollapsed,
        CivAtWar,

        // Game state
        WorldRebirth,
        GodFaded,
        Victory,
        Defeat,

        // Ambient
        FollowerPrayer,
        TempleChant,
        WorldHeartbeat,
    }

    // ─── Music Layers ─────────────────────────────────────────
    public enum MusicLayer
    {
        Base,       // Luôn phát, nhạc nền chính
        Religion,   // Fade in khi có nhiều temple
        War,        // Fade in khi có crusade/holy war
        Apocalypse, // Fade in khi có Apex entity
        Victory,    // One-shot khi thắng
    }

    // ─── Audio Manager ────────────────────────────────────────
    /// <summary>
    /// Quản lý tất cả âm thanh game:
    /// - SFX pool (tránh allocate AudioSource liên tục)
    /// - Dynamic layered music (nhiều layer fade in/out độc lập)
    /// - Adaptive audio dựa trên game state
    /// - Mobile: auto lower quality khi battery low
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX")]
        [SerializeField] private int sfxPoolSize = 12;
        [SerializeField] private AudioClip[] sfxClips; // Gán theo thứ tự SfxId enum trong Inspector
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;

        [Header("Music Layers")]
        [SerializeField] private AudioClip musicBase;
        [SerializeField] private AudioClip musicReligion;
        [SerializeField] private AudioClip musicWar;
        [SerializeField] private AudioClip musicApocalypse;
        [SerializeField] private AudioClip musicVictory;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private float musicFadeDuration = 2f;

        [Header("Adaptive")]
        [SerializeField] private bool adaptiveAudio = true;

        // SFX pool
        private readonly Queue<AudioSource> _sfxPool = new();
        private readonly List<AudioSource> _activeSfx = new();

        // Music layers
        private readonly Dictionary<MusicLayer, AudioSource> _musicSources = new();
        private readonly Dictionary<MusicLayer, Coroutine> _fadeCoroutines = new();

        // Settings
        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;

        // Adaptive state
        private int _activeReligions;
        private bool _hasWar;
        private bool _hasApexEntity;

        // ─── Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSfxPool();
            InitMusicLayers();
            LoadSettings();
        }

        private void Start()
        {
            // Subscribe game events
            var gm = Managers.GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded      += _ => OnWorldLoaded();
                gm.OnMiracleResult    += OnMiracleResult;
                gm.OnRebirth          += _ => PlaySfx(SfxId.WorldRebirth);
                gm.OnGameOver         += evt => PlaySfx(
                    evt.WinnerGodId == gm.MyGod?.Id ? SfxId.Victory : SfxId.Defeat);
            }

            // Start base music
            PlayMusicLayer(MusicLayer.Base, 1f);
        }

        private void OnDestroy()
        {
            SaveSettings();
        }

        // ─── SFX ──────────────────────────────────────────────

        public void PlaySfx(SfxId id, float pitchVariance = 0.05f, float volumeScale = 1f)
        {
            if (!_sfxEnabled) return;

            int index = (int)id;
            if (sfxClips == null || index >= sfxClips.Length || sfxClips[index] == null) return;

            var source = GetPooledSource();
            source.clip = sfxClips[index];
            source.volume = sfxVolume * volumeScale;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
            source.Play();

            StartCoroutine(ReturnToPool(source, source.clip.length + 0.1f));
        }

        public void PlaySfxAt(SfxId id, Vector3 position, float spatialBlend = 0.8f)
        {
            if (!_sfxEnabled) return;
            int index = (int)id;
            if (sfxClips == null || index >= sfxClips.Length || sfxClips[index] == null) return;

            var source = GetPooledSource();
            source.clip = sfxClips[index];
            source.volume = sfxVolume;
            source.spatialBlend = spatialBlend;
            source.transform.position = position;
            source.Play();

            StartCoroutine(ReturnToPool(source, source.clip.length + 0.1f));
        }

        // ─── Music ────────────────────────────────────────────

        public void PlayMusicLayer(MusicLayer layer, float targetVolume = 1f)
        {
            if (!_musicSources.TryGetValue(layer, out var source)) return;

            var clip = layer switch
            {
                MusicLayer.Base       => musicBase,
                MusicLayer.Religion   => musicReligion,
                MusicLayer.War        => musicWar,
                MusicLayer.Apocalypse => musicApocalypse,
                MusicLayer.Victory    => musicVictory,
                _ => null
            };

            if (clip == null) return;

            if (!source.isPlaying)
            {
                source.clip = clip;
                source.loop = layer != MusicLayer.Victory;
                source.volume = 0f;
                source.Play();
            }

            FadeMusicLayer(layer, targetVolume * musicVolume);
        }

        public void StopMusicLayer(MusicLayer layer)
            => FadeMusicLayer(layer, 0f, stopOnFade: true);

        private void FadeMusicLayer(MusicLayer layer, float target, bool stopOnFade = false)
        {
            if (_fadeCoroutines.TryGetValue(layer, out var existing) && existing != null)
                StopCoroutine(existing);

            if (!_musicSources.TryGetValue(layer, out var source)) return;
            _fadeCoroutines[layer] = StartCoroutine(FadeCoroutine(source, target, stopOnFade));
        }

        private IEnumerator FadeCoroutine(AudioSource source, float target, bool stopOnFade)
        {
            float start = source.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, elapsed / musicFadeDuration);
                yield return null;
            }

            source.volume = target;
            if (stopOnFade && target <= 0.001f) source.Stop();
        }

        // ─── Adaptive Audio ───────────────────────────────────

        private void OnWorldLoaded()
        {
            PlayMusicLayer(MusicLayer.Base, 1f);
            StopMusicLayer(MusicLayer.War);
            StopMusicLayer(MusicLayer.Apocalypse);
            _hasWar = false;
            _hasApexEntity = false;
        }

        private void OnMiracleResult(Shared.Contracts.MiracleResultEvent evt)
        {
            if (!evt.Success) { PlaySfx(SfxId.Error); return; }

            if (evt.WasCountered)
            {
                PlaySfx(SfxId.MiracleCountered);
                return;
            }

            // Map miracle type → SFX
            SfxId sfx = evt.Miracle switch
            {
                Shared.Enums.MiracleType.Rain              => SfxId.MiracleRain,
                Shared.Enums.MiracleType.Dream             => SfxId.MiracleDream,
                Shared.Enums.MiracleType.BlessHarvest      => SfxId.MiracleBlessHarvest,
                Shared.Enums.MiracleType.HealFollower      => SfxId.MiracleHeal,
                Shared.Enums.MiracleType.Omen              => SfxId.MiracleOmen,
                Shared.Enums.MiracleType.Storm             => SfxId.MiracleStorm,
                Shared.Enums.MiracleType.Earthquake        => SfxId.MiracleEarthquake,
                Shared.Enums.MiracleType.Curse             => SfxId.MiracleCurse,
                Shared.Enums.MiracleType.Portal            => SfxId.MiraclePortal,
                Shared.Enums.MiracleType.DivineVoice       => SfxId.MiracleDivineVoice,
                Shared.Enums.MiracleType.Volcano           => SfxId.MiracleVolcano,
                Shared.Enums.MiracleType.DemonInvasion     => SfxId.MiracleDemonInvasion,
                Shared.Enums.MiracleType.DivineBeastCreation => SfxId.MiracleDivineBeast,
                Shared.Enums.MiracleType.Revelation        => SfxId.MiracleRevelation,
                Shared.Enums.MiracleType.HolyWar           => SfxId.MiracleHolyWar,
                _ => SfxId.Success
            };
            PlaySfx(sfx);

            // Adaptive: Holy War → war music
            if (evt.Miracle == Shared.Enums.MiracleType.HolyWar && !_hasWar)
            {
                _hasWar = true;
                PlayMusicLayer(MusicLayer.War, 0.6f);
            }
        }

        // Gọi từ GameManager khi nhận religion update
        public void OnReligionCountChanged(int count)
        {
            _activeReligions = count;
            if (count >= 3)
                PlayMusicLayer(MusicLayer.Religion, Mathf.Clamp01(count * 0.15f));
            else if (count == 0)
                StopMusicLayer(MusicLayer.Religion);
        }

        // Gọi từ GameManager khi có Apex entity
        public void OnApexEntitySpawned()
        {
            if (_hasApexEntity) return;
            _hasApexEntity = true;
            PlayMusicLayer(MusicLayer.Apocalypse, 0.7f);
            PlaySfx(SfxId.EntityApex, volumeScale: 1.2f);
        }

        // ─── Volume Settings ──────────────────────────────────

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("wf_sfx_vol", sfxVolume);
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            foreach (var src in _musicSources.Values)
                if (src.isPlaying) src.volume = musicVolume;
            PlayerPrefs.SetFloat("wf_music_vol", musicVolume);
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt("wf_sfx_on", enabled ? 1 : 0);
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            foreach (var src in _musicSources.Values)
                src.mute = !enabled;
            PlayerPrefs.SetInt("wf_music_on", enabled ? 1 : 0);
        }

        // ─── Init ─────────────────────────────────────────────

        private void InitSfxPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_Pool_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Enqueue(src);
            }
        }

        private void InitMusicLayers()
        {
            foreach (MusicLayer layer in Enum.GetValues(typeof(MusicLayer)))
            {
                var go = new GameObject($"Music_{layer}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.loop = true;
                src.playOnAwake = false;
                src.volume = 0f;
                _musicSources[layer] = src;
            }
        }

        private void LoadSettings()
        {
            sfxVolume   = PlayerPrefs.GetFloat("wf_sfx_vol",   0.8f);
            musicVolume = PlayerPrefs.GetFloat("wf_music_vol",  0.5f);
            _sfxEnabled   = PlayerPrefs.GetInt("wf_sfx_on",   1) == 1;
            _musicEnabled = PlayerPrefs.GetInt("wf_music_on",  1) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("wf_sfx_vol",   sfxVolume);
            PlayerPrefs.SetFloat("wf_music_vol",  musicVolume);
            PlayerPrefs.SetInt("wf_sfx_on",   _sfxEnabled   ? 1 : 0);
            PlayerPrefs.SetInt("wf_music_on",  _musicEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ─── Pool Helpers ─────────────────────────────────────

        private AudioSource GetPooledSource()
        {
            if (_sfxPool.Count > 0)
            {
                var src = _sfxPool.Dequeue();
                _activeSfx.Add(src);
                return src;
            }
            // Pool cạn: tạo thêm
            var go = new GameObject("SFX_Extra");
            go.transform.SetParent(transform);
            var extra = go.AddComponent<AudioSource>();
            _activeSfx.Add(extra);
            return extra;
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.clip = null;
            source.spatialBlend = 0f;
            source.transform.localPosition = Vector3.zero;
            _activeSfx.Remove(source);
            _sfxPool.Enqueue(source);
        }
    }
}
