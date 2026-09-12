using System;
using Alchemy.Inspector;
using Bldng = Opoint8182.Building.Building;
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
        [FoldoutGroup("Tunables")] [SerializeField] private float maxFuel = 100f;
        [FoldoutGroup("Tunables")] [SerializeField] private float drainPerSecond = 5f;

        [Title("Crash Source")]
        [FoldoutGroup("Crash Source")] [SerializeField] private Bldng building;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentFuel => Fuel.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool _isRunEnded;

        private ObservableFloat _fuel;
        private PlaneController _planeController;
        private Rigidbody _rigidbody;

        public event Action RunEnded;

        // Lazily constructed so FuelValue/CurrentFuel are safe to read even if another
        // object's OnEnable/Start runs before this component's own Awake - Unity doesn't
        // guarantee Awake order across different GameObjects. maxFuel's Inspector value is
        // already deserialized by the time any script runs, so this is always correct.
        private ObservableFloat Fuel => _fuel ??= new ObservableFloat(maxFuel);

        public ObservableFloat FuelValue => Fuel;
        public float FuelFraction => maxFuel > 0f ? Mathf.Clamp01(Fuel.Value / maxFuel) : 0f;

        private void Awake()
        {
            _planeController = GetComponent<PlaneController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            if (building != null) building.Crashed += HandleCrashed;
        }

        private void OnDisable()
        {
            if (building != null) building.Crashed -= HandleCrashed;
        }

        private void Update()
        {
            if (_isRunEnded) return;

            var drained = Mathf.Clamp(Fuel.Value - drainPerSecond * Time.deltaTime, 0f, maxFuel);
            Fuel.Set(drained);

            if (Fuel.Value <= 0f) EndRun();
        }

        private void HandleCrashed(float quality)
        {
            if (_isRunEnded) return;

            var refueled = Mathf.Clamp(Fuel.Value + quality * maxFuel, 0f, maxFuel);
            Fuel.Set(refueled);
        }

        private void EndRun()
        {
            _isRunEnded = true;
            _planeController.enabled = false;
            _rigidbody.linearVelocity = Vector3.zero;
            RunEnded?.Invoke();
        }
    }
}
