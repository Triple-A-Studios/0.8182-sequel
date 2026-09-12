using Alchemy.Inspector;
using UnityEngine;

namespace Opoint8182.Building
{
    public class WeakPoint : MonoBehaviour
    {
        [Title("Weak Point")]
        [FoldoutGroup("Weak Point")] [SerializeField] private float m_hitRadius = 1.5f;

        public float HitRadius => m_hitRadius;
        public Vector3 Position => transform.position;
        public Vector3 OutwardNormal => transform.forward;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_hitRadius);
        }
    }
}
