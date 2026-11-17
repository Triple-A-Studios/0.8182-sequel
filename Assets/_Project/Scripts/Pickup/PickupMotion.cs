using Alchemy.Inspector;
using UnityEngine;

namespace Opoint8182.Pickup
{
	/// <summary>
	/// Spins and bobs a pickup's visual so it reads as collectable. Cosmetic only: the trigger on the root does not move.
	/// </summary>
	public class PickupMotion : MonoBehaviour
	{
		[Title("Visual")]
		[Tooltip("The child that spins and bobs (not the root: the trigger stays put).")]
		[FoldoutGroup("Visual")] [SerializeField] private Transform m_visual;

		[Title("Motion")]
		[Tooltip("Degrees per second around the vertical axis.")]
		[FoldoutGroup("Motion")] [SerializeField] private float m_spinSpeed = 120f;
		[Tooltip("Half the height of the bob, in world units.")]
		[FoldoutGroup("Motion")] [SerializeField] private float m_bobHeight = 0.3f;
		[Tooltip("Bob cycles per second.")]
		[FoldoutGroup("Motion")] [SerializeField] private float m_bobSpeed = 0.8f;

		private Vector3 m_basePosition;
		private float m_phase;

		private void Awake()
		{
			if (m_visual == null) return;

			m_basePosition = m_visual.localPosition;
			// A random start, so a row of pickups does not bob in lockstep.
			m_phase = Random.value * Mathf.PI * 2f;
		}

		private void Update()
		{
			if (m_visual == null) return;

			m_visual.Rotate(0f, m_spinSpeed * Time.deltaTime, 0f, Space.World);
			var bob = Mathf.Sin(Time.time * m_bobSpeed * Mathf.PI * 2f + m_phase) * m_bobHeight;
			m_visual.localPosition = m_basePosition + new Vector3(0f, bob, 0f);
		}
	}
}
