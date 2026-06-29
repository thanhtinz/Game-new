using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Tutorial System — hướng dẫn từng bước cho người chơi mới.
    /// Hiển thị tooltip highlight + mũi tên chỉ vào UI element.
    /// Tự động bỏ qua nếu người chơi đã từng chơi.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        [Header("Tutorial Overlay")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Image dimOverlay;              // Làm tối background
        [SerializeField] private Image highlightFrame;          // Border sáng quanh UI cần focus
        [SerializeField] private RectTransform arrowIndicator;  // Mũi tên chỉ vào target

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI stepNumberText;
        [SerializeField] private TextMeshProUGUI tooltipTitle;
        [SerializeField] private TextMeshProUGUI tooltipBody;

        [Header("Controls")]
        [SerializeField] private Button nextBtn;
        [SerializeField] private Button skipBtn;
        [SerializeField] private TextMeshProUGUI nextBtnText;
        [SerializeField] private TextMeshProUGUI progressText;

        // Tutorial steps
        private struct TutorialStep
        {
            public string Title;
            public string Body;
            public string TargetTag;   // Tag của GameObject cần highlight
            public float HighlightW;
            public float HighlightH;
            public Vector2 TooltipOffset;
            public bool WaitForAction; // Chờ người dùng click vào target
        }

        private static readonly TutorialStep[] Steps =
        {
            new()
            {
                Title = "Chào mừng đến với WorldFaith!",
                Body  = "Bạn là một vị thần vừa thức tỉnh trong một thế giới mới.\nHãy thu phục tín đồ, thực hiện phép màu, và trở thành thần duy nhất được nhớ đến.",
                TargetTag = ""
            },
            new()
            {
                Title = "⚡ Thanh Faith của bạn",
                Body  = "Faith là nguồn năng lượng của bạn.\nFaith tăng tự động từ: Followers + Temples + Devotion.\nFaith được dùng để thực hiện Miracle.",
                TargetTag    = "FaithBar",
                HighlightW   = 300f,
                HighlightH   = 60f,
                TooltipOffset = new Vector2(0, -100f)
            },
            new()
            {
                Title = "👥 Tín đồ (Followers)",
                Body  = "Followers là số người tin vào bạn.\nCàng nhiều followers → Faith tăng càng nhanh.\nDùng Miracle 'Dream' và 'BlessHarvest' để tăng trust với civilizations.",
                TargetTag    = "FollowersCounter",
                HighlightW   = 200f,
                HighlightH   = 50f,
                TooltipOffset = new Vector2(0, -80f)
            },
            new()
            {
                Title = "✨ Tab Miracle",
                Body  = "Đây là nơi bạn thực hiện phép màu.\nMỗi miracle có chi phí Faith khác nhau.\n💡 Bắt đầu với 'Dream' (5 Faith) để tăng trust!",
                TargetTag    = "MiracleTabBtn",
                HighlightW   = 100f,
                HighlightH   = 50f,
                TooltipOffset = new Vector2(150f, 0)
            },
            new()
            {
                Title = "✝ Sáng Lập Tôn Giáo",
                Body  = "Sau khi có đủ trust với 1-2 civilizations, hãy vào tab 'Tôn Giáo' và sáng lập tôn giáo.\nTôn giáo sẽ tự spread và tăng Faith cho bạn liên tục.",
                TargetTag    = "ReligionTabBtn",
                HighlightW   = 100f,
                HighlightH   = 50f,
                TooltipOffset = new Vector2(150f, 0)
            },
            new()
            {
                Title = "🌍 World Map",
                Body  = "Xem toàn bộ thế giới qua bản đồ.\nMàu sắc cho biết địa hình, civ đang kiểm soát, và tôn giáo đang spread.\nClick tile để xem thông tin và thực hiện miracle tại đó.",
                TargetTag    = "MapTabBtn",
                HighlightW   = 100f,
                HighlightH   = 50f,
                TooltipOffset = new Vector2(150f, 0)
            },
            new()
            {
                Title = "⚡ Counter Miracle",
                Body  = "Khi rival god dùng miracle, sẽ xuất hiện thông báo màu đỏ góc trên.\nBạn có 5 giây để PHẢN PHÉP bằng cách click và chọn counter miracle.\nPhản phép thành công → miracle của địch bị hủy!",
                TargetTag    = "",
                TooltipOffset = Vector2.zero
            },
            new()
            {
                Title = "🔄 Rebirth Cycle",
                Body  = "Mỗi 1000 ticks, thế giới sẽ 'Rebirth' — chu kỳ mới bắt đầu.\nGod nào hết followers sẽ biến mất mãi mãi.\nDuy trì followers để sống sót qua nhiều chu kỳ!",
                TargetTag    = "",
                TooltipOffset = Vector2.zero
            },
            new()
            {
                Title = "Sẵn sàng chưa?",
                Body  = "Bạn đã biết những điều cơ bản!\nHãy bắt đầu hành trình của mình.\nChúc may mắn, hỡi vị thần! ⚡",
                TargetTag    = "",
                TooltipOffset = Vector2.zero
            }
        };

        private int _currentStep = 0;
        private const string TutorialDoneKey = "wf_tutorial_done";

        private void Start()
        {
            nextBtn?.onClick.AddListener(NextStep);
            skipBtn?.onClick.AddListener(SkipTutorial);

            tutorialPanel?.SetActive(false);

            // Hiển thị tutorial nếu chơi lần đầu
            bool done = PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1;
            if (!done)
                StartCoroutine(StartTutorialDelayed());
        }

        private IEnumerator StartTutorialDelayed()
        {
            // Đợi world load xong
            yield return new WaitForSeconds(2f);
            StartTutorial();
        }

        // ─── Tutorial Flow ───────────────────────────────────────

        public void StartTutorial()
        {
            _currentStep = 0;
            tutorialPanel?.SetActive(true);
            ShowStep(_currentStep);
        }

        private void ShowStep(int index)
        {
            if (index >= Steps.Length)
            {
                EndTutorial();
                return;
            }

            var step = Steps[index];

            if (stepNumberText) stepNumberText.text = $"Bước {index + 1}/{Steps.Length}";
            if (tooltipTitle)   tooltipTitle.text   = step.Title;
            if (tooltipBody)    tooltipBody.text     = step.Body;
            if (progressText)   progressText.text    = $"{index + 1} / {Steps.Length}";
            if (nextBtnText)    nextBtnText.text     = index == Steps.Length - 1 ? "Bắt đầu!" : "Tiếp >";

            // Highlight target UI
            if (!string.IsNullOrEmpty(step.TargetTag))
            {
                var target = GameObject.FindGameObjectWithTag(step.TargetTag);
                if (target != null)
                {
                    var targetRt = target.GetComponent<RectTransform>();
                    if (targetRt != null && highlightFrame != null)
                    {
                        highlightFrame.gameObject.SetActive(true);
                        highlightFrame.rectTransform.position = targetRt.position;
                        highlightFrame.rectTransform.sizeDelta = new Vector2(step.HighlightW, step.HighlightH);
                    }
                }
            }
            else
            {
                highlightFrame?.gameObject.SetActive(false);
            }

            // Tooltip position
            if (tooltipPanel != null)
            {
                var rt = tooltipPanel.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = step.TooltipOffset;
            }
        }

        private void NextStep()
        {
            _currentStep++;
            ShowStep(_currentStep);
        }

        private void SkipTutorial()
        {
            EndTutorial();
        }

        private void EndTutorial()
        {
            tutorialPanel?.SetActive(false);
            PlayerPrefs.SetInt(TutorialDoneKey, 1);
            PlayerPrefs.Save();
        }

        // ─── Reset (for testing) ─────────────────────────────────

        [ContextMenu("Reset Tutorial")]
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(TutorialDoneKey);
            PlayerPrefs.Save();
            Debug.Log("[Tutorial] Reset — sẽ hiện lại lần sau");
        }
    }
}
