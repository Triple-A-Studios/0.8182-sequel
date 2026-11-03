using System;
using Opoint8182.Badges;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Achievements (Player profile & run stats Pass 6d): a vertically scrolling list of every counted badge plus the earned hidden ones
	/// (hidden first, with the gold outline), reached from the Profile's BADGES header. A row is the badge name as the heading, its description
	/// under it and one pip per tier on the right (bronze, silver, gold when earned, hollow otherwise); a badge with no tier is dimmed. Tapping a
	/// row raises <see cref="BadgeRequested"/> and the owner opens the tier popup (the popup layer belongs to the menu). Plain class over the
	/// instance's root, like <see cref="ProfileScreen"/>.
	/// </summary>
	public class AchievementsScreen
	{
		private readonly Label m_count;
		private readonly ScrollView m_scroll;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>A row was tapped; the owner opens that badge's popup.</summary>
		public event Action<string> BadgeRequested;

		public AchievementsScreen(VisualElement root)
		{
			m_count = root.Q<Label>("achievements-count");
			m_scroll = root.Q<ScrollView>("achievements-scroll");

			root.Q<LipButton>("achievements-back").clicked += () => BackRequested?.Invoke();

			BuildRows();
		}

		/// <summary>Rebuilds the list from the current badges and scrolls to the top. Call when the router shows the screen.</summary>
		public void Open()
		{
			// Badges for stats that existed before the badges are picked up silently, as on the Profile.
			BadgeService.Evaluate();
			BadgeService.TakeNewUnlocks();

			BuildRows();
			m_scroll.scrollOffset = UnityEngine.Vector2.zero;
		}

		/// <summary>Called when the router hides the screen. Nothing to drop yet.</summary>
		public void Close()
		{
		}

		private void BuildRows()
		{
			m_scroll.Clear();
			foreach (var badge in BadgeCatalog.VisibleOrdered()) m_scroll.Add(CreateRow(badge));

			m_count.text = $"{BadgeService.CountedEarned()} / {BadgeCatalog.CountedTotal}";
		}

		private VisualElement CreateRow(BadgeDefinition badge)
		{
			var earned = BadgeService.EarnedTiers(badge);

			var row = new VisualElement { pickingMode = PickingMode.Position };
			row.AddToClassList("ach-row");
			if (earned == 0) row.AddToClassList("ach-row--locked");
			if (badge.Hidden) row.AddToClassList("ach-row--hidden");

			var text = new VisualElement { pickingMode = PickingMode.Ignore };
			text.AddToClassList("ach-row__text");

			var name = new Label(badge.SingleLineTitle) { pickingMode = PickingMode.Ignore };
			name.AddToClassList("ach-row__name");
			text.Add(name);

			var description = new Label(badge.Description) { pickingMode = PickingMode.Ignore };
			description.AddToClassList("ach-row__desc");
			text.Add(description);

			row.Add(text);

			// An earned hidden badge the player has not opened yet carries the "new" pip until they tap the row.
			VisualElement newPip = null;
			if (BadgeService.IsUnseen(badge))
			{
				newPip = new VisualElement { pickingMode = PickingMode.Ignore };
				newPip.AddToClassList("ach-row__new");
				row.Add(newPip);
			}

			var pips = new VisualElement { pickingMode = PickingMode.Ignore };
			pips.AddToClassList("ach-row__pips");
			for (var i = 0; i < badge.TierCount; i++)
			{
				var pip = new VisualElement { pickingMode = PickingMode.Ignore };
				pip.AddToClassList("ach-pip");

				// Pip i is tier (TierCount - i): the first pip is the easiest tier, bronze, and the last one gold.
				if (i < earned) pip.AddToClassList((badge.TierCount - i) switch { 3 => "ach-pip--bronze", 2 => "ach-pip--silver", _ => "ach-pip--gold" });
				pips.Add(pip);
			}

			row.Add(pips);

			row.AddManipulator(new PressedClassManipulator());
			var id = badge.Id;
			row.RegisterCallback<ClickEvent>(_ =>
			{
				if (newPip != null)
				{
					BadgeService.MarkSeen(badge);
					newPip.RemoveFromHierarchy();
					newPip = null;
				}

				BadgeRequested?.Invoke(id);
			});

			return row;
		}
	}
}
