using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Client.UI
{
    /// <summary>
    /// HUD hiển thị thông tin god: Faith, Trust, Fear, Followers.
    /// Gắn vào Canvas -> GodPanel GameObject.
    /// </summary>
    public class GodPanel : MonoBehaviour
    {
        [Header("God Info")]
        [SerializeField] private TextMeshProUGUI godNameText;
        [SerializeField] private TextMeshProUGUI archetypeText;
        [SerializeField] private TextMeshProUGUI followersText;

        [Header("Resource Bars")]
        [SerializeField] private Slider faithBar;
        [SerializeField] private TextMeshProUGUI faithText;
        [SerializeField] private Slider trustBar;
        [SerializeField] private TextMeshProUGUI trustText;
        [SerializeField] private Slider fearBar;
        [SerializeField] private TextMeshProUGUI fearText;

        [Header("World Info")]
        [SerializeField] private TextMeshProUGUI tickText;
        [SerializeField] private TextMeshProUGUI cycleText;

        [Header("Notification")]
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3f;
        private float _notificationTimer;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnJoinedWorld += _ => RefreshGodInfo();
            gm.OnTick += (tick, cycle) => UpdateTickDisplay(tick, cycle);
            gm.OnNotification += ShowNotification;

            var client = WorldFaith.Client.Network.WorldFaithClient.Instance;
            if (client != null)
                client.OnGodUpdate += _ => RefreshGodInfo();
        }

        private void Update()
        {
            if (_notificationTimer > 0)
            {
                _notificationTimer -= Time.deltaTime;
                if (_notificationTimer <= 0 && notificationText != null)
                    notificationText.text = "";
            }
        }

        private void RefreshGodInfo()
        {
            var god = GameManager.Instance?.MyGod;
            if (god == null) return;

            if (godNameText != null) godNameText.text = god.Name;
            if (archetypeText != null) archetypeText.text = god.Archetype.ToString();
            if (followersText != null) followersText.text = $"Tín đồ: {god.FollowerCount:N0}";

            const float maxFaith = 1000f;
            if (faithBar != null) faithBar.value = god.Faith / maxFaith;
            if (faithText != null) faithText.text = $"Faith: {god.Faith:F0}";

            if (trustBar != null) trustBar.value = god.Trust / 100f;
            if (trustText != null) trustText.text = $"Trust: {god.Trust:F0}";

            if (fearBar != null) fearBar.value = god.Fear / 100f;
            if (fearText != null) fearText.text = $"Fear: {god.Fear:F0}";
        }

        private void UpdateTickDisplay(long tick, int cycle)
        {
            if (tickText != null) tickText.text = $"Tick: {tick}";
            if (cycleText != null) cycleText.text = $"Chu kỳ: {cycle}";
        }

        private void ShowNotification(string message)
        {
            if (notificationText == null) return;
            notificationText.text = message;
            _notificationTimer = notificationDuration;
        }
    }

    /// <summary>
    /// Panel chọn miracle. Gắn vào Canvas -> MiraclePanel.
    /// </summary>
    public class MiraclePanel : MonoBehaviour
    {
        [Header("Miracle Buttons")]
        [SerializeField] private Button rainButton;
        [SerializeField] private Button dreamButton;
        [SerializeField] private Button blessHarvestButton;
        [SerializeField] private Button stormButton;
        [SerializeField] private Button earthquakeButton;
        [SerializeField] private Button curseButton;
        [SerializeField] private Button volcanoButton;
        [SerializeField] private Button holyWarButton;

        [Header("Cost Labels")]
        [SerializeField] private TextMeshProUGUI rainCostText;
        [SerializeField] private TextMeshProUGUI stormCostText;

        // Tọa độ tile được chọn
        private int _selectedX;
        private int _selectedY;
        private string _selectedCivId;

        private void Start()
        {
            rainButton?.onClick.AddListener(() => CastMiracle(MiracleType.Rain));
            dreamButton?.onClick.AddListener(() => CastMiracle(MiracleType.Dream));
            blessHarvestButton?.onClick.AddListener(() => CastMiracle(MiracleType.BlessHarvest));
            stormButton?.onClick.AddListener(() => CastMiracle(MiracleType.Storm));
            earthquakeButton?.onClick.AddListener(() => CastMiracle(MiracleType.Earthquake));
            curseButton?.onClick.AddListener(() => CastMiracle(MiracleType.Curse));
            volcanoButton?.onClick.AddListener(() => CastMiracle(MiracleType.Volcano));
            holyWarButton?.onClick.AddListener(() => CastMiracle(MiracleType.HolyWar));

            // Hiển thị cost
            if (rainCostText != null) rainCostText.text = "10 Faith";
            if (stormCostText != null) stormCostText.text = "30 Faith";
        }

        public void SetTarget(int x, int y, string civId = null)
        {
            _selectedX = x;
            _selectedY = y;
            _selectedCivId = civId;
        }

        private void CastMiracle(MiracleType miracle)
        {
            GameManager.Instance?.PerformMiracle(miracle, _selectedX, _selectedY, _selectedCivId);
        }
    }
}
