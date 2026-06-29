using UnityEngine;
using WorldFaith.Client.Audio;
using WorldFaith.Client.VFX;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Client
{
    /// <summary>
    /// Bridge giữa game events và Visual/Audio feedback.
    /// Subscribe tất cả events từ GameManager và WorldFaithClient,
    /// điều phối VfxManager + AudioManager tương ứng.
    /// </summary>
    public class WorldEventVisualizer : MonoBehaviour
    {
        [Header("Tile Size")]
        [SerializeField] private float tileSize = 1f;

        private void Start()
        {
            var gm = Managers.GameManager.Instance;
            if (gm != null)
            {
                gm.OnMiracleResult    += OnMiracle;
                gm.OnRebirth          += OnRebirth;
                gm.OnGameOver         += OnGameOver;
            }

            var client = Network.WorldFaithClient.Instance;
            if (client != null)
            {
                client.OnWorldTick       += OnWorldTick;
                client.OnReligionUpdate  += OnReligionUpdate;
                client.OnCivilizationUpdate += OnCivUpdate;
            }
        }

        private void OnDestroy()
        {
            var gm = Managers.GameManager.Instance;
            if (gm != null)
            {
                gm.OnMiracleResult    -= OnMiracle;
                gm.OnRebirth          -= OnRebirth;
                gm.OnGameOver         -= OnGameOver;
            }

            var client = Network.WorldFaithClient.Instance;
            if (client != null)
            {
                client.OnWorldTick          -= OnWorldTick;
                client.OnReligionUpdate     -= OnReligionUpdate;
                client.OnCivilizationUpdate -= OnCivUpdate;
            }
        }

        // ─── Miracle ─────────────────────────────────────────

        private void OnMiracle(MiracleResultEvent evt)
        {
            if (!evt.Success)
            {
                AudioManager.Instance?.PlaySfx(SfxId.Error);
                return;
            }

            if (evt.WasCountered)
            {
                VfxManager.Instance?.PlayCounterVfx(0, 0);
                AudioManager.Instance?.PlaySfx(SfxId.MiracleCountered);
                return;
            }

            // VFX tại vị trí miracle (position lưu trong server event - dùng dummy nếu client không có)
            VfxManager.Instance?.PlayMiracleVfx(evt.Miracle, 0, 0);
        }

        // ─── World Tick Deltas ────────────────────────────────

        private void OnWorldTick(WorldTickEvent evt)
        {
            foreach (var delta in evt.Deltas)
            {
                var worldPos = new Vector3(delta.X * tileSize, 0f, delta.Y * tileSize);

                switch (delta.Type)
                {
                    case WorldEventType.EvolutionOccurred:
                        var isApex = delta.Description.Contains("Celestial")
                            || delta.Description.Contains("Apocalyptic")
                            || delta.Description.Contains("FallenDemon");

                        VfxManager.Instance?.Play(
                            isApex ? VfxId.EvolveApex : VfxId.EvolveBasic, worldPos);
                        AudioManager.Instance?.PlaySfx(
                            isApex ? SfxId.EntityApex : SfxId.EntityEvolve);

                        if (isApex)
                            AudioManager.Instance?.OnApexEntitySpawned();
                        break;

                    case WorldEventType.CivilizationCollapsed:
                        VfxManager.Instance?.Play(VfxId.CivCollapse, worldPos);
                        AudioManager.Instance?.PlaySfx(SfxId.CivCollapsed);
                        break;

                    case WorldEventType.CivilizationFounded:
                        VfxManager.Instance?.Play(VfxId.CivFounded, worldPos);
                        AudioManager.Instance?.PlaySfx(SfxId.CivFounded);
                        break;

                    case WorldEventType.HolyWar:
                        VfxManager.Instance?.Play(VfxId.Crusade, worldPos, scale: 2f);
                        AudioManager.Instance?.PlaySfx(SfxId.Crusade);
                        break;

                    case WorldEventType.GodFaded:
                        VfxManager.Instance?.Play(VfxId.GodFaded, worldPos, scale: 3f);
                        AudioManager.Instance?.PlaySfx(SfxId.GodFaded);
                        break;
                }
            }
        }

        // ─── Religion ────────────────────────────────────────

        private void OnReligionUpdate(ReligionUpdateEvent evt)
        {
            var gm = Managers.GameManager.Instance;

            switch (evt.Event)
            {
                case ReligionEvent.Founded:
                    AudioManager.Instance?.PlaySfx(SfxId.ReligionFounded);
                    break;

                case ReligionEvent.TempleBuilt:
                    AudioManager.Instance?.PlaySfx(SfxId.TempleBuilt);
                    break;

                case ReligionEvent.Conversion:
                    AudioManager.Instance?.PlaySfx(SfxId.Conversion);
                    break;

                case ReligionEvent.Schism:
                    VfxManager.Instance?.Play(VfxId.Schism, Vector3.zero);
                    AudioManager.Instance?.PlaySfx(SfxId.Schism);
                    break;

                case ReligionEvent.HeresyFormed:
                    AudioManager.Instance?.PlaySfx(SfxId.Heresy);
                    break;

                case ReligionEvent.CrusadeStarted:
                    VfxManager.Instance?.Play(VfxId.Crusade, Vector3.zero, scale: 2f);
                    AudioManager.Instance?.PlaySfx(SfxId.Crusade);
                    break;

                case ReligionEvent.ReligionErased:
                    AudioManager.Instance?.PlaySfx(SfxId.GodFaded);
                    break;
            }

            // Adaptive music: cập nhật religion count
            int count = gm?.Religions.Count ?? 0;
            AudioManager.Instance?.OnReligionCountChanged(count);
        }

        // ─── Civilization ─────────────────────────────────────

        private void OnCivUpdate(CivilizationUpdateEvent evt)
        {
            if (evt.Collapsed)
            {
                VfxManager.Instance?.Play(VfxId.CivCollapse, Vector3.zero);
                AudioManager.Instance?.PlaySfx(SfxId.CivCollapsed);
            }
        }

        // ─── World Events ─────────────────────────────────────

        private void OnRebirth(WorldRebirthEvent evt)
        {
            StartCoroutine(VfxManager.Instance?.PlayWorldRebirthSequence(Vector3.zero));
            AudioManager.Instance?.PlaySfx(SfxId.WorldRebirth, volumeScale: 1.5f);
        }

        private void OnGameOver(GameEndEvent evt)
        {
            bool won = evt.WinnerGodId == Managers.GameManager.Instance?.MyGod?.Id;
            AudioManager.Instance?.PlaySfx(won ? SfxId.Victory : SfxId.Defeat);

            if (won)
                VfxManager.Instance?.Play(VfxId.DivineBeam, Vector3.zero, scale: 5f);
        }
    }
}
