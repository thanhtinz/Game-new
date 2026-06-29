using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Client.Network;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// God Selection Screen — hiển thị trước khi join world.
    /// Chọn tên god + archetype với preview bonus stats.
    /// Mở từ LobbyUI sau khi join room, trước khi set ready.
    /// </summary>
    public class GodSelectionScreen : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private GameObject screen;
        [SerializeField] private Button openBtn;

        [Header("Name Input")]
        [SerializeField] private TMP_InputField godNameInput;
        [SerializeField] private TextMeshProUGUI nameErrorText;

        [Header("Archetype Grid")]
        [SerializeField] private Transform archetypeContainer;
        [SerializeField] private GameObject archetypeCardPrefab;

        [Header("Preview Panel")]
        [SerializeField] private TextMeshProUGUI previewArchetypeText;
        [SerializeField] private TextMeshProUGUI previewDescText;
        [SerializeField] private TextMeshProUGUI previewFaithBonusText;
        [SerializeField] private TextMeshProUGUI previewSpecialText;
        [SerializeField] private Image previewIcon;

        [Header("Confirm")]
        [SerializeField] private Button confirmBtn;
        [SerializeField] private TextMeshProUGUI confirmBtnText;

        private GodArchetype _selectedArchetype = GodArchetype.Order;
        private bool _hasSelected = false;

        // Archetype data
        private static readonly (GodArchetype arch, string icon, string desc, string faithBonus, string special, Color color)[]
            ArchetypeData =
        {
            (GodArchetype.Order,     "⚖️", "Cân bằng, ổn định. Dễ dàng duy trì followers.",
             "+10% Faith từ temples", "Miracle Revelation giảm 20% cost", new Color(0.8f, 0.9f, 1f)),

            (GodArchetype.Chaos,     "🌪️", "Hỗn loạn, bất ngờ. Miracle có hiệu ứng ngẫu nhiên mạnh hơn.",
             "+15% Faith khi civ đang war", "Schism trigger tăng 50%", new Color(1f, 0.4f, 0.8f)),

            (GodArchetype.Light,     "☀️", "Ánh sáng, chữa lành. Tăng trust nhanh hơn.",
             "+20% Faith từ followers có Trust cao", "HealFollower không tốn Faith", new Color(1f, 1f, 0.6f)),

            (GodArchetype.Darkness,  "🌑", "Bóng tối, nguyền rủa. Thu Faith từ Fear thay vì Trust.",
             "+Fear×0.05 Faith/tick thêm", "Curse hiệu ứng x2", new Color(0.5f, 0.1f, 0.7f)),

            (GodArchetype.Nature,    "🌿", "Thiên nhiên, tiến hóa. Evolution entities mạnh hơn.",
             "+10% Faith từ Forest tiles", "Evolution sacred bonus +0.5x", new Color(0.3f, 0.8f, 0.3f)),

            (GodArchetype.Death,     "💀", "Cái chết, tái sinh. Mạnh khi civ collapsing.",
             "+30% Faith khi civ sụp đổ", "Thu Faith từ Fear như Dark god", new Color(0.6f, 0.6f, 0.6f)),

            (GodArchetype.Knowledge, "📚", "Tri thức, khai sáng. Mở khóa miracle nhanh hơn.",
             "+5% Faith/miracle thực hiện", "DivineVoice hiệu quả x1.5", new Color(0.3f, 0.8f, 1f)),

            (GodArchetype.War,       "⚔️", "Chiến tranh, sức mạnh. Civ của bạn military mạnh hơn.",
             "+10% Faith khi civ win war", "HolyWar giảm 30% cost", new Color(1f, 0.2f, 0.2f)),
        };

        private void Start()
        {
            openBtn?.onClick.AddListener(() => screen?.SetActive(true));
            confirmBtn?.onClick.AddListener(OnConfirm);
            godNameInput?.onValueChanged.AddListener(_ => { if (nameErrorText) nameErrorText.text = ""; });

            BuildArchetypeCards();
            SelectArchetype(GodArchetype.Order);
            screen?.SetActive(false);
        }

        // ─── Build Cards ─────────────────────────────────────────

        private void BuildArchetypeCards()
        {
            if (archetypeContainer == null || archetypeCardPrefab == null) return;

            foreach (var (arch, icon, desc, faithBonus, special, color) in ArchetypeData)
            {
                var card  = Instantiate(archetypeCardPrefab, archetypeContainer);
                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                var img   = card.GetComponent<Image>();

                if (texts.Length > 0) texts[0].text = icon;
                if (texts.Length > 1) texts[1].text = arch.ToString();
                if (img != null)     img.color       = new Color(color.r, color.g, color.b, 0.2f);

                var capturedArch = arch;
                var btn = card.GetComponent<Button>() ?? card.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectArchetype(capturedArch));
            }
        }

        private void SelectArchetype(GodArchetype arch)
        {
            _selectedArchetype = arch;
            _hasSelected = true;

            var data = System.Array.Find(ArchetypeData, d => d.arch == arch);

            if (previewArchetypeText) previewArchetypeText.text = $"{data.icon} {arch}";
            if (previewDescText)      previewDescText.text      = data.desc;
            if (previewFaithBonusText) previewFaithBonusText.text = $"⚡ {data.faithBonus}";
            if (previewSpecialText)   previewSpecialText.text   = $"✨ {data.special}";

            // Highlight selected card
            int idx = System.Array.FindIndex(ArchetypeData, d => d.arch == arch);
            for (int i = 0; i < archetypeContainer.childCount; i++)
            {
                var img = archetypeContainer.GetChild(i).GetComponent<Image>();
                if (img == null) continue;
                var c = ArchetypeData[i < ArchetypeData.Length ? i : 0].color;
                img.color = i == idx
                    ? new Color(c.r, c.g, c.b, 0.7f)
                    : new Color(c.r, c.g, c.b, 0.2f);
            }
        }

        // ─── Confirm ─────────────────────────────────────────────

        private void OnConfirm()
        {
            var name = godNameInput?.text?.Trim() ?? "";

            if (string.IsNullOrEmpty(name))
            {
                if (nameErrorText) nameErrorText.text = "Nhập tên thần của bạn!";
                return;
            }
            if (name.Length < 2 || name.Length > 20)
            {
                if (nameErrorText) nameErrorText.text = "Tên từ 2-20 ký tự";
                return;
            }

            // Lưu lựa chọn để LobbyUI dùng khi SetReady
            PlayerPrefs.SetString("wf_god_name",      name);
            PlayerPrefs.SetString("wf_god_archetype", _selectedArchetype.ToString());
            PlayerPrefs.Save();

            // Thông báo LobbyClient
            _ = LobbyClient.Instance?.SetReadyAsync(
                isReady: false,
                godName:  name,
                archetype: _selectedArchetype.ToString());

            screen?.SetActive(false);

            Debug.Log($"[GodSelection] Chọn: {name} — {_selectedArchetype}");
        }

        /// <summary>Mở screen selection từ bên ngoài (LobbyUI gọi).</summary>
        public void Open() => screen?.SetActive(true);
    }
}
