using System.Collections.Generic;
using System.Globalization;
using Opoint8182.Stats;

namespace Opoint8182.Badges
{
	/// <summary>
	/// Every badge in the game, in the order the Profile shows them (a hidden one that was earned is drawn before the rest). Tier thresholds
	/// are placeholders to tune from play: each is one array of three values, tier 3 (bronze), tier 2 (silver), tier 1 (gold). To add a hidden
	/// badge or an easter egg, add a <see cref="BadgeDefinition.Secret"/> entry and, for an egg, call <c>BadgeService.Unlock("id")</c> where it is found.
	/// </summary>
	public static class BadgeCatalog
	{
		public static readonly BadgeDefinition[] All =
		{
			BadgeDefinition.Tiered("flattened", "DEMO\nCREW", "Buildings you have flattened",
				new long[] { 100, 500, 2500 }, () => PlayerStats.BuildingsDestroyed, Count, "buildings flattened"),

			BadgeDefinition.Tiered("streak", "HOT\nSTREAK", "Buildings crashed in a row without a mistake",
				new long[] { 8, 15, 30 }, () => PlayerStats.BestCombo, Count, "buildings in a row", true),

			BadgeDefinition.Tiered("perfect", "PERFECT\nPILOT", "Perfect crashes on a weak point",
				new long[] { 25, 150, 600 }, () => PlayerStats.PerfectCrashes, Count, "perfect crashes"),

			BadgeDefinition.Tiered("bigrun", "BIG\nRUN", "Score in a single ranked run",
				new long[] { 3000, 8000, 20000 }, () => PlayerStats.HighScore, Count, "points in one run", true),

			BadgeDefinition.Tiered("roadtrip", "ROAD\nTRIP", "Distance flown in total",
				new long[] { 25000, 100000, 300000 }, () => (long)PlayerStats.DistanceMeters, Kilometers, "flown"),

			BadgeDefinition.Tiered("heavy", "HEAVY\nHITTER", "Tough buildings you have broken",
				new long[] { 20, 100, 400 }, () => PlayerStats.ToughBroken, Count, "tough buildings broken"),

			BadgeDefinition.Tiered("endurance", "LONG\nHAUL", "Time in the air in a single run",
				new long[] { 90, 180, 300 }, () => (long)PlayerStats.LongestRunSeconds, Clock, "in one run", true),

			BadgeDefinition.Tiered("precision", "DEAD\nEYE", "Perfect crashes in a row",
				new long[] { 3, 6, 12 }, () => PlayerStats.BestPerfectStreak, Count, "perfect crashes in a row", true),

			BadgeDefinition.Tiered("hoarder", "POINT\nHOARDER", "Points scored over all your runs",
				new long[] { 25000, 150000, 1000000 }, () => PlayerStats.TotalScore, Count, "points scored"),

			BadgeDefinition.Tiered("medic", "MEDIC", "Heal buildings you have used",
				new long[] { 5, 25, 100 }, () => PlayerStats.HealsUsed, Count, "heal buildings used"),

			// Nothing collects coins yet (the currency comes with the Beta shop milestone), so this one stays at 0 until it does.
			BadgeDefinition.Tiered("coins", "COIN\nSTASH", "Coins you have collected in total",
				new long[] { 1000, 10000, 50000 }, () => PlayerStats.CoinsCollected, Count, "coins collected"),

			// Hidden: not counted in the Profile's x / y, shown only once earned.
			BadgeDefinition.Secret("allstar", "ALL\nSTAR", "Earn every badge", EveryBadgeEarned),
		};

		/// <summary>The badges that count in the Profile's x / y (everything but the hidden ones).</summary>
		public static int CountedTotal
		{
			get
			{
				var total = 0;
				foreach (var badge in All)
				{
					if (!badge.Hidden) total++;
				}

				return total;
			}
		}

		public static BadgeDefinition Find(string id)
		{
			foreach (var badge in All)
			{
				if (badge.Id == id) return badge;
			}

			return null;
		}

		/// <summary>The badges in Profile order: earned hidden ones first, then the counted ones, never an unearned hidden one.</summary>
		public static List<BadgeDefinition> VisibleOrdered()
		{
			var list = new List<BadgeDefinition>();
			foreach (var badge in All)
			{
				if (badge.Hidden && BadgeService.EarnedTiers(badge) > 0) list.Add(badge);
			}

			foreach (var badge in All)
			{
				if (!badge.Hidden) list.Add(badge);
			}

			return list;
		}

		private static bool EveryBadgeEarned()
		{
			foreach (var badge in All)
			{
				if (!badge.Hidden && BadgeService.EarnedTiers(badge) == 0) return false;
			}

			return true;
		}

		private static string Count(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

		private static string Kilometers(long meters) =>
			meters >= 1000 ? (meters / 1000d).ToString("0.#", CultureInfo.InvariantCulture) + " km" : meters.ToString(CultureInfo.InvariantCulture) + " m";

		private static string Clock(long seconds) => $"{seconds / 60}:{seconds % 60:00}";
	}
}
