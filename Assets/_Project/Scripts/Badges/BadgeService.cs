using System;
using System.Collections.Generic;
using UnityEngine;

namespace Opoint8182.Badges
{
	/// <summary>One tier of one badge that was just earned. <see cref="Tier"/> counts down like the badge: 3 (bronze), 2 (silver), 1 (gold); a hidden badge reports 1.</summary>
	public readonly struct BadgeUnlock
	{
		public readonly BadgeDefinition Badge;
		public readonly int Tier;

		public BadgeUnlock(BadgeDefinition badge, int tier)
		{
			Badge = badge;
			Tier = tier;
		}
	}

	/// <summary>
	/// The player's badge progress (Player profile & run stats Pass 5): how many tiers of each badge are earned, saved locally in
	/// <see cref="PlayerPrefs"/> (<c>badge.&lt;id&gt;</c>) like the stats the tiers read. Earning only ever goes up, so a stat reset does not
	/// take a badge away. <see cref="Evaluate"/> checks every badge against the stats (the run end calls it; the Profile does too, so badges for
	/// stats that existed before the badges are picked up silently); <see cref="Unlock"/> earns a hidden badge directly (an easter egg).
	/// Fresh unlocks wait in a queue that <see cref="TakeNewUnlocks"/> empties, which is how Game Over announces them.
	/// </summary>
	public static class BadgeService
	{
		private const string k_KeyPrefix = "badge.";
		private const string k_SeenSuffix = ".seen";

		private static readonly Dictionary<string, int> m_S_Earned = new();
		private static readonly List<BadgeUnlock> m_S_Pending = new();

		/// <summary>Raised for every tier earned, as it is earned.</summary>
		public static event Action<BadgeUnlock> Unlocked;

		/// <summary>How many tiers of <paramref name="badge"/> are earned (0 to <see cref="BadgeDefinition.TierCount"/>).</summary>
		public static int EarnedTiers(BadgeDefinition badge)
		{
			if (m_S_Earned.TryGetValue(badge.Id, out var earned)) return earned;

			earned = Mathf.Clamp(PlayerPrefs.GetInt(k_KeyPrefix + badge.Id, 0), 0, badge.TierCount);
			m_S_Earned[badge.Id] = earned;
			return earned;
		}

		/// <summary>The best tier earned as its number: 3 (bronze), 2 (silver), 1 (gold), 0 for none.</summary>
		public static int HighestTier(BadgeDefinition badge)
		{
			var earned = EarnedTiers(badge);
			return earned == 0 ? 0 : badge.TierCount - earned + 1;
		}

		/// <summary>An earned hidden badge the player has not opened yet: the Profile shows a pip until they do.</summary>
		public static bool IsUnseen(BadgeDefinition badge)
		{
			return badge.Hidden && EarnedTiers(badge) > 0 && PlayerPrefs.GetInt(k_KeyPrefix + badge.Id + k_SeenSuffix, 0) == 0;
		}

		/// <summary>True when any earned hidden badge is still unseen (the menu's PROFILE chip shows a pip).</summary>
		public static bool HasUnseenHidden()
		{
			foreach (var badge in BadgeCatalog.All)
			{
				if (IsUnseen(badge)) return true;
			}

			return false;
		}

		/// <summary>Marks a hidden badge as seen (called when its tile is tapped).</summary>
		public static void MarkSeen(BadgeDefinition badge)
		{
			if (!badge.Hidden || !IsUnseen(badge)) return;

			PlayerPrefs.SetInt(k_KeyPrefix + badge.Id + k_SeenSuffix, 1);
			PlayerPrefs.Save();
		}

		/// <summary>The counted badges with at least one tier earned (the x of the Profile's x / y).</summary>
		public static int CountedEarned()
		{
			var count = 0;
			foreach (var badge in BadgeCatalog.All)
			{
				if (!badge.Hidden && EarnedTiers(badge) > 0) count++;
			}

			return count;
		}

		/// <summary>Checks every badge against the stats and earns what is due. Returns the tiers earned by this call (also queued for <see cref="TakeNewUnlocks"/>).</summary>
		public static List<BadgeUnlock> Evaluate()
		{
			var fresh = new List<BadgeUnlock>();

			foreach (var badge in BadgeCatalog.All)
			{
				if (badge.Hidden) continue;

				var progress = badge.Progress();
				var due = 0;
				foreach (var threshold in badge.Tiers)
				{
					if (progress >= threshold) due++;
				}

				while (EarnedTiers(badge) < due) Earn(badge, fresh);
			}

			// After the counted ones, so a badge about the others sees what this call just earned.
			foreach (var badge in BadgeCatalog.All)
			{
				if (badge.Hidden && badge.Condition != null && EarnedTiers(badge) == 0 && badge.Condition()) Earn(badge, fresh);
			}

			if (fresh.Count > 0) PlayerPrefs.Save();
			return fresh;
		}

		/// <summary>Earns a hidden badge (an easter egg found in the game). False when the id is unknown, not hidden or already earned.</summary>
		public static bool Unlock(string id)
		{
			var badge = BadgeCatalog.Find(id);
			if (badge == null || !badge.Hidden || EarnedTiers(badge) > 0) return false;

			Earn(badge, null);
			PlayerPrefs.Save();
			return true;
		}

		/// <summary>The unlocks since the last call, oldest first, and empties the queue.</summary>
		public static List<BadgeUnlock> TakeNewUnlocks()
		{
			var taken = new List<BadgeUnlock>(m_S_Pending);
			m_S_Pending.Clear();
			return taken;
		}

		/// <summary>Forgets every earned badge (Editor test menu).</summary>
		public static void Reset()
		{
			foreach (var badge in BadgeCatalog.All)
			{
				PlayerPrefs.DeleteKey(k_KeyPrefix + badge.Id);
				PlayerPrefs.DeleteKey(k_KeyPrefix + badge.Id + k_SeenSuffix);
			}

			PlayerPrefs.Save();
			m_S_Earned.Clear();
			m_S_Pending.Clear();
		}

		private static void Earn(BadgeDefinition badge, List<BadgeUnlock> fresh)
		{
			var earned = EarnedTiers(badge) + 1;
			m_S_Earned[badge.Id] = earned;
			PlayerPrefs.SetInt(k_KeyPrefix + badge.Id, earned);

			var unlock = new BadgeUnlock(badge, badge.TierCount - earned + 1);
			fresh?.Add(unlock);
			m_S_Pending.Add(unlock);
			Unlocked?.Invoke(unlock);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache, the queue and the handlers.
			m_S_Earned.Clear();
			m_S_Pending.Clear();
			Unlocked = null;
		}
	}
}
