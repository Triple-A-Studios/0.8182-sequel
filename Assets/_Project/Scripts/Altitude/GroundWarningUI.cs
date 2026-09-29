using Alchemy.Inspector;
using Opoint8182.Altitude;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GroundWarningUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private AltitudeSystem m_altitudeSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "ground-warning-root";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_rootElement;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            if (m_altitudeSystem != null) m_altitudeSystem.IsGroundWarningValue.AddListener(OnIsWarningChanged);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_altitudeSystem == null) return;
            m_altitudeSystem.IsGroundWarningValue.RemoveListener(OnIsWarningChanged);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            m_rootElement = root.Q<VisualElement>(m_rootElementName);

            if (m_altitudeSystem != null) SetVisible(m_altitudeSystem.IsGroundWarningValue.Value);
            else SetVisible(false);
        }

        private void OnIsWarningChanged(bool isWarning)
        {
            SetVisible(isWarning);
        }

        private void SetVisible(bool visible)
        {
            if (m_rootElement == null) return;
            m_rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
