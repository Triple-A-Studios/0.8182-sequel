using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Profile screen (mockup S08): portrait slot, display name card, six stat tiles, equipped plane / crash FX cards
	/// and the badge row. Plain class over the instance's root, like <see cref="SettingsScreen"/>. Everything except the
	/// display name is static placeholder data (<see cref="ProfilePlaceholderData"/>); the name is edited in place
	/// (pencil -> text field with dice re-roll and a tick, <see cref="UiProfileState.MAX_NAME_LENGTH"/> characters) and
	/// kept in <see cref="UiProfileState"/> for the session only.
	/// </summary>
	public class ProfileScreen
	{
		private const string k_BadgeClass = "badge";
		private const string k_BadgeLockedClass = "badge--locked";
		private const string k_BadgeSelectedClass = "is-selected";
		private const string k_DefaultCaption = "Tap a locked badge to see how to unlock it.";
		private const string k_EditHint = "Up to 16 characters";

		private readonly VisualElement m_root;
		private readonly Label m_nameLabel;
		private readonly TextField m_nameInput;
		private readonly LipButton m_editButton;
		private readonly LipButton m_diceButton;
		private readonly LipButton m_confirmButton;
		private readonly Label m_idLabel;
		private readonly Label m_badgeTitleCount;
		private readonly Label m_badgeCaption;
		private readonly ScrollView m_badgeScroll;
		private readonly VisualElement[] m_badgeTiles;

		private bool m_editing;
		private int m_selectedBadge = -1;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>The display name was changed and confirmed (the menu chip refreshes on return).</summary>
		public event Action NameChanged;

		public bool IsEditing => m_editing;

		public ProfileScreen(VisualElement root)
		{
			m_root = root;

			m_nameLabel = root.Q<Label>("profile-name-label");
			m_nameInput = root.Q<TextField>("profile-name-input");
			m_editButton = root.Q<LipButton>("profile-edit");
			m_diceButton = root.Q<LipButton>("profile-dice");
			m_confirmButton = root.Q<LipButton>("profile-confirm");
			m_idLabel = root.Q<Label>("profile-id");
			m_badgeTitleCount = root.Q<Label>("profile-badge-count");
			m_badgeCaption = root.Q<Label>("profile-badge-caption");
			m_badgeScroll = root.Q<ScrollView>("profile-badge-scroll");

			m_nameInput.maxLength = UiProfileState.MAX_NAME_LENGTH;

			BindStats();
			m_badgeTiles = BuildBadges();

			root.Q<LipButton>("profile-back").clicked += () => BackRequested?.Invoke();
			m_editButton.clicked += BeginEdit;
			m_diceButton.clicked += RollName;
			m_confirmButton.clicked += ConfirmEdit;

			// Trickle so Enter is seen before the text field's own handling of the key.
			m_nameInput.RegisterCallback<KeyDownEvent>(OnNameKeyDown, TrickleDown.TrickleDown);

			SetEditing(false);
			Refresh();
		}

		/// <summary>Resets the screen to its resting state (name shown, nothing selected). Call when the router shows it.</summary>
		public void Open()
		{
			CancelEdit();
			SelectBadge(-1);
			m_badgeScroll.scrollOffset = Vector2.zero;
			Refresh();
		}

		/// <summary>Drops an edit in progress. Call when the router hides the screen.</summary>
		public void Close()
		{
			CancelEdit();
		}

		/// <summary>Reloads the name from <see cref="UiProfileState"/>.</summary>
		public void Refresh()
		{
			m_nameLabel.text = UiProfileState.DisplayName;
		}

		/// <summary>Leaves edit mode without saving. False when nothing was being edited (so Escape can go back instead).</summary>
		public bool CancelEdit()
		{
			if (!m_editing) return false;

			SetEditing(false);
			return true;
		}

		public void BeginEdit()
		{
			if (m_editing) return;

			m_nameInput.SetValueWithoutNotify(UiProfileState.DisplayName);
			SetEditing(true);

			// The field has to be laid out (display flex) before it can take focus.
			m_root.schedule.Execute(() =>
			{
				m_nameInput.Focus();
				m_nameInput.SelectAll();
			});
		}

		private void ConfirmEdit()
		{
			if (!m_editing) return;

			var name = UiProfileState.Sanitize(m_nameInput.value);
			var changed = name.Length > 0 && name != UiProfileState.DisplayName;
			if (changed) UiProfileState.DisplayName = name;

			SetEditing(false);
			Refresh();
			if (changed) NameChanged?.Invoke();
		}

		// Re-roll fills the field; the name only counts once it is confirmed.
		private void RollName()
		{
			if (!m_editing) return;

			var rolled = UiProfileState.GenerateName();
			for (var attempt = 0; attempt < 5 && rolled == m_nameInput.value; attempt++) rolled = UiProfileState.GenerateName();

			m_nameInput.value = rolled;
		}

		private void OnNameKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;

			ConfirmEdit();
			evt.StopPropagation();
		}

		private void SetEditing(bool editing)
		{
			m_editing = editing;

			SetDisplay(m_nameLabel, !editing);
			SetDisplay(m_editButton, !editing);
			SetDisplay(m_nameInput, editing);
			SetDisplay(m_diceButton, editing);
			SetDisplay(m_confirmButton, editing);
			m_idLabel.text = editing ? k_EditHint : ProfilePlaceholderData.ID_LINE;
		}

		private void BindStats()
		{
			SetText("profile-id", ProfilePlaceholderData.ID_LINE);
			SetText("profile-coins", ProfilePlaceholderData.COINS);
			SetText("stat-high-score", ProfilePlaceholderData.HIGH_SCORE);
			SetText("stat-distance", ProfilePlaceholderData.DISTANCE_FLOWN);
			SetText("stat-buildings", ProfilePlaceholderData.BUILDINGS_DOWN);
			SetText("stat-combo", ProfilePlaceholderData.BEST_COMBO);
			SetText("stat-runs", ProfilePlaceholderData.RUNS_FLOWN);
			SetText("stat-perfect", ProfilePlaceholderData.PERFECT_CRASHES);
			SetText("profile-plane", ProfilePlaceholderData.EQUIPPED_PLANE);
			SetText("profile-fx", ProfilePlaceholderData.CRASH_FX);
		}

		private VisualElement[] BuildBadges()
		{
			var definitions = ProfilePlaceholderData.Badges;
			var tiles = new VisualElement[definitions.Length];
			var unlocked = 0;

			for (var i = 0; i < definitions.Length; i++)
			{
				var index = i;
				var definition = definitions[i];
				if (definition.Unlocked) unlocked++;

				var tile = new LipBox { variant = definition.Unlocked ? LipVariant.Primary : LipVariant.Secondary };
				tile.AddToClassList(k_BadgeClass);
				if (!definition.Unlocked) tile.AddToClassList(k_BadgeLockedClass);

				var label = new Label(definition.Unlocked ? definition.Title : "?") { pickingMode = PickingMode.Ignore };
				label.AddToClassList("badge__label");
				tile.Add(label);

				tile.AddManipulator(new PressedClassManipulator());
				tile.RegisterCallback<ClickEvent>(_ => SelectBadge(index));

				m_badgeScroll.Add(tile);
				tiles[i] = tile;
			}

			m_badgeTitleCount.text = $"{unlocked} / {definitions.Length}";
			m_badgeCaption.text = k_DefaultCaption;
			return tiles;
		}

		// Tapping a tile shows its condition in the caption line; tapping it again (or reopening) clears it.
		private void SelectBadge(int index)
		{
			if (index == m_selectedBadge) index = -1;

			for (var i = 0; i < m_badgeTiles.Length; i++) m_badgeTiles[i].EnableInClassList(k_BadgeSelectedClass, i == index);
			m_selectedBadge = index;

			if (index < 0)
			{
				m_badgeCaption.text = k_DefaultCaption;
				return;
			}

			var definition = ProfilePlaceholderData.Badges[index];
			m_badgeCaption.text = definition.Unlocked
				? $"{definition.SingleLineTitle} · {definition.Condition}"
				: $"LOCKED · {definition.Condition}";
		}

		private void SetText(string labelName, string text)
		{
			m_root.Q<Label>(labelName).text = text;
		}

		private static void SetDisplay(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
