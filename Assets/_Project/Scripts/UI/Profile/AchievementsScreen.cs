using System;
using System.Collections.Generic;
using Opoint8182.Badges;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Achievements (Player profile & run stats Pass 6d-6e): a vertically scrolling list of every counted badge plus the earned hidden ones
	/// (hidden first, with the gold outline), reached from the Profile's BADGES header. A row is the badge name as the heading, its description
	/// under it and one pip per tier on the right (bronze, silver, gold when earned, hollow otherwise); a badge with no tier is dimmed. Tapping a
	/// row raises <see cref="BadgeRequested"/> and the owner opens the tier popup (the popup layer belongs to the menu).
	/// <para>PICK (shown once at least one counted badge is earned) turns the list into a checklist for the Profile's badge row: earned counted
	/// rows get a checkbox and the whole row toggles it, up to <see cref="BadgePicks.MAX"/>, after which the unticked rows grey out. DONE saves
	/// the ticks through <see cref="BadgePicks"/>; back or Escape (<see cref="CancelPick"/>) drops them. Plain class over the instance's root,
	/// like <see cref="ProfileScreen"/>.</para>
	/// </summary>
	public class AchievementsScreen
	{
		private sealed class RowRef
		{
			public BadgeDefinition Badge;
			public VisualElement Row;
			public VisualElement Check;
		}

		private readonly Label m_title;
		private readonly Label m_count;
		private readonly ScrollView m_scroll;
		private readonly LipButton m_pickButton;
		private readonly List<RowRef> m_rows = new();
		private readonly HashSet<string> m_ticked = new();

		private bool m_picking;

		/// <summary>The back chevron was tapped (the owner pops the router). Not raised while picking: that first leaves pick mode.</summary>
		public event Action BackRequested;

		/// <summary>A row was tapped outside pick mode; the owner opens that badge's popup.</summary>
		public event Action<string> BadgeRequested;

		public bool IsPicking => m_picking;

		public AchievementsScreen(VisualElement root)
		{
			m_title = root.Q<Label>("achievements-title");
			m_count = root.Q<Label>("achievements-count");
			m_scroll = root.Q<ScrollView>("achievements-scroll");
			m_pickButton = root.Q<LipButton>("achievements-pick");

			root.Q<LipButton>("achievements-back").clicked += OnBackClicked;
			m_pickButton.clicked += OnPickClicked;

			BuildRows();
			UpdateHeader();
		}

		/// <summary>Rebuilds the list from the current badges and scrolls to the top. Call when the router shows the screen.</summary>
		public void Open()
		{
			// Badges for stats that existed before the badges are picked up silently, as on the Profile.
			BadgeService.Evaluate();
			BadgeService.TakeNewUnlocks();

			m_picking = false;
			BuildRows();
			UpdateHeader();
			m_scroll.scrollOffset = Vector2.zero;
		}

		/// <summary>Called when the router hides the screen: a pick in progress is dropped.</summary>
		public void Close()
		{
			m_picking = false;
		}

		/// <summary>Leaves pick mode without saving. False when not picking (so back / Escape can leave the screen instead).</summary>
		public bool CancelPick()
		{
			if (!m_picking) return false;

			SetPicking(false);
			return true;
		}

		private void OnBackClicked()
		{
			if (CancelPick()) return;

			BackRequested?.Invoke();
		}

		private void OnPickClicked()
		{
			if (m_picking)
			{
				BadgePicks.Save(m_ticked);
				SetPicking(false);
				return;
			}

			if (!BadgePicks.AnythingToPick()) return;

			m_ticked.Clear();
			foreach (var badge in BadgePicks.Current()) m_ticked.Add(badge.Id);
			SetPicking(true);
		}

		// Rebuilds the rows for the new mode and keeps the list where it was.
		private void SetPicking(bool picking)
		{
			var offset = m_scroll.scrollOffset;
			m_picking = picking;
			BuildRows();
			UpdateHeader();
			m_scroll.schedule.Execute(() => m_scroll.scrollOffset = offset);
		}

		private void BuildRows()
		{
			m_scroll.Clear();
			m_rows.Clear();

			foreach (var badge in BadgeCatalog.VisibleOrdered())
			{
				var rowRef = CreateRow(badge);
				m_rows.Add(rowRef);
				m_scroll.Add(rowRef.Row);
			}

			RefreshPickVisuals();
		}

		// Title, counter and the PICK / DONE button for the current mode. PICK only shows when there is something to pick.
		private void UpdateHeader()
		{
			m_title.text = m_picking ? "PICK BADGES" : "ACHIEVEMENTS";
			m_count.text = m_picking ? PickCountText() : $"{BadgeService.CountedEarned()} / {BadgeCatalog.CountedTotal}";

			m_pickButton.text = m_picking ? "DONE" : "PICK";
			m_pickButton.variant = m_picking ? LipVariant.Primary : LipVariant.Secondary;
			m_pickButton.style.display = m_picking || BadgePicks.AnythingToPick() ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private string PickCountText() => $"{m_ticked.Count} / {BadgePicks.MAX} picked";

		// Ticks, the greyed-out rows at the limit and the counter.
		private void RefreshPickVisuals()
		{
			if (!m_picking) return;

			var full = m_ticked.Count >= BadgePicks.MAX;
			foreach (var rowRef in m_rows)
			{
				if (rowRef.Check == null) continue;

				var on = m_ticked.Contains(rowRef.Badge.Id);
				rowRef.Check.EnableInClassList("ach-check--on", on);
				rowRef.Row.EnableInClassList("ach-row--off", full && !on);
			}

			m_count.text = PickCountText();
		}

		private void Toggle(BadgeDefinition badge)
		{
			if (!BadgePicks.IsPickable(badge)) return;

			if (!m_ticked.Remove(badge.Id))
			{
				if (m_ticked.Count >= BadgePicks.MAX) return;
				m_ticked.Add(badge.Id);
			}

			RefreshPickVisuals();
		}

		private RowRef CreateRow(BadgeDefinition badge)
		{
			var earned = BadgeService.EarnedTiers(badge);
			var rowRef = new RowRef { Badge = badge };

			var row = new VisualElement { pickingMode = PickingMode.Position };
			row.AddToClassList("ach-row");
			if (earned == 0) row.AddToClassList("ach-row--locked");
			if (badge.Hidden) row.AddToClassList("ach-row--hidden");
			rowRef.Row = row;

			// Pick mode: earned counted badges get a checkbox (locked and hidden rows have none and cannot be ticked).
			if (m_picking && BadgePicks.IsPickable(badge))
			{
				var check = new VisualElement { pickingMode = PickingMode.Ignore };
				check.AddToClassList("ach-check");
				var mark = new VisualElement { pickingMode = PickingMode.Ignore };
				mark.AddToClassList("ach-check__mark");
				check.Add(mark);
				row.Add(check);
				rowRef.Check = check;
			}

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
				// In pick mode the whole row is the checkbox and there is no popup.
				if (m_picking)
				{
					Toggle(badge);
					return;
				}

				if (newPip != null)
				{
					BadgeService.MarkSeen(badge);
					newPip.RemoveFromHierarchy();
					newPip = null;
				}

				BadgeRequested?.Invoke(id);
			});

			return rowRef;
		}
	}
}
