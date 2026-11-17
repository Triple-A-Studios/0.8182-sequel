using Alchemy.Inspector;
using Opoint8182.Common;
using UnityEngine;

namespace Opoint8182.Obstacle
{
    public class BirdHazard : MonoBehaviour, IDamageDealer
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_hitDamage = 10f;

        [Title("Patrol")]
        [FoldoutGroup("Patrol")] [SerializeField] private float m_moveSpeed = 8f;
        [FoldoutGroup("Patrol")] [SerializeField] private float m_patrolDistance = 6f;

        [Title("Visual")]
        [Tooltip("The model's parent, turned to face the patrol direction. The model must face +Z.")]
        [FoldoutGroup("Visual")] [SerializeField] private Transform m_visual;

        public float Damage => m_hitDamage;

        private Vector3 m_startPosition;
        private int m_direction = 1;

        private void Awake()
        {
            m_startPosition = transform.position;
            FaceDirection();
        }

        private void Update()
        {
            transform.position += Vector3.right * (m_direction * m_moveSpeed * Time.deltaTime);

            if (Vector3.Distance(m_startPosition, transform.position) >= m_patrolDistance)
            {
                m_direction *= -1;
                FaceDirection();
            }
        }

        // Cosmetic only: the trigger and the patrol stay on the root and do not turn.
        private void FaceDirection()
        {
            if (m_visual == null) return;

            m_visual.localRotation = Quaternion.Euler(0f, m_direction > 0 ? 90f : -90f, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;

            Debug.Log($"[BirdHazard] '{name}' hit by player - dealing {m_hitDamage:0.0} damage");
            CombatEvents.RaiseDamageDealt(this, m_hitDamage);
        }
    }
}
