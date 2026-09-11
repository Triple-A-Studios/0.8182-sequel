using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlaneController : MonoBehaviour
    {
        [Title("Movement")]
        [FoldoutGroup("Movement")] [SerializeField] private float forwardSpeed = 20f;
        [FoldoutGroup("Movement")] [SerializeField] private float pitchRateDegPerSec = 60f;
        [FoldoutGroup("Movement")] [SerializeField] private float sideSpeed = 15f;
        [FoldoutGroup("Movement")] [SerializeField] private float maxPitchAngle = 60f;

        [Title("Visual Bank (cosmetic only)")]
        [FoldoutGroup("Visual Bank")] [SerializeField] private Transform visualRoot;
        [FoldoutGroup("Visual Bank")] [SerializeField] private float maxBankAngle = 40f;
        [FoldoutGroup("Visual Bank")] [SerializeField] private float bankSpeedDegPerSec = 180f;

        [Title("Input")]
        [FoldoutGroup("Input")] [SerializeField] private InputActionReference moveAction;
        [FoldoutGroup("Input")] [SerializeField] private float inputDeadZone = 0.1f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private Vector2 _steerInput;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private float _currentBankAngle;

        private Rigidbody _rigidbody;
        private float _pitchDeg;

        public Vector3 Velocity => _rigidbody.linearVelocity;
        public float Speed => _rigidbody.linearVelocity.magnitude;
        public Vector3 Forward => transform.forward;
        public float SpeedMultiplier { get; set; } = 1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
        }

        private void FixedUpdate()
        {
            _steerInput = moveAction.action.ReadValue<Vector2>();
            if (_steerInput.magnitude < inputDeadZone)
            {
                _steerInput = Vector2.zero;
            }

            _pitchDeg += -_steerInput.y * pitchRateDegPerSec * Time.fixedDeltaTime;
            _pitchDeg = Mathf.Clamp(_pitchDeg, -maxPitchAngle, maxPitchAngle);

            var rotation = Quaternion.Euler(_pitchDeg, 0f, 0f);
            _rigidbody.MoveRotation(rotation);

            var forwardVelocity = rotation * Vector3.forward * (forwardSpeed * SpeedMultiplier);
            var lateralVelocity = Vector3.right * (_steerInput.x * sideSpeed);
            _rigidbody.linearVelocity = forwardVelocity + lateralVelocity;

            if (visualRoot != null)
            {
                var targetBankAngle = -_steerInput.x * maxBankAngle;
                _currentBankAngle = Mathf.MoveTowards(_currentBankAngle, targetBankAngle, bankSpeedDegPerSec * Time.fixedDeltaTime);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, _currentBankAngle);
            }
        }
    }
}
