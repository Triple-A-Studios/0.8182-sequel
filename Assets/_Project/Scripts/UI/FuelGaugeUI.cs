using Alchemy.Inspector;
using Opoint8182.Fuel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FuelGaugeUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private FuelSystem fuelSystem;
        [FoldoutGroup("References")] [SerializeField] private string fillElementName = "fuel-gauge-fill";

        private UIDocument _uiDocument;
        private VisualElement _fillElement;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _fillElement = _uiDocument.rootVisualElement.Q<VisualElement>(fillElementName);

            if (fuelSystem != null)
            {
                fuelSystem.FuelValue.AddListener(OnFuelChanged);
                SetFillWidth(fuelSystem.FuelFraction);
            }
        }

        private void OnDisable()
        {
            if (fuelSystem != null) fuelSystem.FuelValue.RemoveListener(OnFuelChanged);
        }

        private void OnFuelChanged(float _)
        {
            SetFillWidth(fuelSystem.FuelFraction);
        }

        private void SetFillWidth(float fraction)
        {
            if (_fillElement == null) return;
            _fillElement.style.width = new StyleLength(Length.Percent(Mathf.Clamp01(fraction) * 100f));
        }
    }
}
