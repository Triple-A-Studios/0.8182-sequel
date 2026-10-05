using Alchemy.Inspector;
using Opoint8182.Common;
using Opoint8182.Player;
using UnityEngine;

namespace Opoint8182.Building
{
    public class HealBuilding : MonoBehaviour, IRestorer
    {
        [Title("Weak Point")]
        [FoldoutGroup("Weak Point")] [SerializeField] private WeakPoint m_weakPoint;

        [Title("Heal Quality")]
        [FoldoutGroup("Heal Quality")] [SerializeField] private float m_referenceMaxSpeed = 25f;
        [FoldoutGroup("Heal Quality")] [SerializeField] private float m_maxRestoreAmount = 35f;

        public float Restore => m_maxRestoreAmount;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var plane = other.GetComponent<PlaneController>();
            if (plane == null) return;

            var planeRigidbody = other.attachedRigidbody;
            if (planeRigidbody == null) return;

            // Same trigger-based quality math as Building: the building has no Rigidbody, so
            // the plane's own velocity is the impact velocity.
            var impactVelocity = planeRigidbody.linearVelocity;
            var closestPoint = other.ClosestPoint(m_weakPoint.Position);
            var hitWeakPoint = Vector3.Distance(closestPoint, m_weakPoint.Position) <= m_weakPoint.HitRadius;

            var quality = hitWeakPoint ? Mathf.Clamp01(impactVelocity.magnitude / m_referenceMaxSpeed) : 0f;
            var amount = quality * m_maxRestoreAmount;

            Debug.Log($"[HealBuilding] '{name}' crashed - hitWeakPoint={hitWeakPoint}, quality={quality:0.00}, restoring {amount:0.0}");
            CombatEvents.RaiseRestored(this, amount);

            Destroy(gameObject);
        }
    }
}
