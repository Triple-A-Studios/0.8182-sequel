using Alchemy.Inspector;
using Opoint8182.Common;
using Opoint8182.Fuel;
using Opoint8182.Health;
using UnityEngine;

namespace Opoint8182.Player
{
    [RequireComponent(typeof(PlaneController))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FuelSystem))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerManager : MonoBehaviour
    {
        [Title("Debug")]
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isRunEnded;

        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;
        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_fuelSystem = GetComponent<FuelSystem>();
            m_healthSystem = GetComponent<HealthSystem>();
        }

        private void OnEnable()
        {
            CombatEvents.Crashed += HandleCrashed;
            CombatEvents.DamageDealt += HandleDamageDealt;
            CombatEvents.Restored += HandleRestored;
            m_fuelSystem.Depleted += HandleDepleted;
            m_healthSystem.Depleted += HandleDepleted;
        }

        private void OnDisable()
        {
            CombatEvents.Crashed -= HandleCrashed;
            CombatEvents.DamageDealt -= HandleDamageDealt;
            CombatEvents.Restored -= HandleRestored;
            m_fuelSystem.Depleted -= HandleDepleted;
            m_healthSystem.Depleted -= HandleDepleted;
        }

        private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
        {
            if (m_isRunEnded) return;

            m_fuelSystem.Refuel(quality);
        }

        private void HandleDamageDealt(IDamageDealer source, float damage)
        {
            if (m_isRunEnded) return;

            m_fuelSystem.Drain(damage * source.FuelDamageMultiplier);
            m_healthSystem.TakeDamage(damage * source.HealthDamageMultiplier);
        }

        private void HandleRestored(IRestorer source, float amount)
        {
            if (m_isRunEnded) return;

            m_healthSystem.Heal(amount);
        }

        private void HandleDepleted()
        {
            if (m_isRunEnded) return;

            m_isRunEnded = true;
            m_planeController.enabled = false;
            m_rigidbody.linearVelocity = Vector3.zero;
        }
    }
}
