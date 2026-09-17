using System;
using Alchemy.Inspector;
using Opoint8182.Player;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Altitude
{
    [RequireComponent(typeof(PlaneController))]
    public class AltitudeSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_ceilingY = 30f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_groundY = -1f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_ceilingWarningSeconds = 4f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_ceilingExceeded;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_groundHit;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private float m_ceilingWarningRemaining;

        private ObservableBool m_isWarning;
        private ObservableFloat m_warningFraction;
        private PlaneController m_planeController;

        public event Action CeilingExceeded;
        public event Action GroundHit;

        // Lazily constructed for the same reason as FuelSystem.Fuel/HealthSystem.Health -
        // Unity doesn't guarantee Awake order across different GameObjects.
        private ObservableBool IsWarningObservable => m_isWarning ??= new ObservableBool(false);
        private ObservableFloat WarningFractionObservable => m_warningFraction ??= new ObservableFloat(1f);

        public ObservableBool IsWarningValue => IsWarningObservable;
        public ObservableFloat WarningFractionValue => WarningFractionObservable;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
            m_ceilingWarningRemaining = m_ceilingWarningSeconds;
        }

        private void FixedUpdate()
        {
            if (m_ceilingExceeded || m_groundHit) return;

            var altitude = transform.position.y;

            if (altitude <= m_groundY)
            {
                MarkGroundHit();
                return;
            }

            if (altitude >= m_ceilingY)
            {
                IsWarningObservable.Set(true);

                // Descending (held S / negative vertical input) pauses the countdown in place
                // rather than resetting it - only dropping back below the ceiling resets it.
                var isDescending = m_planeController.Velocity.y < 0f;
                if (!isDescending)
                {
                    m_ceilingWarningRemaining = Mathf.Max(0f, m_ceilingWarningRemaining - Time.fixedDeltaTime);
                    WarningFractionObservable.Set(m_ceilingWarningSeconds > 0f
                        ? Mathf.Clamp01(m_ceilingWarningRemaining / m_ceilingWarningSeconds)
                        : 0f);

                    if (m_ceilingWarningRemaining <= 0f) MarkCeilingExceeded();
                }
            }
            else if (IsWarningObservable.Value)
            {
                m_ceilingWarningRemaining = m_ceilingWarningSeconds;
                IsWarningObservable.Set(false);
                WarningFractionObservable.Set(1f);
            }
        }

        private void MarkCeilingExceeded()
        {
            m_ceilingExceeded = true;
            CeilingExceeded?.Invoke();
        }

        private void MarkGroundHit()
        {
            m_groundHit = true;
            GroundHit?.Invoke();
        }
    }
}
