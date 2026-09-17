using Alchemy.Inspector;
using Opoint8182.Altitude;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class AltitudeWarningUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private AltitudeSystem m_altitudeSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_rootElementName = "altitude-warning-root";
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "altitude-warning-fill";

        private UIDocument m_uiDocument;
        private VisualElement m_rootElement;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_rootElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_rootElementName);
            m_fillElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_fillElementName);

            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.IsWarningValue.AddListener(OnIsWarningChanged);
                m_altitudeSystem.WarningFractionValue.AddListener(OnFractionChanged);
                SetVisible(m_altitudeSystem.IsWarningValue.Value);
                SetFillWidth(m_altitudeSystem.WarningFractionValue.Value);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void OnDisable()
        {
            if (m_altitudeSystem == null) return;
            m_altitudeSystem.IsWarningValue.RemoveListener(OnIsWarningChanged);
            m_altitudeSystem.WarningFractionValue.RemoveListener(OnFractionChanged);
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
