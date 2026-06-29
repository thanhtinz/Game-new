using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Audio;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Settings panel: âm thanh, gameplay, display.
    /// Accessible từ pause menu và lobby.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openBtn;
        [SerializeField] private Button closeBtn;

        [Header("Audio")]
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private Toggle musicToggle;

        [Header("Display")]
        [SerializeField] private Toggle showGridToggle;
        [SerializeField] private Toggle showEntityNamesToggle;
        [SerializeField] private Toggle showCivNamesToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Notifications (Mobile)")]
        [SerializeField] private Toggle notificationsToggle;

        private void Start()
        {
            openBtn?.onClick.AddListener(() => { panel?.SetActive(true); LoadSettings(); });
            closeBtn?.onClick.AddListener(() => { panel?.SetActive(false); SaveSettings(); });

            sfxSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSfxVolume(v);
                if (sfxValueText) sfxValueText.text = $"{(int)(v * 100)}%";
                AudioManager.Instance?.PlaySfx(SfxId.ButtonClick);
            });

            musicSlider?.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMusicVolume(v);
                if (musicValueText) musicValueText.text = $"{(int)(v * 100)}%";
            });

            sfxToggle?.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxEnabled(v));
            musicToggle?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicEnabled(v));

            qualityDropdown?.onValueChanged.AddListener(v =>
            {
                QualitySettings.SetQualityLevel(v);
                PlayerPrefs.SetInt("wf_quality", v);
            });

            panel?.SetActive(false);
            LoadSettings();
        }

        private void LoadSettings()
        {
            float sfxVol   = PlayerPrefs.GetFloat("wf_sfx_vol",   0.8f);
            float musicVol = PlayerPrefs.GetFloat("wf_music_vol",  0.5f);
            bool sfxOn     = PlayerPrefs.GetInt("wf_sfx_on",   1) == 1;
            bool musicOn   = PlayerPrefs.GetInt("wf_music_on",  1) == 1;
            int quality    = PlayerPrefs.GetInt("wf_quality",   QualitySettings.GetQualityLevel());

            if (sfxSlider)   { sfxSlider.value   = sfxVol;   }
            if (musicSlider) { musicSlider.value  = musicVol; }
            if (sfxToggle)   sfxToggle.isOn   = sfxOn;
            if (musicToggle) musicToggle.isOn  = musicOn;

            if (sfxValueText)   sfxValueText.text   = $"{(int)(sfxVol   * 100)}%";
            if (musicValueText) musicValueText.text  = $"{(int)(musicVol * 100)}%";

            if (qualityDropdown) qualityDropdown.value = Mathf.Clamp(quality, 0, qualityDropdown.options.Count - 1);

            bool notifs = PlayerPrefs.GetInt("wf_notifs", 1) == 1;
            if (notificationsToggle) notificationsToggle.isOn = notifs;

            bool grid       = PlayerPrefs.GetInt("wf_show_grid",          0) == 1;
            bool entityName = PlayerPrefs.GetInt("wf_show_entity_names",  1) == 1;
            bool civName    = PlayerPrefs.GetInt("wf_show_civ_names",     1) == 1;

            if (showGridToggle)         showGridToggle.isOn         = grid;
            if (showEntityNamesToggle)  showEntityNamesToggle.isOn  = entityName;
            if (showCivNamesToggle)     showCivNamesToggle.isOn     = civName;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("wf_notifs",           notificationsToggle?.isOn == true ? 1 : 0);
            PlayerPrefs.SetInt("wf_show_grid",        showGridToggle?.isOn == true ? 1 : 0);
            PlayerPrefs.SetInt("wf_show_entity_names", showEntityNamesToggle?.isOn == true ? 1 : 0);
            PlayerPrefs.SetInt("wf_show_civ_names",   showCivNamesToggle?.isOn == true ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
