using System;
using Alchemy.Inspector;
using Opoint8182.Fuel;
using Opoint8182.Health;
using TripleA.Utils.Singletons;
using UnityEngine;

namespace Opoint8182.Game
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public bool IsRunActive { get; private set; } = true;

        public event Action RunEnded;

        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;

        private void OnEnable()
        {
            m_fuelSystem = FindAnyObjectByType<FuelSystem>();
            m_healthSystem = FindAnyObjectByType<HealthSystem>();

            if (m_fuelSystem != null) m_fuelSystem.RunEnded += HandleRunEnded;
            if (m_healthSystem != null) m_healthSystem.Died += HandleRunEnded;
        }

        private void OnDisable()
        {
            if (m_fuelSystem != null) m_fuelSystem.RunEnded -= HandleRunEnded;
            if (m_healthSystem != null) m_healthSystem.Died -= HandleRunEnded;
        }

        private void HandleRunEnded()
        {
            if (!IsRunActive) return;

            IsRunActive = false;
            RunEnded?.Invoke();
        }
    }
}
