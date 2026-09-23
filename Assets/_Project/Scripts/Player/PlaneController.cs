using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Opoint8182.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlaneController : MonoBehaviour
    {
        [Title("Movement")]
        [FoldoutGroup("Movement")] [SerializeField] private float m_forwardSpeed = 20f;
        [FoldoutGroup("Movement")] [SerializeField] private float m_sideSpeed = 15f;
        [FoldoutGroup("Movement")] [SerializeField] private float m_verticalSpeed = 15f;

        [Title("Visual Rotation (cosmetic only)")]
        [FoldoutGroup("Visual Bank")] [SerializeField] private Transform m_visualRoot;
        [FoldoutGroup("Visual Bank")] [SerializeField] private float m_maxBankAngle = 40f;
        [FoldoutGroup("Visual Bank")] [SerializeField] private float m_bankSpeedDegPerSec = 180f;
        [FoldoutGroup("Visual Bank")] [SerializeField] private float m_maxPitchAngle = 60f;
        [FoldoutGroup("Visual Bank")] [FormerlySerializedAs("m_pitchRateDegPerSec")] [SerializeField] private float m_pitchSpeedDegPerSec = 60f;

        [Title("Input")]
        [FoldoutGroup("Input")] [SerializeField] private InputActionReference m_moveAction;
        [FoldoutGroup("Input")] [SerializeField] private float m_inputDeadZone = 0.1f;

        [Title("Boost")]
        [FoldoutGroup("Boost")] [SerializeField] private InputActionReference m_boostAction;
        [FoldoutGroup("Boost")] [SerializeField] private float m_boostSpeedMultiplier = 1.6f;
        [FoldoutGroup("Boost")] [SerializeField] private float m_boostSteerMultiplier = 0.5f;

        private Vector2 m_steerInput;
        private float m_currentBankAngle;
        private float m_currentPitchAngle;

        private Rigidbody m_rigidbody;

        public Vector3 Velocity => m_rigidbody.linearVelocity;
        public float Speed => m_rigidbody.linearVelocity.magnitude;
        public Vector3 Forward => transform.forward;
        public float SpeedMultiplier { get; set; } = 1f;
        public bool IsBoosting { get; private set; }
        public Vector2 TouchSteer { get; set; }
        public bool TouchBoost { get; set; }

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            m_moveAction.action.Enable();
            m_boostAction.action.Enable();
        }

        private void OnDisable()
        {
            m_moveAction.action.Disable();
            m_boostAction.action.Disable();
        }

        private void FixedUpdate()
        {
            m_steerInput = Vector2.ClampMagnitude(m_moveAction.action.ReadValue<Vector2>() + TouchSteer, 1f);
            if (m_steerInput.magnitude < m_inputDeadZone)
            {
                m_steerInput = Vector2.zero;
            }

            IsBoosting = m_boostAction.action.IsPressed() || TouchBoost;
            SpeedMultiplier = IsBoosting ? m_boostSpeedMultiplier : 1f;
            var steerMultiplier = IsBoosting ? m_boostSteerMultiplier : 1f;

            var forwardVelocity = Vector3.forward * (m_forwardSpeed * SpeedMultiplier);
            var lateralVelocity = Vector3.right * (m_steerInput.x * m_sideSpeed * steerMultiplier);
            var verticalVelocity = Vector3.up * (m_steerInput.y * m_verticalSpeed * steerMultiplier);
            m_rigidbody.linearVelocity = forwardVelocity + lateralVelocity + verticalVelocity;

            if (m_visualRoot != null)
            {
                var targetBankAngle = -m_steerInput.x * m_maxBankAngle;
                m_currentBankAngle = Mathf.MoveTowards(m_currentBankAngle, targetBankAngle, m_bankSpeedDegPerSec * Time.fixedDeltaTime);

                var targetPitchAngle = -m_steerInput.y * m_maxPitchAngle;
                m_currentPitchAngle = Mathf.MoveTowards(m_currentPitchAngle, targetPitchAngle, m_pitchSpeedDegPerSec * Time.fixedDeltaTime);

                m_visualRoot.localRotation = Quaternion.Euler(m_currentPitchAngle, 0f, m_currentBankAngle);
            }
        }
    }
}
