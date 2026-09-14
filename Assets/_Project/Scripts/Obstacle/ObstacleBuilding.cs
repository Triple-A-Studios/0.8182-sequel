using Alchemy.Inspector;
using Opoint8182.Common;
using UnityEngine;

namespace Opoint8182.Obstacle
{
    public class ObstacleBuilding : MonoBehaviour, IDamageDealer
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_hitDamage = 20f;

        public float Damage => m_hitDamage;

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            Debug.Log($"[ObstacleBuilding] '{name}' hit by player - dealing {m_hitDamage:0.0} damage");
            CombatEvents.RaiseDamageDealt(this, m_hitDamage);
        }
    }
}
