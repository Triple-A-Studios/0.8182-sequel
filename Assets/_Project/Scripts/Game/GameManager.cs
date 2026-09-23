using System;
using Opoint8182.Altitude;
using Opoint8182.Fuel;
using Opoint8182.Health;
using Opoint8182.Lateral;
using TripleA.Utils.Singletons;
using UnityEngine;

namespace Opoint8182.Game
{
    public class GameManager : GenericSingleton<GameManager>
    {
        public bool IsRunActive { get; private set; } = true;

        public event Action RunEnded;

        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;
        private AltitudeSystem m_altitudeSystem;
        private LateralSystem m_lateralSystem;

        private void OnEnable()
        {
            m_fuelSystem = FindAnyObjectByType<FuelSystem>();
            m_healthSystem = FindAnyObjectByType<HealthSystem>();
            m_altitudeSystem = FindAnyObjectByType<AltitudeSystem>();
            m_lateralSystem = FindAnyObjectByType<LateralSystem>();

            if (m_fuelSystem != null) m_fuelSystem.Depleted += HandleRunEnded;
            if (m_healthSystem != null) m_healthSystem.Depleted += HandleRunEnded;
            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.CeilingExceeded += HandleRunEnded;
                m_altitudeSystem.GroundHit += HandleRunEnded;
            }
            if (m_lateralSystem != null) m_lateralSystem.HardBoundExceeded += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (m_fuelSystem != null) m_fuelSystem.Depleted -= HandleRunEnded;
            if (m_healthSystem != null) m_healthSystem.Depleted -= HandleRunEnded;
            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.CeilingExceeded -= HandleRunEnded;
                m_altitudeSystem.GroundHit -= HandleRunEnded;
            }
            if (m_lateralSystem != null) m_lateralSystem.HardBoundExceeded -= HandleRunEnded;
        }

        private void HandleRunEnded()
        {
            if (!IsRunActive) return;

            IsRunActive = false;
            RunEnded?.Invoke();
        }
    }
}
