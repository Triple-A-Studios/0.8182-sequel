using Alchemy.Inspector;
using Opoint8182.Common;
using Opoint8182.Player;
using UnityEngine;

namespace Opoint8182.Building
{
    public class Building : MonoBehaviour, IDamageDealer, ICrashSource
    {
        [Title("Weak Point")]
        [FoldoutGroup("Weak Point")] [SerializeField] private WeakPoint m_weakPoint;

        [Title("Crash Quality")]
        [FoldoutGroup("Crash Quality")] [SerializeField] private float m_referenceMaxSpeed = 25f;

        [Title("Tough Settings")]
        [FoldoutGroup("Tough Settings")] [SerializeField] private BuildingType m_buildingType = BuildingType.Normal;
        [FoldoutGroup("Tough Settings")] [SerializeField] private float m_toughBreakSpeed = 30f;
        [FoldoutGroup("Tough Settings")] [SerializeField] private float m_toughHitDamage = 35f;

        [Title("Scoring")]
        [FoldoutGroup("Scoring")] [SerializeField] private int m_scoreValue = 10;

        public float Damage => m_toughHitDamage;
        public int ScoreValue => m_scoreValue;
        public BuildingType BuildingType => m_buildingType;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var plane = other.GetComponent<PlaneController>();
            if (plane == null) return;

            var planeRigidbody = other.attachedRigidbody;
            if (planeRigidbody == null) return;

            // The building has no Rigidbody of its own (static as far as PhysX is concerned), so its
            // velocity is always zero - the plane's own Rigidbody velocity is therefore exactly
            // equivalent to the old collision.relativeVelocity (velocityA - velocityB, velocityB == 0).
            // Triggers skip collision resolution entirely, so unlike OnCollisionEnter there's no
            // pre/post-impact distinction here - this is just the plane's live velocity.
            var impactVelocity = planeRigidbody.linearVelocity;

            // No contact manifold on a trigger - use the closest point on the plane's own collider to
            // the weak point as the equivalent of the old collision contact point.
            var closestPoint = other.ClosestPoint(m_weakPoint.Position);
            var hitWeakPoint = Vector3.Distance(closestPoint, m_weakPoint.Position) <= m_weakPoint.HitRadius;

            var canBreak = m_buildingType == BuildingType.Normal || impactVelocity.magnitude >= m_toughBreakSpeed;
            if (!canBreak)
            {
                Debug.Log($"[Building] '{name}' mistimed tough hit - impact speed {impactVelocity.magnitude:0.0} below tough threshold {m_toughBreakSpeed:0.0}, dealing {m_toughHitDamage:0.0} damage, destroying with base score");
                CombatEvents.RaiseDamageDealt(this, m_toughHitDamage);
                CombatEvents.RaiseCrashed(this, 0f, countsForCombo: false);
                Destroy(gameObject);
                return;
            }

            var quality = hitWeakPoint ? Mathf.Clamp01(impactVelocity.magnitude / m_referenceMaxSpeed) : 0f;

            Debug.Log($"[Building] '{name}' crashed - hitWeakPoint={hitWeakPoint}, quality={quality:0.00}");
            CombatEvents.RaiseCrashed(this, quality);

            Destroy(gameObject);
        }
    }
}
