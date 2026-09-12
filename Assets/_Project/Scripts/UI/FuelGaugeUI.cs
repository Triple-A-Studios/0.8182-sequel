using Alchemy.Inspector;
using Opoint8182.Fuel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FuelGaugeUI : MonoBehaviour
    {
        [FormerlySerializedAs("fuelSystem")]
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private FuelSystem m_fuelSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "fuel-gauge-fill";

        private UIDocument m_uiDocument;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            m_fillElement = m_uiDocument.rootVisualElement.Q<VisualElement>(m_fillElementName);

            if (m_fuelSystem != null)
            {
                m_fuelSystem.FuelValue.AddListener(OnFuelChanged);
                SetFillWidth(m_fuelSystem.FuelFraction);
            }
        }

        private void OnDisable()
        {
            if (m_fuelSystem != null) m_fuelSystem.FuelValue.RemoveListener(OnFuelChanged);
        }

        private void OnFuelChanged(float _)
        {
            SetFillWidth(m_fuelSystem.FuelFraction);
        }

        private void SetFillWidth(float fraction)
        {
            if (m_fillElement == null) return;
            m_fillElement.style.width = new StyleLength(Length.Percent(Mathf.Clamp01(fraction) * 100f));
        }
    }
}
