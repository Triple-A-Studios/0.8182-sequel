using Alchemy.Inspector;
using Opoint8182.Fuel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class FuelGaugeUI : MonoBehaviour
    {
        [FormerlySerializedAs("fuelSystem")]
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private FuelSystem m_fuelSystem;
        [FoldoutGroup("References")] [SerializeField] private string m_fillElementName = "fuel-gauge-fill";

        private PanelRenderer m_panelRenderer;
        private VisualElement m_fillElement;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

            if (m_fuelSystem != null) m_fuelSystem.FuelValue.AddListener(OnFuelChanged);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            if (m_fuelSystem != null) m_fuelSystem.FuelValue.RemoveListener(OnFuelChanged);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            m_fillElement = root.Q<VisualElement>(m_fillElementName);

            if (m_fuelSystem != null) SetFillWidth(m_fuelSystem.FuelFraction);
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
