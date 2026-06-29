using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WorldFaith.Client.Managers;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Client.UI.Game
{
    /// <summary>
    /// Victory/Defeat Screen — hiển thị khi game kết thúc.
    /// Stats: followers, miracles, religions, crusades, entities evolved.
    /// Ranking của tất cả gods trong world.
    /// Animates in từ trên xuống.
    /// </summary>
    public class VictoryDefeatScreen : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private GameObject screen;
        [SerializeField] private CanvasGroup screenCanvasGroup;
        [SerializeField] private RectTransform screenRect;

        [Header("Result")]
        [SerializeField] private TextMeshProUGUI resultTitleText;   // CHIẾN THẮNG / THẤT BẠI
        [SerializeField] private TextMeshProUGUI resultSubtitleText;
        [SerializeField] private Image resultBg;                    // màu xanh/đỏ

        [Header("My Stats")]
        [SerializeField] private TextMeshProUGUI myGodNameText;
        [SerializeField] private TextMeshProUGUI myArchetypeText;
        [SerializeField] private TextMeshProUGUI myFollowersText;
        [SerializeField] private TextMeshProUGUI myCyclesText;
        [SerializeField] private TextMeshProUGUI myReligionsText;

        [Header("Rankings")]
        [SerializeField] private Transform rankingContainer;
        [SerializeField] private GameObject rankingItemPrefab;

        [Header("Victory Condition")]
        [SerializeField] private TextMeshProUGUI victoryConditionText;

        [Header("Buttons")]
        [SerializeField] private Button playAgainBtn;
        [SerializeField] private Button lobbyBtn;
        [SerializeField] private Button leaderboardBtn;

        [Header("FX")]
        [SerializeField] private ParticleSystem victoryParticles;
        [SerializeField] private ParticleSystem defeatParticles;
        [SerializeField] private float animDuration = 0.8f;

        private void Start()
        {
            playAgainBtn?.onClick.AddListener(OnPlayAgain);
            lobbyBtn?.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));
            leaderboardBtn?.onClick.AddListener(OnShowLeaderboard);

            screen?.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnGameOver += ShowResult;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnGameOver -= ShowResult;
        }

        // ─── Show Result ─────────────────────────────────────────

        private void ShowResult(GameEndEvent evt)
        {
            screen?.SetActive(true);

            var gm    = GameManager.Instance;
            var myGod = gm?.MyGod;
            bool won  = evt.WinnerGodId == myGod?.Id;

            // Title & colors
            if (resultTitleText)
                resultTitleText.text = won ? "⚡ CHIẾN THẮNG ⚡" : "💀 THẤT BẠI 💀";

            if (resultSubtitleText)
                resultSubtitleText.text = won
                    ? "Đức tin của bạn đã chinh phục thế giới!"
                    : "Tên bạn bị lãng quên trong bụi thời gian...";

            if (resultBg)
                resultBg.color = won
                    ? new Color(0.1f, 0.3f, 0.6f, 0.95f)
                    : new Color(0.4f, 0.05f, 0.05f, 0.95f);

            // My god stats
            if (myGod != null)
            {
                if (myGodNameText)    myGodNameText.text    = myGod.Name;
                if (myArchetypeText)  myArchetypeText.text  = myGod.Archetype.ToString();
                if (myFollowersText)  myFollowersText.text  = $"👥 {myGod.FollowerCount:N0} tín đồ";
                if (myCyclesText)     myCyclesText.text     = $"🔄 {gm.CurrentCycle} chu kỳ sống sót";
                if (myReligionsText)
                {
                    int relCount = 0;
                    foreach (var r in gm.Religions.Values)
                        if (r.GodId == myGod.Id) relCount++;
                    myReligionsText.text = $"✝ {relCount} tôn giáo";
                }
            }

            // Victory condition
            if (victoryConditionText)
            {
                string condName = evt.VictoryCondition switch
                {
                    "LastSurvivingGod"        => "Thần cuối cùng còn lại",
                    "HighestFaithAfterCycle"  => "Faith cao nhất sau chu kỳ",
                    "DominantReligionControl" => "Tôn giáo thống trị",
                    _ => evt.VictoryCondition
                };
                string winner = evt.WinnerGodId != null && gm?.Gods.TryGetValue(evt.WinnerGodId, out var wg) == true
                    ? wg.Name : "Unknown";
                victoryConditionText.text = $"Điều kiện thắng: {condName}\nNgười thắng: {winner}";
            }

            // Rankings
            BuildRankings(evt, gm);

            // FX
            if (won)
                victoryParticles?.Play();
            else
                defeatParticles?.Play();

            // Animate in
            StartCoroutine(AnimateIn());
        }

        private void BuildRankings(GameEndEvent evt, GameManager gm)
        {
            if (rankingContainer == null || rankingItemPrefab == null || gm == null) return;

            foreach (Transform child in rankingContainer) Destroy(child.gameObject);

            // Sort by rank
            var sorted = new List<(string godId, int rank)>();
            foreach (var (godId, rank) in evt.FinalRankings)
                sorted.Add((godId, rank));
            sorted.Sort((a, b) => a.rank.CompareTo(b.rank));

            string[] rankIcons = { "🥇", "🥈", "🥉" };

            foreach (var (godId, rank) in sorted)
            {
                if (!gm.Gods.TryGetValue(godId, out var god)) continue;

                var item  = Instantiate(rankingItemPrefab, rankingContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();

                bool isMe = godId == gm.MyGod?.Id;
                string rankStr = rank <= 3 ? rankIcons[rank - 1] : $"#{rank}";

                if (texts.Length > 0) texts[0].text = rankStr;
                if (texts.Length > 1)
                {
                    texts[1].text      = god.Name;
                    texts[1].fontStyle = isMe ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                }
                if (texts.Length > 2) texts[2].text = god.Archetype.ToString();
                if (texts.Length > 3) texts[3].text = $"👥 {god.FollowerCount:N0}";

                var bg = item.GetComponent<Image>();
                if (bg != null && isMe) bg.color = new Color(0.3f, 0.2f, 0.5f, 0.5f);
            }
        }

        // ─── Buttons ─────────────────────────────────────────────

        private async void OnPlayAgain()
        {
            // Clear world id và quay về lobby
            PlayerPrefs.DeleteKey("wf_world_id");
            PlayerPrefs.Save();
            SceneManager.LoadScene("LobbyScene");
        }

        private void OnShowLeaderboard()
        {
            var lb = FindObjectOfType<LeaderboardPanel>();
            lb?.gameObject.SetActive(true);
        }

        // ─── Animation ───────────────────────────────────────────

        private IEnumerator AnimateIn()
        {
            if (screenCanvasGroup == null) yield break;

            screenCanvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                screenCanvasGroup.alpha = Mathf.Clamp01(elapsed / animDuration);
                yield return null;
            }

            screenCanvasGroup.alpha = 1f;
        }
    }
}
