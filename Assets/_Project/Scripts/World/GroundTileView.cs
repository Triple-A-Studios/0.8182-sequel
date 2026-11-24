using UnityEngine;

namespace Opoint8182.World
{
	/// <summary>
	/// Sits on the ground tile prefab and holds its side-dressing variants (houses, shops, trees outside the flight lane,
	/// baked by <c>Tools &gt; World &gt; Build Ground Dressing</c>). Exactly one variant is active, picked by
	/// <see cref="GroundTiler"/> each time the tile is placed.
	/// </summary>
	public class GroundTileView : MonoBehaviour
	{
		[Tooltip("One child per dressing variant; only the picked one is active.")]
		[SerializeField] private GameObject[] m_dressings;

		public int DressingCount => m_dressings == null ? 0 : m_dressings.Length;

		public void SetDressing(int index)
		{
			if (m_dressings == null) return;

			for (var i = 0; i < m_dressings.Length; i++)
			{
				if (m_dressings[i] != null) m_dressings[i].SetActive(i == index);
			}
		}
	}
}
