using Alchemy.Inspector;
using Opoint8182.Altitude;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class AltitudeWarningUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private AltitudeSystem m_altitudeSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "altitude-warning-root";
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "altitude-warning-fill";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_rootElement;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.IsWarningValue.AddListener(OnIsWarningChanged);
                m_altitudeSystem.WarningFractionValue.AddListener(OnFractionChanged);
            }
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_altitudeSystem == null) return;
            m_altitudeSystem.IsWarningValue.RemoveListener(OnIsWarningChanged);
            m_altitudeSystem.WarningFractionValue.RemoveListener(OnFractionChanged);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            m_rootElement = root.Q<VisualElement>(m_rootElementName);
            m_fillElement = root.Q<VisualElement>(m_fillElementName);

            if (m_altitudeSystem != null)
            {
                SetVisible(m_altitudeSystem.IsWarningValue.Value);
                SetFillWidth(m_altitudeSystem.WarningFractionValue.Value);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void OnIsWarningChanged(bool isWarning)
        {
            SetVisible(isWarning);
        }

        private void OnFractionChanged(float fraction)
        {
            SetFillWidth(fraction);
        }

        private void SetVisible(bool visible)
        {
            if (m_rootElement == null) return;
            m_rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetFillWidth(float fraction)
        {
            if (m_fillElement == null) return;
            m_fillElement.style.width = new StyleLength(Length.Percent(Mathf.Clamp01(fraction) * 100f));
        }
    }
}
