using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Models;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Panel quản lý tôn giáo của god:
    /// - Sáng lập tôn giáo mới
    /// - Xây temple tại civilization
    /// - Xem danh sách religions và stats
    /// - Theo dõi devotion, followers, temples
    /// </summary>
    public class ReligionPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleBtn;
        [SerializeField] private Button closeBtn;

        [Header("Tab Bar")]
        [SerializeField] private Button tabMyReligionsBtn;
        [SerializeField] private Button tabAllReligionsBtn;
        [SerializeField] private Button tabFoundBtn;

        [Header("My Religions Tab")]
        [SerializeField] private Transform myReligionListContainer;
        [SerializeField] private GameObject religionItemPrefab;

        [Header("All Religions Tab")]
        [SerializeField] private Transform allReligionListContainer;
        [SerializeField] private GameObject allReligionItemPrefab;

        [Header("Found Religion Tab")]
        [SerializeField] private TMP_InputField religionNameInput;
        [SerializeField] private Toggle hiddenToggle;
        [SerializeField] private Button foundBtn;
        [SerializeField] private TextMeshProUGUI foundErrorText;

        [Header("Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailGodText;
        [SerializeField] private TextMeshProUGUI detailFollowersText;
        [SerializeField] private TextMeshProUGUI detailTemplesText;
        [SerializeField] private Slider devotionSlider;
        [SerializeField] private TextMeshProUGUI devotionText;
        [SerializeField] private Transform civListForTemple;
        [SerializeField] private GameObject civButtonPrefab;
        [SerializeField] private Button closeDetailBtn;

        [Header("Notification")]
        [SerializeField] private TextMeshProUGUI notifText;

        private enum ActiveTab { MyReligions, AllReligions, Found }
        private ActiveTab _currentTab = ActiveTab.MyReligions;
        private ReligionDto _selectedReligion;
        private readonly List<GameObject> _myItems = new();
        private readonly List<GameObject> _allItems = new();
        private readonly List<GameObject> _civButtons = new();

        private void Start()
        {
            toggleBtn?.onClick.AddListener(TogglePanel);
            closeBtn?.onClick.AddListener(() => panel?.SetActive(false));
            tabMyReligionsBtn?.onClick.AddListener(() => SwitchTab(ActiveTab.MyReligions));
            tabAllReligionsBtn?.onClick.AddListener(() => SwitchTab(ActiveTab.AllReligions));
            tabFoundBtn?.onClick.AddListener(() => SwitchTab(ActiveTab.Found));
            foundBtn?.onClick.AddListener(OnFoundReligion);
            closeDetailBtn?.onClick.AddListener(() => detailPanel?.SetActive(false));

            panel?.SetActive(false);
            detailPanel?.SetActive(false);

            // Subscribe events
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded += _ => RefreshCurrentTab();
                gm.OnReligionUpdate += _ => RefreshCurrentTab();
            }
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWorldLoaded -= _ => RefreshCurrentTab();
                gm.OnReligionUpdate -= _ => RefreshCurrentTab();
            }
        }

        // ─── Toggle & Tab ────────────────────────────────────────

        private void TogglePanel()
        {
            bool active = !(panel?.activeSelf ?? false);
            panel?.SetActive(active);
            if (active) RefreshCurrentTab();
        }

        private void SwitchTab(ActiveTab tab)
        {
            _currentTab = tab;
            myReligionListContainer?.gameObject.SetActive(tab == ActiveTab.MyReligions);
            allReligionListContainer?.gameObject.SetActive(tab == ActiveTab.AllReligions);
            var foundRoot = foundBtn?.transform.parent.gameObject;
            foundRoot?.SetActive(tab == ActiveTab.Found);
            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            switch (_currentTab)
            {
                case ActiveTab.MyReligions: RefreshMyReligions(); break;
                case ActiveTab.AllReligions: RefreshAllReligions(); break;
            }
        }

        // ─── My Religions ────────────────────────────────────────

        private void RefreshMyReligions()
        {
            foreach (var item in _myItems) Destroy(item);
            _myItems.Clear();

            var gm = GameManager.Instance;
            if (gm == null || myReligionListContainer == null || religionItemPrefab == null) return;

            var myGodId = gm.MyGod?.Id ?? "";
            var myReligions = new List<ReligionDto>();
            foreach (var r in gm.Religions.Values)
                if (r.GodId == myGodId) myReligions.Add(r);

            if (!myReligions.Any())
            {
                ShowNotif("Bạn chưa có tôn giáo nào. Hãy sáng lập tôn giáo đầu tiên!");
                return;
            }

            foreach (var religion in myReligions)
                _myItems.Add(CreateReligionItem(religion, myReligionListContainer, isOwned: true));
        }

        private void RefreshAllReligions()
        {
            foreach (var item in _allItems) Destroy(item);
            _allItems.Clear();

            var gm = GameManager.Instance;
            if (gm == null || allReligionListContainer == null || allReligionItemPrefab == null) return;

            var sorted = new List<ReligionDto>(gm.Religions.Values);
            sorted.Sort((a, b) => b.FollowerCount.CompareTo(a.FollowerCount));

            foreach (var religion in sorted)
                if (!religion.IsHidden)
                    _allItems.Add(CreateReligionItem(religion, allReligionListContainer, isOwned: false));
        }

        private GameObject CreateReligionItem(ReligionDto religion, Transform container, bool isOwned)
        {
            var item = Instantiate(religionItemPrefab, container);
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            string hiddenIcon = religion.IsHidden ? " 🔒" : "";
            if (texts.Length > 0) texts[0].text = religion.Name + hiddenIcon;
            if (texts.Length > 1) texts[1].text = $"👥 {religion.FollowerCount:N0}";
            if (texts.Length > 2) texts[2].text = $"🏛 {religion.TempleCount}";

            // Devotion bar color
            var slider = item.GetComponentInChildren<Slider>();
            if (slider != null) slider.value = religion.DevotionLevel;

            var btn = item.GetComponent<Button>();
            btn?.onClick.AddListener(() => ShowReligionDetail(religion, isOwned));

            return item;
        }

        // ─── Religion Detail ─────────────────────────────────────

        private void ShowReligionDetail(ReligionDto religion, bool isOwned)
        {
            _selectedReligion = religion;
            detailPanel?.SetActive(true);

            var gm = GameManager.Instance;
            string godName = gm?.Gods.TryGetValue(religion.GodId, out var god) == true ? god.Name : "Unknown";

            if (detailNameText)     detailNameText.text     = religion.Name;
            if (detailGodText)      detailGodText.text      = $"Thần: {godName}";
            if (detailFollowersText) detailFollowersText.text = $"Tín đồ: {religion.FollowerCount:N0}";
            if (detailTemplesText)  detailTemplesText.text  = $"Đền thờ: {religion.TempleCount}";
            if (devotionSlider)     devotionSlider.value    = religion.DevotionLevel;
            if (devotionText)       devotionText.text       = $"Devotion: {religion.DevotionLevel:P0}";

            // Danh sách civ để xây temple (chỉ nếu là religion của mình)
            foreach (var btn in _civButtons) Destroy(btn);
            _civButtons.Clear();

            if (isOwned && civListForTemple != null && civButtonPrefab != null && gm != null)
            {
                foreach (var civ in gm.Civilizations.Values)
                {
                    if (civ.State.ToString() == "Fallen") continue;
                    var civBtn = Instantiate(civButtonPrefab, civListForTemple);
                    var btnText = civBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText) btnText.text = $"Xây temple tại {civ.Name}";
                    var capturedCiv = civ;
                    civBtn.GetComponent<Button>()?.onClick.AddListener(
                        () => BuildTempleAt(religion.Id, capturedCiv.Id, capturedCiv.Name));
                    _civButtons.Add(civBtn);
                }
            }
        }

        // ─── Actions ─────────────────────────────────────────────

        private async void OnFoundReligion()
        {
            var name = religionNameInput?.text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                if (foundErrorText) foundErrorText.text = "Nhập tên tôn giáo!";
                return;
            }
            if (name.Length < 2 || name.Length > 30)
            {
                if (foundErrorText) foundErrorText.text = "Tên phải từ 2-30 ký tự";
                return;
            }

            bool isHidden = hiddenToggle?.isOn ?? false;
            await WorldFaithClient.Instance.FoundReligionAsync(name, isHidden);

            if (foundErrorText) foundErrorText.text = "";
            if (religionNameInput) religionNameInput.text = "";
            ShowNotif($"Đã sáng lập tôn giáo: {name}" + (isHidden ? " (bí mật)" : ""));
            SwitchTab(ActiveTab.MyReligions);
        }

        private async void BuildTempleAt(string religionId, string civId, string civName)
        {
            var myFaith = GameManager.Instance?.MyGod?.Faith ?? 0f;
            if (myFaith < 20f)
            {
                ShowNotif("Không đủ Faith để xây temple (cần 20)");
                return;
            }

            await WorldFaithClient.Instance.BuildTempleAsync(religionId, civId);
            ShowNotif($"Đang xây temple tại {civName}...");
            detailPanel?.SetActive(false);
        }

        private void ShowNotif(string msg)
        {
            if (notifText == null) return;
            notifText.text = msg;
            StopAllCoroutines();
            StartCoroutine(ClearNotif());
        }

        private IEnumerator ClearNotif()
        {
            yield return new WaitForSeconds(3f);
            if (notifText) notifText.text = "";
        }
    }
}
