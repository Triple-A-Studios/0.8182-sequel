using Opoint8182.Badges;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// The popup a badge tile opens (<c>popup-badge</c> in <c>MainMenu.uxml</c>): the badge's name and what it measures, one row per tier (done,
	/// the next one with its progress, the rest dimmed) and, once every tier is done, a muted all-time line. A hidden badge shows only its
	/// description. The owner shows the popup element through the <see cref="ScreenRouter"/> and wires the CLOSE button.
	/// </summary>
	public class BadgePopup
	{
		private readonly Label m_title;
		private readonly Label m_subtitle;
		private readonly VisualElement m_tiers;
		private readonly Label m_total;

		public BadgePopup(VisualElement popup)
		{
			m_title = popup.Q<Label>("badge-popup-title");
			m_subtitle = popup.Q<Label>("badge-popup-sub");
			m_tiers = popup.Q<VisualElement>("badge-popup-tiers");
			m_total = popup.Q<Label>("badge-popup-total");
		}

		/// <summary>Fills the popup for <paramref name="badge"/> from the current progress.</summary>
		public void Bind(BadgeDefinition badge)
		{
			m_title.text = badge.SingleLineTitle;
			m_tiers.Clear();

			if (badge.Hidden)
			{
				m_subtitle.text = "Secret badge · " + badge.Description;
				m_tiers.style.display = DisplayStyle.None;
				m_total.style.display = DisplayStyle.None;
				return;
			}

			m_subtitle.text = badge.Description;
			m_tiers.style.display = DisplayStyle.Flex;

			var progress = badge.Progress();
			var earned = BadgeService.EarnedTiers(badge);

			for (var i = 0; i < badge.Tiers.Length; i++)
			{
				m_tiers.Add(CreateTierRow(badge, i, earned, progress));
			}

			// Every tier done: the progress has no next goal, so show the all-time figure instead.
			var allDone = earned >= badge.TierCount;
			m_total.style.display = allDone ? DisplayStyle.Flex : DisplayStyle.None;
			if (allDone) m_total.text = $"{(badge.IsBest ? "Best" : "Total")}: {badge.FormatValue(progress)} {badge.Unit}";
		}

		private static VisualElement CreateTierRow(BadgeDefinition badge, int index, int earned, long progress)
		{
			var tierNumber = badge.TierCount - index;
			var done = index < earned;
			var isNext = index == earned;
			var threshold = badge.Tiers[index];

			var row = new VisualElement { pickingMode = PickingMode.Ignore };
			row.AddToClassList("badge-tier");
			if (done) row.AddToClassList("badge-tier--done");
			else if (isNext) row.AddToClassList("badge-tier--next");
			else row.AddToClassList("badge-tier--locked");

			var medal = new VisualElement { pickingMode = PickingMode.Ignore };
			medal.AddToClassList("badge-tier__medal");
			medal.AddToClassList(tierNumber switch { 3 => "badge-tier__medal--bronze", 2 => "badge-tier__medal--silver", _ => "badge-tier__medal--gold" });
			row.Add(medal);

			var text = new Label($"TIER {tierNumber} · {badge.FormatValue(threshold)} {badge.Unit}") { pickingMode = PickingMode.Ignore };
			text.AddToClassList("badge-tier__text");
			row.Add(text);

			var state = new Label(done ? "DONE" : isNext ? $"{badge.FormatValue(System.Math.Min(progress, threshold))} / {badge.FormatValue(threshold)}" : string.Empty)
			{
				pickingMode = PickingMode.Ignore,
			};
			state.AddToClassList("badge-tier__state");
			row.Add(state);

			return row;
		}
	}
}
