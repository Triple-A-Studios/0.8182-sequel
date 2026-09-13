using System;
using Alchemy.Inspector;
using Opoint8182.Common;
using Opoint8182.Player;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Health
{
    [RequireComponent(typeof(PlaneController))]
    [RequireComponent(typeof(Rigidbody))]
    public class HealthSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_maxHealth = 100f;

        [Title("Damage Sources")]
        [FoldoutGroup("Damage Sources")] [SerializeField] private MonoBehaviour[] m_damageDealerBehaviours;

        [Title("Restore Sources")]
        [FoldoutGroup("Restore Sources")] [SerializeField] private MonoBehaviour[] m_restorerBehaviours;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentHealth => Health.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isRunEnded;

        private ObservableFloat m_health;
        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;
        private IDamageDealer[] m_damageDealers;
        private IRestorer[] m_restorers;

        public event Action Died;

        // Lazily constructed so HealthValue/CurrentHealth are safe to read even if another
        // object's OnEnable/Start runs before this component's own Awake - Unity doesn't
        // guarantee Awake order across different GameObjects. maxHealth's Inspector value is
        // already deserialized by the time any script runs, so this is always correct.
        private ObservableFloat Health => m_health ??= new ObservableFloat(m_maxHealth);

        public ObservableFloat HealthValue => Health;
        public float HealthFraction => m_maxHealth > 0f ? Mathf.Clamp01(Health.Value / m_maxHealth) : 0f;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
            m_rigidbody = GetComponent<Rigidbody>();
            // Unity can't serialize a bare interface reference in the Inspector, so the field is
            // typed MonoBehaviour[] and cast here - any damage-dealing component (Building,
            // ObstacleBuilding, future hazards) can be dropped in as long as it implements IDamageDealer.
            m_damageDealers = Array.ConvertAll(m_damageDealerBehaviours, b => b as IDamageDealer);
            m_restorers = Array.ConvertAll(m_restorerBehaviours, b => b as IRestorer);
        }

        private void OnEnable()
        {
            foreach (var dealer in m_damageDealers)
            {
                if (dealer != null) dealer.DamageDealt += HandleDamageDealt;
            }

            foreach (var restorer in m_restorers)
            {
                if (restorer != null) restorer.Restored += HandleRestored;
            }
        }

        private void OnDisable()
        {
            foreach (var dealer in m_damageDealers)
            {
                if (dealer != null) dealer.DamageDealt -= HandleDamageDealt;
            }

            foreach (var restorer in m_restorers)
            {
                if (restorer != null) restorer.Restored -= HandleRestored;
            }
        }

        private void HandleDamageDealt(float damage)
        {
            if (m_isRunEnded) return;

            var damaged = Mathf.Clamp(Health.Value - damage, 0f, m_maxHealth);
            Health.Set(damaged);

            Debug.Log($"[HealthSystem] Took {damage:0.0} damage - health now {damaged:0.0}/{m_maxHealth:0.0}");

            if (Health.Value <= 0f) EndRun();
        }

        private void HandleRestored(float amount)
        {
            if (m_isRunEnded) return;

            var restored = Mathf.Clamp(Health.Value + amount, 0f, m_maxHealth);
            Health.Set(restored);

            Debug.Log($"[HealthSystem] Restored {amount:0.0} - health now {restored:0.0}/{m_maxHealth:0.0}");
        }

        private void EndRun()
        {
            m_isRunEnded = true;
            m_planeController.enabled = false;
            m_rigidbody.linearVelocity = Vector3.zero;
            Died?.Invoke();
        }
    }
}
