using System;
using System.Collections.Generic;
using UnityEngine;

namespace Opoint8182.Badges
{
	/// <summary>
	/// Which badges the player put on the Profile (Player profile & run stats Pass 6b). Up to <see cref="MAX"/> earned counted badges, read back
	/// in catalog order whatever order they were ticked in. Before the player has ever saved a pick the set is derived: the first
	/// <see cref="MAX"/> earned badges in catalog order. After the first <see cref="Save"/> the stored set is used and never changes by itself, so a
	/// badge earned later appears only once picked, and fewer than <see cref="MAX"/> picked leaves free slots. Earned hidden badges are not
	/// picked: <see cref="ProfileBadges"/> always puts them first. Local only (<see cref="PlayerPrefs"/>), like the badges.
	/// </summary>
	public static class BadgePicks
	{
		public const int MAX = 6;

		private const string k_PickedKey = "badge.picked";
		private const string k_HasPickedKey = "badge.has_picked";

		/// <summary>Raised after <see cref="Save"/> or <see cref="Reset"/> changed the set.</summary>
		public static event Action Changed;

		/// <summary>True once the player has saved a pick (the set is then stored, not derived).</summary>
		public static bool HasPicked => PlayerPrefs.GetInt(k_HasPickedKey, 0) != 0;

		/// <summary>The picked counted badges in catalog order, at most <see cref="MAX"/>: derived until the first save, stored after.</summary>
		public static List<BadgeDefinition> Current()
		{
			var result = new List<BadgeDefinition>();

			if (!HasPicked)
			{
				foreach (var badge in BadgeCatalog.All)
				{
					if (result.Count == MAX) break;
					if (IsPickable(badge)) result.Add(badge);
				}

				return result;
			}

			var stored = new HashSet<string>(PlayerPrefs.GetString(k_PickedKey, string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));
			foreach (var badge in BadgeCatalog.All)
			{
				if (result.Count == MAX) break;
				if (stored.Contains(badge.Id) && IsPickable(badge)) result.Add(badge);
			}

			return result;
		}

		/// <summary>What the Profile's badge row shows: every earned hidden badge first, then the picked ones.</summary>
		public static List<BadgeDefinition> ProfileBadges()
		{
			var result = new List<BadgeDefinition>();
			foreach (var badge in BadgeCatalog.All)
			{
				if (badge.Hidden && BadgeService.EarnedTiers(badge) > 0) result.Add(badge);
			}

			result.AddRange(Current());
			return result;
		}

		/// <summary>Whether <paramref name="badge"/> is among <see cref="Current"/>.</summary>
		public static bool IsPicked(BadgeDefinition badge) => Current().Contains(badge);

		/// <summary>A badge can be picked when it is a counted one with at least one tier earned.</summary>
		public static bool IsPickable(BadgeDefinition badge) => !badge.Hidden && BadgeService.EarnedTiers(badge) > 0;

		/// <summary>True when at least one counted badge is earned, i.e. there is something to pick (the PICK button shows only then).</summary>
		public static bool AnythingToPick()
		{
			foreach (var badge in BadgeCatalog.All)
			{
				if (IsPickable(badge)) return true;
			}

			return false;
		}

		/// <summary>
		/// Stores the picked set (the player pressed DONE). Ids that are unknown, hidden, not earned or repeated are ignored, and only the first
		/// <see cref="MAX"/> pickable ones in catalog order are kept. Returns how many were saved.
		/// </summary>
		public static int Save(IEnumerable<string> ids)
		{
			var wanted = new HashSet<string>(ids);
			var kept = new List<string>();
			foreach (var badge in BadgeCatalog.All)
			{
				if (kept.Count == MAX) break;
				if (wanted.Contains(badge.Id) && IsPickable(badge)) kept.Add(badge.Id);
			}

			PlayerPrefs.SetString(k_PickedKey, string.Join(",", kept));
			PlayerPrefs.SetInt(k_HasPickedKey, 1);
			PlayerPrefs.Save();
			Changed?.Invoke();
			return kept.Count;
		}

		/// <summary>Forgets the stored pick, so the set is derived again (Editor test menu; also called when the badges are reset).</summary>
		public static void Reset()
		{
			PlayerPrefs.DeleteKey(k_PickedKey);
			PlayerPrefs.DeleteKey(k_HasPickedKey);
			PlayerPrefs.Save();
			Changed?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the handlers.
			Changed = null;
		}
	}
}
