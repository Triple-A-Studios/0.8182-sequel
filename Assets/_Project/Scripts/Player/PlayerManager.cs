using Alchemy.Inspector;
using Opoint8182.Altitude;
using Opoint8182.Common;
using Opoint8182.Fuel;
using Opoint8182.Health;
using Unity.Cinemachine;
using UnityEngine;

namespace Opoint8182.Player
{
    [RequireComponent(typeof(PlaneController))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FuelSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(AltitudeSystem))]
    public class PlayerManager : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private CinemachineFollow m_followCamera;

        private bool m_isRunEnded;

        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;
        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;
        private AltitudeSystem m_altitudeSystem;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_fuelSystem = GetComponent<FuelSystem>();
            m_healthSystem = GetComponent<HealthSystem>();
            m_altitudeSystem = GetComponent<AltitudeSystem>();
        }

        private void OnEnable()
        {
            CombatEvents.Crashed += HandleCrashed;
            CombatEvents.DamageDealt += HandleDamageDealt;
            CombatEvents.Restored += HandleRestored;
            m_fuelSystem.Depleted += HandleDepleted;
            m_healthSystem.Depleted += HandleDepleted;
            m_altitudeSystem.GroundHit += HandleDepleted;
            m_altitudeSystem.CeilingExceeded += HandleCeilingExceeded;
        }

        private void OnDisable()
        {
            CombatEvents.Crashed -= HandleCrashed;
            CombatEvents.DamageDealt -= HandleDamageDealt;
            CombatEvents.Restored -= HandleRestored;
            m_fuelSystem.Depleted -= HandleDepleted;
            m_healthSystem.Depleted -= HandleDepleted;
            m_altitudeSystem.GroundHit -= HandleDepleted;
            m_altitudeSystem.CeilingExceeded -= HandleCeilingExceeded;
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

        private void HandleCeilingExceeded()
        {
            if (m_isRunEnded) return;

            m_isRunEnded = true;
            // Unlike HandleDepleted, don't touch linearVelocity - the plane keeps flying off
            // in whatever direction it was last actually moving (no gravity/drag on this
            // Rigidbody, so it drifts on forever once PlaneController stops overwriting it).
            m_planeController.enabled = false;
            if (m_followCamera != null) m_followCamera.enabled = false;
        }
    }
}
