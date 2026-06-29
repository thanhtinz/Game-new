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
    /// Commandment Panel — phát lệnh thần thánh đến civilization.
    /// God chọn civ mục tiêu, chọn loại lệnh, tùy chọn custom message.
    /// Civ có trust cao → nghe lệnh; trust thấp → phớt lờ.
    /// Faith cost: 5-30 Faith tùy loại lệnh.
    /// </summary>
    public class CommandmentPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleBtn;
        [SerializeField] private Button closeBtn;

        [Header("Civ Selection")]
        [SerializeField] private TMP_Dropdown civDropdown;
        [SerializeField] private TextMeshProUGUI civTrustText;
        [SerializeField] private Slider civTrustSlider;

        [Header("Commandment Types")]
        [SerializeField] private Transform commandmentBtnContainer;
        [SerializeField] private GameObject commandmentBtnPrefab;

        [Header("Custom Message")]
        [SerializeField] private TMP_InputField customMessageInput;
        [SerializeField] private Toggle useCustomToggle;

        [Header("Confirm")]
        [SerializeField] private Button issueBtn;
        [SerializeField] private TextMeshProUGUI issueBtnText;
        [SerializeField] private TextMeshProUGUI faithCostText;
        [SerializeField] private TextMeshProUGUI resultText;

        // Commandment types với thông tin hiển thị
        private static readonly (string type, string label, string icon, float cost, float trustReq)[] CommandmentTypes =
        {
            ("ExpandTerritory", "Mở Rộng Lãnh Thổ", "🗺️", 20f, 40f),
            ("BuildTemple",     "Xây Đền Thờ",       "🏛️", 25f, 40f),
            ("SpreadFaith",     "Lan Truyền Đức Tin", "✝️", 15f, 40f),
            ("MakeWar",         "Phát Động Chiến Tranh","⚔️", 30f, 70f),
            ("MakePeace",       "Đình Chiến",         "🕊️", 10f, 40f),
            ("FocusEconomy",    "Phát Triển Kinh Tế", "💰", 10f, 40f),
            ("FocusMilitary",   "Tăng Cường Quân Đội","🛡️", 15f, 40f),
            ("Worship",         "Thờ Phụng Ta",       "🙏", 5f,  30f),
        };

        private string _selectedCivId = "";
        private string _selectedCommandmentType = "";
        private float _selectedCost = 15f;
        private readonly List<GameObject> _commandmentBtns = new();
        private readonly List<(string id, string name)> _civList = new();

        private void Start()
        {
            toggleBtn?.onClick.AddListener(TogglePanel);
            closeBtn?.onClick.AddListener(() => panel?.SetActive(false));
            issueBtn?.onClick.AddListener(OnIssueCommandment);
            civDropdown?.onValueChanged.AddListener(OnCivSelected);
            useCustomToggle?.onValueChanged.AddListener(v => customMessageInput?.gameObject.SetActive(v));

            customMessageInput?.gameObject.SetActive(false);
            panel?.SetActive(false);

            BuildCommandmentButtons();

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnWorldLoaded += _ => RefreshCivList();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnWorldLoaded -= _ => RefreshCivList();
        }

        // ─── UI Setup ────────────────────────────────────────────

        private void BuildCommandmentButtons()
        {
            if (commandmentBtnContainer == null || commandmentBtnPrefab == null) return;

            foreach (var (type, label, icon, cost, trustReq) in CommandmentTypes)
            {
                var btn = Instantiate(commandmentBtnPrefab, commandmentBtnContainer);
                var texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = $"{icon} {label}";
                if (texts.Length > 1) texts[1].text = $"{cost} Faith";
                if (texts.Length > 2) texts[2].text = $"Trust ≥ {trustReq}";

                var capturedType = type;
                var capturedCost = cost;
                btn.GetComponent<Button>()?.onClick.AddListener(() =>
                {
                    SelectCommandment(capturedType, capturedCost);
                    // Highlight selected
                    foreach (Transform child in commandmentBtnContainer)
                    {
                        var img = child.GetComponent<Image>();
                        if (img) img.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
                    }
                    var selImg = btn.GetComponent<Image>();
                    if (selImg) selImg.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);
                });

                _commandmentBtns.Add(btn);
            }
        }

        private void RefreshCivList()
        {
            _civList.Clear();
            if (civDropdown == null) return;

            civDropdown.ClearOptions();
            var options = new List<string>();

            var gm = GameManager.Instance;
            if (gm == null) return;

            foreach (var civ in gm.Civilizations.Values)
            {
                if (civ.State.ToString() == "Fallen") continue;
                _civList.Add((civ.Id, civ.Name));
                options.Add($"{civ.Name} [{civ.State}]");
            }

            civDropdown.AddOptions(options);
            if (_civList.Any()) OnCivSelected(0);
        }

        // ─── Selection Handlers ──────────────────────────────────

        private void OnCivSelected(int index)
        {
            if (index >= _civList.Count) return;
            _selectedCivId = _civList[index].id;

            var gm = GameManager.Instance;
            if (!gm?.Civilizations.TryGetValue(_selectedCivId, out var civ) ?? true) return;

            float trust = civ.AiMemory?.GodTrustLevel ?? 0f;
            if (civTrustText)   civTrustText.text   = $"Trust: {trust:F0}/100";
            if (civTrustSlider) civTrustSlider.value = trust / 100f;
        }

        private void SelectCommandment(string type, float cost)
        {
            _selectedCommandmentType = type;
            _selectedCost = cost;

            float myFaith = GameManager.Instance?.MyGod?.Faith ?? 0f;
            bool canAfford = myFaith >= cost;

            if (faithCostText) faithCostText.text = $"Chi phí: {cost} Faith (bạn có: {myFaith:F0})";
            if (issueBtnText)  issueBtnText.text  = canAfford ? "⚡ Phán Lệnh!" : "Không đủ Faith";
            if (issueBtn)      issueBtn.interactable = canAfford;
        }

        private void TogglePanel()
        {
            bool active = !(panel?.activeSelf ?? false);
            panel?.SetActive(active);
            if (active) RefreshCivList();
        }

        // ─── Issue Commandment ───────────────────────────────────

        private async void OnIssueCommandment()
        {
            if (string.IsNullOrEmpty(_selectedCivId))
            {
                if (resultText) resultText.text = "Chọn civilization trước!";
                return;
            }
            if (string.IsNullOrEmpty(_selectedCommandmentType))
            {
                if (resultText) resultText.text = "Chọn loại lệnh trước!";
                return;
            }

            string? customMsg = (useCustomToggle?.isOn == true)
                ? customMessageInput?.text?.Trim() : null;

            if (!string.IsNullOrEmpty(customMsg) && customMsg.Length > 100)
            {
                if (resultText) resultText.text = "Lời phán tối đa 100 ký tự!";
                return;
            }

            await WorldFaithClient.Instance.IssueCommandmentAsync(
                _selectedCivId, _selectedCommandmentType, customMsg);

            if (resultText) resultText.text = "Đã phán lệnh! Đang chờ phản hồi...";
        }
    }
}
