using System;
using Alchemy.Inspector;
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

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentFuel => Fuel.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_isRunEnded;

        private ObservableFloat m_fuel;
        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;

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
        }

        private void OnEnable()
        {
            CombatEvents.Crashed += HandleCrashed;
            CombatEvents.DamageDealt += HandleDamagePenalty;
        }

        private void OnDisable()
        {
            CombatEvents.Crashed -= HandleCrashed;
            CombatEvents.DamageDealt -= HandleDamagePenalty;
        }

        private void Update()
        {
            if (m_isRunEnded) return;

            var drainRate = m_drainPerSecond * (m_planeController.IsBoosting ? m_boostDrainMultiplier : 1f);
            var drained = Mathf.Clamp(Fuel.Value - drainRate * Time.deltaTime, 0f, m_maxFuel);
            Fuel.Set(drained);

            if (Fuel.Value <= 0f) EndRun();
        }

        // source is unused here (ScoreSystem is the one that reads source.ScoreValue) - kept in
        // the signature only to match CombatEvents.Crashed.
        private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
        {
            if (m_isRunEnded) return;

            var refueled = Mathf.Clamp(Fuel.Value + quality * m_maxFuel, 0f, m_maxFuel);
            Fuel.Set(refueled);
        }

        private void HandleDamagePenalty(IDamageDealer source, float damage)
        {
            if (m_isRunEnded) return;

            var drained = Mathf.Clamp(Fuel.Value - damage * source.FuelDamageMultiplier, 0f, m_maxFuel);
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
