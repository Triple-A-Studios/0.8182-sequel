using Alchemy.Inspector;
using Opoint8182.Game;
using Opoint8182.Score;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "game-over-root";
        [FoldoutGroup("References")] [SerializeField] private string m_scoreLabelName = "game-over-score-label";
        [FoldoutGroup("References")] [SerializeField] private string m_restartButtonName = "restart-button";

        private UIDocument m_uiDocument;
        private VisualElement m_rootElement;
        private Label m_scoreLabel;
        private Button m_restartButton;
        private GameManager m_gameManager;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_rootElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_rootElementName);
            m_scoreLabel = m_uiDocument.rootVisualElement.Q<Label>(m_scoreLabelName);
            m_restartButton = m_uiDocument.rootVisualElement.Q<Button>(m_restartButtonName);

            SetVisible(false);

            if (m_restartButton != null) m_restartButton.clicked += HandleRestartClicked;
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
            if (m_gameManager != null) m_gameManager.RunEnded -= HandleRunEnded;
            if (m_restartButton != null) m_restartButton.clicked -= HandleRestartClicked;
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
