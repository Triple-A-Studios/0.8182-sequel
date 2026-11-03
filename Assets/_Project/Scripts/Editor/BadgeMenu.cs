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

		[MenuItem("Tools/Badges/Log Picks")]
		private static void LogPicks()
		{
			Debug.Log($"[Badges] picks: {(BadgePicks.HasPicked ? "stored" : "derived (first 6 earned)")}, anything to pick: {BadgePicks.AnythingToPick()}\n" +
			          $"  picked: {Names(BadgePicks.Current())}\n  profile row: {Names(BadgePicks.ProfileBadges())}");
		}

		[MenuItem("Tools/Badges/Save Picks: Last 3 Earned")]
		private static void SaveLastThree()
		{
			var earned = new System.Collections.Generic.List<string>();
			foreach (var badge in BadgeCatalog.All)
			{
				if (BadgePicks.IsPickable(badge)) earned.Add(badge.Id);
			}

			var last = earned.GetRange(System.Math.Max(0, earned.Count - 3), System.Math.Min(3, earned.Count));
			Debug.Log($"[Badges] saved {BadgePicks.Save(last)} pick(s).");
			LogPicks();
		}

		[MenuItem("Tools/Badges/Save Picks: Try 8 (keeps 6)")]
		private static void SaveEight()
		{
			var all = new System.Collections.Generic.List<string>();
			foreach (var badge in BadgeCatalog.All) all.Add(badge.Id);
			Debug.Log($"[Badges] saved {BadgePicks.Save(all)} pick(s) of {all.Count} offered (hidden, unearned and the 7th onwards are dropped).");
			LogPicks();
		}

		[MenuItem("Tools/Badges/Clear Picks")]
		private static void ClearPicks()
		{
			BadgePicks.Reset();
			Debug.Log("[Badges] picks cleared (derived again).");
			LogPicks();
		}

		private static string Names(System.Collections.Generic.List<BadgeDefinition> badges)
		{
			if (badges.Count == 0) return "(none)";
			var names = new System.Collections.Generic.List<string>();
			foreach (var badge in badges) names.Add(badge.Id);
			return string.Join(", ", names);
		}

		[MenuItem("Tools/Badges/Unlock Hidden: ALL STAR (test)")]
		private static void UnlockAllStar()
		{
			Debug.Log($"[Badges] ALL STAR unlocked: {BadgeService.Unlock("allstar")} (false = already earned).");
			LogBadges();
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
