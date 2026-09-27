using Alchemy.Inspector;
using Opoint8182.Health;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class HealthGaugeUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private HealthSystem m_healthSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "health-gauge-fill";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            if (m_healthSystem != null) m_healthSystem.HealthValue.AddListener(OnHealthChanged);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_healthSystem != null) m_healthSystem.HealthValue.RemoveListener(OnHealthChanged);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            m_fillElement = root.Q<VisualElement>(m_fillElementName);

            if (m_healthSystem != null) SetFillWidth(m_healthSystem.HealthFraction);
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
