using System;

namespace Opoint8182.Badges
{
	/// <summary>
	/// One badge. A <b>counted</b> badge has three tiers (thresholds ascending: tier 3 first, bronze; tier 1 last, gold) and reads its progress
	/// from a stat. A <b>hidden</b> badge has one unlock, is not counted in the Profile's x / y, shows only once earned and is drawn first with a
	/// glow; it unlocks from a <see cref="Condition"/> (checked by <see cref="BadgeService.Evaluate"/>) or by a call to
	/// <see cref="BadgeService.Unlock"/> (an easter egg). Add new ones in <see cref="BadgeCatalog"/>.
	/// </summary>
	public sealed class BadgeDefinition
	{
		/// <summary>Stable key of the saved unlock state (<c>badge.&lt;id&gt;</c>); never change it once released.</summary>
		public string Id { get; }

		/// <summary>The tile text; a line break keeps it inside the tile.</summary>
		public string Title { get; }

		/// <summary>What the badge is about, shown under the title in the popup.</summary>
		public string Description { get; }

		public bool Hidden { get; }

		/// <summary>Ascending thresholds of the tiers (tier 3, tier 2, tier 1); empty for a single-unlock (hidden) badge.</summary>
		public long[] Tiers { get; }

		/// <summary>The stat the tiers measure; null for a single-unlock badge.</summary>
		public Func<long> Progress { get; }

		/// <summary>A value of the stat as text ("1,200", "25 km", "1:30").</summary>
		public Func<long, string> FormatValue { get; }

		/// <summary>What the value counts, after it: "buildings flattened", "points in one run".</summary>
		public string Unit { get; }

		/// <summary>The stat is a best (one run, or a streak) and not a running total: the all-time line reads "Best" instead of "Total".</summary>
		public bool IsBest { get; }

		/// <summary>Unlocks a hidden badge by itself; null means it only unlocks through <see cref="BadgeService.Unlock"/>.</summary>
		public Func<bool> Condition { get; }

		public int TierCount => Tiers.Length > 0 ? Tiers.Length : 1;

		public string SingleLineTitle => Title.Replace('\n', ' ');

		private BadgeDefinition(string id, string title, string description, bool hidden, long[] tiers, Func<long> progress,
			Func<long, string> formatValue, string unit, bool isBest, Func<bool> condition)
		{
			Id = id;
			Title = title;
			Description = description;
			Hidden = hidden;
			Tiers = tiers ?? Array.Empty<long>();
			Progress = progress;
			FormatValue = formatValue;
			Unit = unit;
			IsBest = isBest;
			Condition = condition;
		}

		/// <summary>A counted badge: three tiers measured by a stat.</summary>
		public static BadgeDefinition Tiered(string id, string title, string description, long[] tiers, Func<long> progress,
			Func<long, string> formatValue, string unit, bool isBest = false)
		{
			return new BadgeDefinition(id, title, description, false, tiers, progress, formatValue, unit, isBest, null);
		}

		/// <summary>A hidden badge. <paramref name="condition"/> may be null when only <see cref="BadgeService.Unlock"/> earns it.</summary>
		public static BadgeDefinition Secret(string id, string title, string description, Func<bool> condition = null)
		{
			return new BadgeDefinition(id, title, description, true, null, null, null, null, false, condition);
		}
	}
}
