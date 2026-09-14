using System;
using Alchemy.Inspector;
using Opoint8182.Player;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Fuel
{
    [RequireComponent(typeof(PlaneController))]
    public class FuelSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_maxFuel = 100f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_drainPerSecond = 5f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_boostDrainMultiplier = 2f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentFuel => Fuel.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isDepleted;

        private ObservableFloat m_fuel;
        private PlaneController m_planeController;

        public event Action Depleted;

        // Lazily constructed so FuelValue/CurrentFuel are safe to read even if another
        // object's OnEnable/Start runs before this component's own Awake - Unity doesn't
        // guarantee Awake order across different GameObjects. maxFuel's Inspector value is
        // already deserialized by the time any script runs, so this is always correct.
        private ObservableFloat Fuel => m_fuel ??= new ObservableFloat(m_maxFuel);

        public ObservableFloat FuelValue => Fuel;
        public float FuelFraction => m_maxFuel > 0f ? Mathf.Clamp01(Fuel.Value / m_maxFuel) : 0f;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
        }

        private void Update()
        {
            if (m_isDepleted) return;

            var drainRate = m_drainPerSecond * (m_planeController.IsBoosting ? m_boostDrainMultiplier : 1f);
            Drain(drainRate * Time.deltaTime);
        }

        public void Refuel(float quality)
        {
            if (m_isDepleted) return;

            var refueled = Mathf.Clamp(Fuel.Value + quality * m_maxFuel, 0f, m_maxFuel);
            Fuel.Set(refueled);
        }

        public void Drain(float amount)
        {
            if (m_isDepleted) return;

            var drained = Mathf.Clamp(Fuel.Value - amount, 0f, m_maxFuel);
            Fuel.Set(drained);

            if (Fuel.Value <= 0f) MarkDepleted();
        }

        private void MarkDepleted()
        {
            m_isDepleted = true;
            Depleted?.Invoke();
        }
    }
}
