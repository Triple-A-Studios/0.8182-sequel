using Alchemy.Inspector;
using Opoint8182.Game;
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
        [FoldoutGroup("References")] [SerializeField] private string m_restartButtonName = "restart-button";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_rootElement;
        private Label m_scoreLabel;
        private Button m_restartButton;
        private GameManager m_gameManager;

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
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            // Guards against double-registration - OnUIReload can fire more than once per
            // enable (e.g. a live asset reload), unlike the old UIDocument setup where a
            // single OnEnable query only ever ran once per enable/disable cycle.
            if (m_restartButton != null) m_restartButton.clicked -= HandleRestartClicked;

            m_rootElement = root.Q<VisualElement>(m_rootElementName);
            m_scoreLabel = root.Q<Label>(m_scoreLabelName);
            m_restartButton = root.Q<Button>(m_restartButtonName);

            SetVisible(false);

            if (m_restartButton != null) m_restartButton.clicked += HandleRestartClicked;
        }

        private void HandleRunEnded()
        {
            if (m_scoreSystem != null) SetScoreLabel(m_scoreSystem.CurrentScore);
            SetVisible(true);
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
    }
}
