using Alchemy.Inspector;
using Opoint8182.Health;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HealthGaugeUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private HealthSystem m_healthSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "health-gauge-fill";

        private UIDocument m_uiDocument;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_fillElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_fillElementName);

            if (m_healthSystem != null)
            {
                m_healthSystem.HealthValue.AddListener(OnHealthChanged);
                SetFillWidth(m_healthSystem.HealthFraction);
            }
        }

        private void OnDisable()
        {
            if (m_healthSystem != null) m_healthSystem.HealthValue.RemoveListener(OnHealthChanged);
        }

        private void OnHealthChanged(float _)
        {
            SetFillWidth(m_healthSystem.HealthFraction);
        }

        private void SetFillWidth(float fraction)
        {
            if (m_fillElement == null) return;
            m_fillElement.style.width = new StyleLength(Length.Percent(Mathf.Clamp01(fraction) * 100f));
        }
    }
}
