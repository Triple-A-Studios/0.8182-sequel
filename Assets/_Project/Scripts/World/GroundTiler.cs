using Opoint8182.Player;
using UnityEngine;

namespace Opoint8182.World
{
	/// <summary>
	/// The ground that never ends: a fixed set of tiles laid along Z around the plane. Each tile is slot-bound, so as the
	/// plane flies on, the tile that falls behind is simply moved to the front (no instantiate or destroy during the run).
	/// Visual only: the tiles have no colliders, and the end of a run on the deck comes from <c>AltitudeSystem</c>.
	/// </summary>
	public class GroundTiler : MonoBehaviour
	{
		[Tooltip("One ground tile, pivot at its centre (see Tools > World > Build Ground Tile).")]
		[SerializeField] private GameObject m_tilePrefab;

		[Tooltip("Distance between tile centres along Z. A little less than the tile's own length (60) on purpose: the white kerb at each tile edge then overlaps its neighbour's, so a seam looks like the kerb between a road and a lot.")]
		[SerializeField] private float m_tileLength = 56.6f;

		[Tooltip("Tiles kept in front of the plane. Ahead distance = this x Tile Length; keep it a little past the fog end.")]
		[SerializeField] private int m_tilesAhead = 5;

		[Tooltip("Tiles kept behind the plane, so the ground is under the camera and never shows a gap.")]
		[SerializeField] private int m_tilesBehind = 2;

		[Tooltip("Optional huge flat quad under the tiles, in the fog colour. It follows the plane and covers the part of the skybox below the horizon, so the far edge of the last tile is never seen.")]
		[SerializeField] private Transform m_skirt;

		[SerializeField] private PlaneController m_planeController;

		private Transform[] m_tiles;
		private GroundTileView[] m_views;
		private int[] m_slotIndex;
		private float m_originZ;
		private int m_lastIndex = int.MinValue;

		private void OnEnable()
		{
			if (m_planeController == null) m_planeController = FindAnyObjectByType<PlaneController>();
		}

		private void Start()
		{
			if (m_tilePrefab == null || m_planeController == null || m_tileLength <= 0f)
			{
				Debug.LogError("[GroundTiler] Needs a tile prefab, a PlaneController and a positive tile length: disabled.", this);
				enabled = false;
				return;
			}

			// Tile 0 starts centred under the plane, so a run always begins on a whole tile.
			m_originZ = m_planeController.transform.position.z;

			var count = m_tilesAhead + m_tilesBehind + 1;
			m_tiles = new Transform[count];
			m_views = new GroundTileView[count];
			m_slotIndex = new int[count];
			for (var i = 0; i < count; i++)
			{
				m_tiles[i] = Instantiate(m_tilePrefab, transform).transform;
				m_views[i] = m_tiles[i].GetComponent<GroundTileView>();
				m_slotIndex[i] = int.MinValue;
			}

			Layout(CurrentIndex());
		}

		private void LateUpdate()
		{
			var index = CurrentIndex();
			if (index != m_lastIndex) Layout(index);

			if (m_skirt != null)
			{
				var plane = m_planeController.transform.position;
				m_skirt.position = new Vector3(plane.x, m_skirt.position.y, plane.z);
			}
		}

		private int CurrentIndex()
		{
			return Mathf.FloorToInt((m_planeController.transform.position.z - m_originZ) / m_tileLength + 0.5f);
		}

		// Tile k always lives in slot k mod count, so only the tiles that left the window actually move (and are re-dressed).
		private void Layout(int index)
		{
			m_lastIndex = index;
			var count = m_tiles.Length;
			var first = index - m_tilesBehind;

			for (var k = first; k < first + count; k++)
			{
				var slot = ((k % count) + count) % count;
				if (m_slotIndex[slot] == k) continue;

				var tile = m_tiles[slot];
				var position = tile.position;
				tile.position = new Vector3(position.x, position.y, m_originZ + k * m_tileLength);
				m_slotIndex[slot] = k;

				var view = m_views[slot];
				if (view != null && view.DressingCount > 0) view.SetDressing(DressingFor(k, view.DressingCount));
			}
		}

		// A fixed hash of the tile index, so the same stretch of ground is always dressed the same way and neighbours
		// vary without a pattern.
		private static int DressingFor(int tileIndex, int count)
		{
			var hash = unchecked((uint)tileIndex * 2654435761u + 40503u);
			return (int)((hash >> 8) % (uint)count);
		}
	}
}
