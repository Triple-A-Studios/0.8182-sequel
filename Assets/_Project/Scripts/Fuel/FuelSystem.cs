using System;
using Alchemy.Inspector;
using Bldng = Opoint8182.Building.Building;
using Opoint8182.Common;
using Opoint8182.Player;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Fuel
{
    [RequireComponent(typeof(PlaneController))]
    [RequireComponent(typeof(Rigidbody))]
    public class FuelSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_maxFuel = 100f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_drainPerSecond = 5f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_boostDrainMultiplier = 2f;

        [Title("Crash Sources")]
        [FoldoutGroup("Crash Sources")] [SerializeField] private Bldng[] m_buildings;

        [Title("Damage Sources")]
        [FoldoutGroup("Damage Sources")] [SerializeField] private MonoBehaviour[] m_damageSourceBehaviours;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentFuel => Fuel.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isRunEnded;

        private ObservableFloat m_fuel;
        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;
        private IDamageDealer[] m_damageSources;

        public event Action RunEnded;

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
            m_rigidbody = GetComponent<Rigidbody>();
            // Same MonoBehaviour-cast idiom as HealthSystem.m_damageDealerBehaviours - Unity can't
            // serialize a bare interface reference, so obstacle/hazard sources get dropped in here
            // and cast at Awake.
            m_damageSources = Array.ConvertAll(m_damageSourceBehaviours, b => b as IDamageDealer);
        }

        private void OnEnable()
        {
            foreach (var building in m_buildings)
            {
                if (building != null) building.Crashed += HandleCrashed;
            }

            foreach (var source in m_damageSources)
            {
                if (source != null) source.DamageDealt += HandleDamagePenalty;
            }
        }

        private void OnDisable()
        {
            foreach (var building in m_buildings)
            {
                if (building != null) building.Crashed -= HandleCrashed;
            }

            foreach (var source in m_damageSources)
            {
                if (source != null) source.DamageDealt -= HandleDamagePenalty;
            }
        }

        private void Update()
        {
            if (m_isRunEnded) return;

            var drainRate = m_drainPerSecond * (m_planeController.IsBoosting ? m_boostDrainMultiplier : 1f);
            var drained = Mathf.Clamp(Fuel.Value - drainRate * Time.deltaTime, 0f, m_maxFuel);
            Fuel.Set(drained);

            if (Fuel.Value <= 0f) EndRun();
        }

        // scoreValue is ScoreSystem's concern, not FuelSystem's - FuelSystem only needs quality,
        // but it has to match Building.Crashed's signature. Exactly the kind of coupling the
        // planned "Core systems refactor" milestone (PlayerManager mediating sources -> Fuel/
        // Health/Score) exists to remove.
        private void HandleCrashed(float quality, int scoreValue)
        {
            if (m_isRunEnded) return;

            var refueled = Mathf.Clamp(Fuel.Value + quality * m_maxFuel, 0f, m_maxFuel);
            Fuel.Set(refueled);
        }

        private void HandleDamagePenalty(float damage)
        {
            if (m_isRunEnded) return;

            var drained = Mathf.Clamp(Fuel.Value - damage, 0f, m_maxFuel);
            Fuel.Set(drained);

            if (Fuel.Value <= 0f) EndRun();
        }

        private void EndRun()
        {
            m_isRunEnded = true;
            m_planeController.enabled = false;
            m_rigidbody.linearVelocity = Vector3.zero;
            RunEnded?.Invoke();
        }
    }
}
