using System;
using Alchemy.Inspector;
using Opoint8182.Player;
using TripleA.Utils.Extensions;
using UnityEngine;

namespace Opoint8182.Building
{
    public class Building : MonoBehaviour
    {
        [Title("Weak Point")]
        [FoldoutGroup("Weak Point")] [SerializeField] private WeakPoint weakPoint;

        [Title("Crash Quality")]
        [FoldoutGroup("Crash Quality")] [SerializeField] private float referenceMaxSpeed = 25f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private float _lastCrashQuality;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private bool _lastHitWeakPoint;

        public event Action<float> Crashed;

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            var plane = collision.gameObject.GetComponent<PlaneController>();
            if (plane == null) return;

            var contactPoint = collision.GetContact(0).point;
            var hitWeakPoint = Vector3.Distance(contactPoint, weakPoint.Position) <= weakPoint.HitRadius;

            var quality = 0f;
            if (hitWeakPoint)
            {
                // collision.relativeVelocity is the impact velocity computed before Unity resolves the
                // collision response - reading plane.Velocity/Speed here would give the post-impact
                // (already-stopped) velocity instead, since resolution happens before this callback fires.
                var impactVelocity = collision.relativeVelocity;
                var speed01 = Mathf.Clamp01(impactVelocity.magnitude / referenceMaxSpeed);
                var alignment01 = Mathf.Clamp01(Vector3Math.GetDotProduct(impactVelocity.normalized, -weakPoint.OutwardNormal));
                quality = speed01 * alignment01;
            }

            _lastCrashQuality = quality;
            _lastHitWeakPoint = hitWeakPoint;

            Debug.Log($"[Building] '{name}' crashed - hitWeakPoint={hitWeakPoint}, quality={quality:0.00}");
            Crashed?.Invoke(quality);

            Destroy(gameObject);
        }
    }
}
