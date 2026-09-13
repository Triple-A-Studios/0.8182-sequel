using Alchemy.Inspector;
using Opoint8182.Score;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ScoreUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_scoreLabelName = "score-label";

        private UIDocument m_uiDocument;
        private Label m_scoreLabel;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_scoreLabel = m_uiDocument.rootVisualElement.Q<Label>(m_scoreLabelName);

            if (m_scoreSystem != null)
            {
                m_scoreSystem.ScoreValue.AddListener(OnScoreChanged);
                SetLabel(m_scoreSystem.CurrentScore);
            }
        }

        private void OnDisable()
        {
            if (m_scoreSystem != null) m_scoreSystem.ScoreValue.RemoveListener(OnScoreChanged);
        }

        private void OnScoreChanged(int score)
        {
            SetLabel(score);
        }

        private void SetLabel(int score)
        {
            if (m_scoreLabel == null) return;
            m_scoreLabel.text = $"Score: {score}";
        }
    }
}
