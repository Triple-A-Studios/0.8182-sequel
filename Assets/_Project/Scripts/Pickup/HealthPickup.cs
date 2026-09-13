using System;
using Alchemy.Inspector;
using Opoint8182.Common;
using UnityEngine;

namespace Opoint8182.Pickup
{
    public class HealthPickup : MonoBehaviour, IRestorer
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_restoreAmount = 25f;

        public float Restore => m_restoreAmount;

        public event Action<float> Restored;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;

            Debug.Log($"[HealthPickup] '{name}' collected - restoring {m_restoreAmount:0.0}");
            Restored?.Invoke(m_restoreAmount);

            Destroy(gameObject);
        }
    }
}
