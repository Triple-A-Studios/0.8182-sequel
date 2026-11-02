using Opoint8182.Badges;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.BadgesEditor
{
	/// <summary>Editor shortcuts for checking the badges without flying for them.</summary>
	public static class BadgeMenu
	{
		[MenuItem("Tools/Badges/Log Badges")]
		private static void LogBadges()
		{
			var lines = new System.Text.StringBuilder($"[Badges] {BadgeService.CountedEarned()} / {BadgeCatalog.CountedTotal} earned\n");
			foreach (var badge in BadgeCatalog.All)
			{
				lines.AppendLine($"  {badge.Id}{(badge.Hidden ? " (hidden)" : string.Empty)}: {BadgeService.EarnedTiers(badge)} / {badge.TierCount} tiers" +
				                 (badge.Hidden ? string.Empty : $", progress {badge.FormatValue(badge.Progress())} {badge.Unit}"));
			}

			Debug.Log(lines.ToString());
		}

		[MenuItem("Tools/Badges/Evaluate Now")]
		private static void EvaluateNow()
		{
			var fresh = BadgeService.Evaluate();
			BadgeService.TakeNewUnlocks();
			Debug.Log($"[Badges] earned {fresh.Count} tier(s) now.");
			LogBadges();
		}

		[MenuItem("Tools/Badges/Reset Badges")]
		private static void ResetBadges()
		{
			BadgeService.Reset();
			Debug.Log("[Badges] reset (the stats are untouched, so Evaluate Now earns them back).");
		}

		[MenuItem("Tools/Badges/Add Big Sample Run (earns several tiers)")]
		private static void AddBigRun()
		{
			Opoint8182.Stats.PlayerStats.RecordRun(new Opoint8182.Stats.RunStats(9000, 120000f, 700, 20, 200, false, 150, 30, 7, 200f));
			EvaluateNow();
		}
	}
}
