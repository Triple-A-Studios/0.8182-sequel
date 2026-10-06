using Alchemy.Inspector;
using Opoint8182.Game;
using Opoint8182.Leaderboard;
using Opoint8182.Score;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GameOverUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "game-over-root";
        [FoldoutGroup("References")] [SerializeField] private string m_scoreLabelName = "game-over-score-label";
        [FoldoutGroup("References")] [SerializeField] private string m_rankLabelName = "game-over-rank-label";
        [FoldoutGroup("References")] [SerializeField] private string m_rankRetryButtonName = "game-over-rank-retry-button";
        [FoldoutGroup("References")] [SerializeField] private string m_restartButtonName = "restart-button";

        private const string k_SubmittingText = "Submitting...";
        private const string k_UnavailableText = "Leaderboard unavailable";
        private const string k_SubmitFailedText = "Couldn't submit score";
        private const string k_RankUpClass = "game-over-rank-label--up";
        private const string k_RankDownClass = "game-over-rank-label--down";
        private const string k_RankSameClass = "game-over-rank-label--same";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_rootElement;
        private Label m_scoreLabel;
        private Label m_rankLabel;
        private Button m_rankRetryButton;
        private Button m_restartButton;
        private GameManager m_gameManager;
        private int m_submitToken;
        private int m_lastScore;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            // m_hud (this object's parent) gets SetActive(false)/(true) across the
            // Bootstrap menu<->play transition (PlayerManager.HandleReturnedToMenu/
            // HandleRunStarted), which re-fires OnEnable/OnDisable on every toggle but
            // only ever fires Start() once. Subscribing here too (once m_gameManager is
            // cached, see Start() below) is what makes the second and later re-enables
            // still receive RunEnded - Start() alone missed a real bug where the menu
            // round-trip silently dropped the subscription and Game Over never showed.
            if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;
        }

        private void Start()
        {
            // Unity only guarantees every object's Awake() runs before any object's
            // Start() - NOT before every other object's OnEnable(). GenericSingleton<T>
            // sets _s_instance in Awake, so subscribing from OnEnable risked running
            // before GameManager's own Awake if HUD happened to be processed first,
            // silently never subscribing (TryGetInstance returning null with no error).
            // Start() is where this is actually safe. TryGetInstance() (not Instance) is
            // used so a missing GameManager fails quietly rather than GenericSingleton
            // auto-instantiating a stand-in that never receives RunEnded from the real
            // fuel/health systems anyway.
            m_gameManager = GameManager.TryGetInstance();
            if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_gameManager != null) m_gameManager.RunEnded -= HandleRunEnded;
            if (m_restartButton != null) m_restartButton.clicked -= HandleRestartClicked;
            if (m_rankRetryButton != null) m_rankRetryButton.clicked -= HandleRetryClicked;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            // Guards against double-registration - OnUIReload can fire more than once per
            // enable (e.g. a live asset reload), unlike the old UIDocument setup where a
            // single OnEnable query only ever ran once per enable/disable cycle.
            if (m_restartButton != null) m_restartButton.clicked -= HandleRestartClicked;
            if (m_rankRetryButton != null) m_rankRetryButton.clicked -= HandleRetryClicked;

            m_rootElement = root.Q<VisualElement>(m_rootElementName);
            m_scoreLabel = root.Q<Label>(m_scoreLabelName);
            m_rankLabel = root.Q<Label>(m_rankLabelName);
            m_rankRetryButton = root.Q<Button>(m_rankRetryButtonName);
            m_restartButton = root.Q<Button>(m_restartButtonName);

            SetVisible(false);
            SetRetryVisible(false);

            if (m_restartButton != null) m_restartButton.clicked += HandleRestartClicked;
            if (m_rankRetryButton != null) m_rankRetryButton.clicked += HandleRetryClicked;
        }

        private void HandleRunEnded()
        {
            int score = 0;
            if (m_scoreSystem != null)
            {
                score = m_scoreSystem.CurrentScore;
                SetScoreLabel(score);
            }
            SetVisible(true);
            m_lastScore = score;
            SubmitAndShowRank(score);
        }

        private void HandleRetryClicked()
        {
            SubmitAndShowRank(m_lastScore);
        }

        private async void SubmitAndShowRank(int score)
        {
            // Each run end (or retry) bumps the token so a result that lands after a newer attempt
            // (or after the scene reloaded under it) is dropped instead of overwriting the label.
            int token = ++m_submitToken;
            SetRetryVisible(false);

            if (score <= 0)
            {
                SetRankLabel(string.Empty, null);
                return;
            }

            LeaderboardService service = LeaderboardService.TryGetInstance();
            if (service == null)
            {
                SetRankLabel(k_UnavailableText, null);
                return;
            }

            SetRankLabel(k_SubmittingText, null);
            SubmitResult result = await service.SubmitScoreAsync(score);
            if (this == null || token != m_submitToken) return;

            if (!result.Success)
            {
                SetRankLabel(k_SubmitFailedText, null);
                SetRetryVisible(true);
                return;
            }

            SetRankLabel($"Rank #{result.Rank} {TrendGlyph(result.Trend)}", result.Trend);
        }

        private static string TrendGlyph(RankTrend trend)
        {
            return trend switch
            {
                RankTrend.Up => "▲",
                RankTrend.Down => "▼",
                _ => "–"
            };
        }

        private void HandleRestartClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetVisible(bool visible)
        {
            if (m_rootElement == null) return;
            m_rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetScoreLabel(int score)
        {
            if (m_scoreLabel == null) return;
            m_scoreLabel.text = $"Score: {score}";
        }

        private void SetRetryVisible(bool visible)
        {
            if (m_rankRetryButton == null) return;
            m_rankRetryButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetRankLabel(string text, RankTrend? trend)
        {
            if (m_rankLabel == null) return;
            m_rankLabel.text = text;
            m_rankLabel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            m_rankLabel.EnableInClassList(k_RankUpClass, trend == RankTrend.Up);
            m_rankLabel.EnableInClassList(k_RankDownClass, trend == RankTrend.Down);
            m_rankLabel.EnableInClassList(k_RankSameClass, trend == RankTrend.Same);
        }
    }
}
