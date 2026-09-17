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

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private float m_lastCrashQuality;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool m_lastHitWeakPoint;

        public float Damage => m_toughHitDamage;
        public int ScoreValue => m_scoreValue;

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            var plane = collision.gameObject.GetComponent<PlaneController>();
            if (plane == null) return;

            var contactPoint = collision.GetContact(0).point;
            var hitWeakPoint = Vector3.Distance(contactPoint, m_weakPoint.Position) <= m_weakPoint.HitRadius;

            // collision.relativeVelocity is the impact velocity computed before Unity resolves the
            // collision response - reading plane.Velocity/Speed here would give the post-impact
            // (already-stopped) velocity instead, since resolution happens before this callback fires.
            var impactVelocity = collision.relativeVelocity;

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

            m_lastCrashQuality = quality;
            m_lastHitWeakPoint = hitWeakPoint;

            Debug.Log($"[Building] '{name}' crashed - hitWeakPoint={hitWeakPoint}, quality={quality:0.00}");
            CombatEvents.RaiseCrashed(this, quality);

            Destroy(gameObject);
        }
    }
}
