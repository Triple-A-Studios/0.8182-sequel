using Alchemy.Inspector;
using Opoint8182.Lateral;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class LateralWarningUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private LateralSystem m_lateralSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "lateral-warning-root";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_rootElement;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            if (m_lateralSystem != null) m_lateralSystem.IsWarningValue.AddListener(OnIsWarningChanged);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_lateralSystem == null) return;
            m_lateralSystem.IsWarningValue.RemoveListener(OnIsWarningChanged);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            m_rootElement = root.Q<VisualElement>(m_rootElementName);

            if (m_lateralSystem != null) SetVisible(m_lateralSystem.IsWarningValue.Value);
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
