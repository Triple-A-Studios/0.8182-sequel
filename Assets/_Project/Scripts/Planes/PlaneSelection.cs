using System;
using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// The plane the player has equipped, saved locally in <see cref="PlayerPrefs"/> like <see cref="SettingsService"/>.
	/// Static, so the menu, the shop and the run all read the same answer with no scene object. A saved id that is not in the
	/// <see cref="PlaneCatalog"/> (a removed plane, a corrupt pref) reads as the catalog default. There is no ownership
	/// yet: the Beta shop adds its owned-set check in front of <see cref="Equip"/>. A change writes straight to disk, it
	/// happens a few times a month rather than while dragging a slider.
	/// </summary>
	public static class PlaneSelection
	{
		private const string k_EquippedKey = "plane.equipped";

		private static PlaneCatalog m_s_catalog;
		private static string m_s_equippedId;

		/// <summary>Raised after the equipped plane actually changed (not for an equip of the current plane).</summary>
		public static event Action Changed;

		/// <summary>The loaded catalog, or null when the asset is missing.</summary>
		public static PlaneCatalog Catalog
		{
			get
			{
				if (m_s_catalog == null) m_s_catalog = Resources.Load<PlaneCatalog>(PlaneCatalog.RESOURCE_NAME);
				return m_s_catalog;
			}
		}

		/// <summary>The equipped plane's definition, or null when the catalog is missing or empty.</summary>
		public static PlaneDefinition Equipped
		{
			get
			{
				var catalog = Catalog;
				if (catalog == null) return null;

				EnsureLoaded();
				return catalog.TryGet(m_s_equippedId, out var plane) ? plane : catalog.Default;
			}
		}

		/// <summary>Id of <see cref="Equipped"/>, or an empty string when there is none.</summary>
		public static string EquippedId
		{
			get
			{
				var plane = Equipped;
				return plane == null ? string.Empty : plane.Id;
			}
		}

		/// <summary>Equips a plane from the catalog. False (nothing changes) for an unknown id.</summary>
		public static bool Equip(string id)
		{
			var catalog = Catalog;
			if (catalog == null || !catalog.TryGet(id, out _)) return false;

			var previous = EquippedId;
			m_s_equippedId = id;
			PlayerPrefs.SetString(k_EquippedKey, id);
			PlayerPrefs.Save();

			if (previous != id) Changed?.Invoke();
			return true;
		}

		/// <summary>Forgets the choice, so the catalog default is equipped again.</summary>
		public static void Reset()
		{
			var previous = EquippedId;
			m_s_equippedId = string.Empty;
			PlayerPrefs.DeleteKey(k_EquippedKey);
			PlayerPrefs.Save();

			if (previous != EquippedId) Changed?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the caches so the pref and the asset are read again.
			m_s_catalog = null;
			m_s_equippedId = null;
			Changed = null;
		}

		private static void EnsureLoaded()
		{
			if (m_s_equippedId != null) return;

			m_s_equippedId = PlayerPrefs.GetString(k_EquippedKey, string.Empty);
		}
	}
}
