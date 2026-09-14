using System;
using Alchemy.Inspector;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Health
{
    public class HealthSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_maxHealth = 100f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentHealth => Health.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isDepleted;

        private ObservableFloat m_health;

        public event Action Depleted;

        // Lazily constructed so HealthValue/CurrentHealth are safe to read even if another
        // object's OnEnable/Start runs before this component's own Awake - Unity doesn't
        // guarantee Awake order across different GameObjects. maxHealth's Inspector value is
        // already deserialized by the time any script runs, so this is always correct.
        private ObservableFloat Health => m_health ??= new ObservableFloat(m_maxHealth);

        public ObservableFloat HealthValue => Health;
        public float HealthFraction => m_maxHealth > 0f ? Mathf.Clamp01(Health.Value / m_maxHealth) : 0f;

        public void TakeDamage(float amount)
        {
            if (m_isDepleted) return;

            var damaged = Mathf.Clamp(Health.Value - amount, 0f, m_maxHealth);
            Health.Set(damaged);

            Debug.Log($"[HealthSystem] Took {amount:0.0} damage - health now {damaged:0.0}/{m_maxHealth:0.0}");

            if (Health.Value <= 0f) MarkDepleted();
        }

        public void Heal(float amount)
        {
            if (m_isDepleted) return;

            var restored = Mathf.Clamp(Health.Value + amount, 0f, m_maxHealth);
            Health.Set(restored);

            Debug.Log($"[HealthSystem] Restored {amount:0.0} - health now {restored:0.0}/{m_maxHealth:0.0}");
        }

        private void MarkDepleted()
        {
            m_isDepleted = true;
            Depleted?.Invoke();
        }
    }
}
