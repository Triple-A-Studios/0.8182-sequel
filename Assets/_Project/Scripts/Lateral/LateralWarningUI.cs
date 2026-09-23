using Alchemy.Inspector;
using Opoint8182.Lateral;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LateralWarningUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private LateralSystem m_lateralSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "lateral-warning-root";

        private UIDocument m_uiDocument;
        private VisualElement m_rootElement;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_rootElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_rootElementName);

            if (m_lateralSystem != null)
            {
                m_lateralSystem.IsWarningValue.AddListener(OnIsWarningChanged);
                SetVisible(m_lateralSystem.IsWarningValue.Value);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void OnDisable()
        {
            if (m_lateralSystem == null) return;
            m_lateralSystem.IsWarningValue.RemoveListener(OnIsWarningChanged);
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
