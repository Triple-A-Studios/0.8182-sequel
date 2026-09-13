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
        [FoldoutGroup("References")] [SerializeField] private string m_comboLabelName = "combo-label";
        [FoldoutGroup("References")] [SerializeField] private string m_comboTimerFillElementName = "combo-timer-fill";

        private UIDocument m_uiDocument;
        private Label m_scoreLabel;
        private Label m_comboLabel;
        private VisualElement m_comboTimerFillElement;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_scoreLabel = m_uiDocument.rootVisualElement.Q<Label>(m_scoreLabelName);
            m_comboLabel = m_uiDocument.rootVisualElement.Q<Label>(m_comboLabelName);
            m_comboTimerFillElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_comboTimerFillElementName);

            if (m_scoreSystem != null)
            {
                m_scoreSystem.ScoreValue.AddListener(OnScoreChanged);
                m_scoreSystem.MultiplierValue.AddListener(OnMultiplierChanged);
                m_scoreSystem.ComboTimerValue.AddListener(OnComboTimerChanged);
                SetScoreLabel(m_scoreSystem.CurrentScore);
                SetComboLabel(m_scoreSystem.CurrentMultiplier);
                SetComboTimerFill(m_scoreSystem.ComboTimerFraction);
            }
        }

        private void OnDisable()
        {
            if (m_scoreSystem == null) return;
            m_scoreSystem.ScoreValue.RemoveListener(OnScoreChanged);
            m_scoreSystem.MultiplierValue.RemoveListener(OnMultiplierChanged);
            m_scoreSystem.ComboTimerValue.RemoveListener(OnComboTimerChanged);
        }

        private void OnScoreChanged(int score)
        {
            SetScoreLabel(score);
        }

        private void OnMultiplierChanged(int multiplier)
        {
            SetComboLabel(multiplier);
        }

        private void OnComboTimerChanged(float _)
        {
            SetComboTimerFill(m_scoreSystem.ComboTimerFraction);
        }

        private void SetScoreLabel(int score)
        {
            if (m_scoreLabel == null) return;
            m_scoreLabel.text = $"Score: {score}";
        }

        private void SetComboLabel(int multiplier)
        {
            if (m_comboLabel == null) return;
            m_comboLabel.text = $"x{multiplier}";
        }

        private void SetComboTimerFill(float fraction)
        {
            if (m_comboTimerFillElement == null) return;
            m_comboTimerFillElement.style.width = new StyleLength(Length.Percent(Mathf.Clamp01(fraction) * 100f));
        }
    }
}
