using System;
using System.Collections.Generic;
using Opoint8182.Badges;
using Opoint8182.Stats;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Profile screen (mockup S08): portrait slot, display name card, six stat tiles, equipped plane / crash FX cards
	/// and the badge row. Plain class over the instance's root, like <see cref="SettingsScreen"/>. The six stat tiles are the
	/// player's real lifetime stats (<see cref="PlayerStats"/>); the rest except the display name is static placeholder data
	/// (<see cref="ProfilePlaceholderData"/>); the name is edited in place
	/// (pencil -> text field with dice re-roll and a tick, <see cref="UiProfileState.MAX_NAME_LENGTH"/> characters) and
	/// saved (and sent to the leaderboards) through <see cref="UiProfileState"/>; <see cref="DisplayNameRules"/> says what a name may be.
	/// </summary>
	public class ProfileScreen
	{
		private const string k_BadgeClass = "badge";
		private const string k_BadgeLockedClass = "badge--locked";
		private const string k_DefaultCaption = "Tap a badge to see its tiers.";
		private const long k_GlowPulseMs = 900;
		private const string k_EditHint = "3-16 letters, numbers, _ or -";
		private const string k_EditHintWeb = "Tap the name to type · 3-16 letters, numbers, _ or -";
		private const string k_TooShortHint = "Use at least 3 letters or numbers";
		private const string k_NotAllowedHint = "That name isn't available, try another";
		private const float k_KeyboardMargin = 24f;
		private const long k_KeyboardPollMs = 100;
		private const float k_StatValueSize = 72f;
		private const float k_StatValueMinSize = 44f;
		private static readonly string[] m_S_StatValueNames = { "stat-high-score", "stat-distance", "stat-buildings", "stat-combo", "stat-runs", "stat-perfect" };

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
		private readonly List<VisualElement> m_hiddenBadgeTiles = new();
		private IVisualElementScheduledItem m_glowPulse;
		private bool m_glowOn;
		private readonly VisualElement m_nameField;
		private IVisualElementScheduledItem m_keyboardPoll;
		private float m_keyboardShift;

		private bool m_editing;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>The display name was changed and confirmed (the menu chip refreshes on return).</summary>
		public event Action NameChanged;

		/// <summary>A badge tile was tapped; the owner opens its popup (the popup layer belongs to the menu).</summary>
		public event Action<string> BadgeRequested;

		/// <summary>SHARE was tapped: the owner shares a render of this screen.</summary>
		public event Action ShareRequested;

		public bool IsEditing => m_editing;

		/// <summary>Height in screen pixels of the phone keyboard covering the bottom of the screen, 0 when there is none. A test can swap it.</summary>
		public static System.Func<float> KeyboardHeightProvider { get; set; } = DefaultKeyboardHeight;

		private static float DefaultKeyboardHeight()
		{
			return TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
		}

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
			m_nameField = root.Q<VisualElement>(className: "profile-name-field");

			m_nameInput.maxLength = UiProfileState.MAX_NAME_LENGTH;

			BindStats();
			FitStatValues();
			BuildBadges();

			root.Q<LipButton>("profile-back").clicked += () => BackRequested?.Invoke();
			root.Q<LipButton>("profile-share").clicked += () => ShareRequested?.Invoke();
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

			// Badges for stats that existed before the badges (or an unlock the Game Over toast already announced) are picked up silently.
			BadgeService.Evaluate();
			BadgeService.TakeNewUnlocks();
			BuildBadges();
			m_badgeScroll.scrollOffset = Vector2.zero;
			Refresh();
		}

		/// <summary>
		/// For the share card: drops the buttons and the hint lines (back, SHARE, rename, the dev caption in the portrait slot, the
		/// "tap a badge" line and the badge row's scroll bar), leaving the profile as an image.
		/// </summary>
		public void PrepareForShare()
		{
			SetDisplay(m_root.Q<VisualElement>("profile-back"), false);
			SetDisplay(m_root.Q<VisualElement>("profile-share"), false);
			SetDisplay(m_editButton, false);
			SetDisplay(m_root.Q<VisualElement>(className: "profile-portrait__caption"), false);
			SetDisplay(m_badgeCaption, false);
			m_badgeScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
		}

		/// <summary>Drops an edit in progress. Call when the router hides the screen.</summary>
		public void Close()
		{
			CancelEdit();
			m_glowPulse?.Pause();
			m_glowPulse = null;
		}

		/// <summary>Reloads the name from <see cref="UiProfileState"/> and the stats from <see cref="PlayerStats"/>, and fits the stat values to their tiles again.</summary>
		public void Refresh()
		{
			PlayerNameText.Apply(m_nameLabel, UiProfileState.DisplayName, UiProfileState.NameTag);
			BindLifetimeStats();
			foreach (var name in m_S_StatValueNames) FitStatValue(m_root.Q<Label>(name));
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

			// Nothing usable typed (or the same name): leave the edit and keep the old name. Too short or on the blocklist: stay in the edit and say so.
			var name = DisplayNameRules.Sanitize(m_nameInput.value);
			if (name.Length > 0 && name != UiProfileState.DisplayName)
			{
				var check = DisplayNameRules.Check(name);
				if (check != NameCheck.Ok)
				{
					m_idLabel.text = check == NameCheck.TooShort ? k_TooShortHint : k_NotAllowedHint;
					return;
				}

				UiProfileState.DisplayName = name;
				var sync = UiProfileState.SyncToServiceAsync();
				SetEditing(false);
				Refresh();
				NameChanged?.Invoke();
				RefreshWhenSynced(sync);
				return;
			}

			SetEditing(false);
			Refresh();
		}

		// The service hands out the name's "#1234" tag when it accepts the name: show it once it has.
		private async void RefreshWhenSynced(System.Threading.Tasks.Task sync)
		{
			await sync;
			if (!m_editing) PlayerNameText.Apply(m_nameLabel, UiProfileState.DisplayName, UiProfileState.NameTag);
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
			m_idLabel.text = editing ? EditHint() : ProfilePlaceholderData.ID_LINE;

			if (editing)
			{
				m_keyboardPoll ??= m_root.schedule.Execute(AvoidKeyboard).Every(k_KeyboardPollMs);
			}
			else
			{
				m_keyboardPoll?.Pause();
				m_keyboardPoll = null;
				SetKeyboardShift(0f);
			}
		}

		// A browser only raises the phone keyboard for a tap on the field itself, so on the web the hint says so.
		private static string EditHint()
		{
			return Application.platform == RuntimePlatform.WebGLPlayer ? k_EditHintWeb : k_EditHint;
		}

		// The phone keyboard covers the bottom of the screen and UI Toolkit does not move anything out of its way: while
		// renaming, slide the whole screen up until the name field sits above the keyboard (Android; the browser pans the
		// page itself on the web, where the keyboard area is not reported).
		private void AvoidKeyboard()
		{
			if (!m_editing || m_root.panel == null) return;

			var shift = 0f;
			var keyboardPixels = KeyboardHeightProvider();
			if (keyboardPixels > 0f && Screen.height > 0)
			{
				var panelHeight = m_root.panel.visualTree.layout.height;
				var keyboardTop = panelHeight - keyboardPixels * panelHeight / Screen.height;
				var fieldBottom = m_nameField.worldBound.yMax + m_keyboardShift; // where it sits without the shift
				shift = Mathf.Max(0f, fieldBottom + k_KeyboardMargin - keyboardTop);
			}

			SetKeyboardShift(shift);
		}

		private void SetKeyboardShift(float shift)
		{
			if (Mathf.Approximately(shift, m_keyboardShift)) return;

			m_keyboardShift = shift;
			m_root.style.translate = new Translate(new Length(0f), new Length(-shift));
		}

		private void BindStats()
		{
			SetText("profile-id", ProfilePlaceholderData.ID_LINE);
			SetText("profile-coins", ProfilePlaceholderData.COINS);
			BindLifetimeStats();
			SetText("profile-plane", ProfilePlaceholderData.EQUIPPED_PLANE);
			SetText("profile-fx", ProfilePlaceholderData.CRASH_FX);
		}

		// The six tiles are the player's lifetime stats; HIGH SCORE is the same best Game Over compares a run with.
		private void BindLifetimeStats()
		{
			SetText("stat-high-score", RunStatsFormat.Count(PlayerBest.Current));
			SetText("stat-distance", RunStatsFormat.TotalDistance(PlayerStats.DistanceMeters));
			SetText("stat-buildings", RunStatsFormat.Count(PlayerStats.BuildingsDestroyed));
			SetText("stat-combo", RunStatsFormat.Count(PlayerStats.BestCombo));
			SetText("stat-runs", RunStatsFormat.Count(PlayerStats.RunsFlown));
			SetText("stat-perfect", RunStatsFormat.Count(PlayerStats.PerfectCrashes));
		}

		// A stat value is one big number in a tile whose width follows the panel's aspect; on a narrow panel (16:10 is the default WebGL canvas) a
		// long number would clip, so its font shrinks to fit (never below k_StatValueMinSize). Re-fits whenever the tile's width changes.
		private void FitStatValues()
		{
			foreach (var name in m_S_StatValueNames)
			{
				var label = m_root.Q<Label>(name);
				label.RegisterCallback<GeometryChangedEvent>(_ => FitStatValue(label));
			}
		}

		private static void FitStatValue(Label label)
		{
			var box = label.contentRect.width;
			if (box <= 1f || string.IsNullOrEmpty(label.text)) return;

			var current = label.resolvedStyle.fontSize;
			var measured = label.MeasureTextSize(label.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
			var natural = measured * k_StatValueSize / Mathf.Max(current, 1f);
			var target = Mathf.Clamp(k_StatValueSize * box / Mathf.Max(natural, 1f), k_StatValueMinSize, k_StatValueSize);
			if (Mathf.Abs(target - current) > 0.5f) label.style.fontSize = target;
		}

		// Every visible badge as a tile: the best tier earned colours it (bronze, silver, gold), pips count the tiers, an unearned counted badge is
		// a "?", and an earned hidden badge comes first with a pulsing gold outline. The header counts the counted badges that have a tier.
		private void BuildBadges()
		{
			m_badgeScroll.Clear();
			m_hiddenBadgeTiles.Clear();

			foreach (var badge in BadgeCatalog.VisibleOrdered())
			{
				var tile = CreateBadgeTile(badge);
				if (badge.Hidden) m_hiddenBadgeTiles.Add(tile);
				m_badgeScroll.Add(tile);
			}

			m_badgeTitleCount.text = $"{BadgeService.CountedEarned()} / {BadgeCatalog.CountedTotal}";
			m_badgeCaption.text = k_DefaultCaption;
			UpdateGlowPulse();
		}

		private VisualElement CreateBadgeTile(BadgeDefinition badge)
		{
			var earned = BadgeService.EarnedTiers(badge);
			var locked = earned == 0;

			var tile = new LipBox { variant = locked ? LipVariant.Secondary : LipVariant.Primary };
			tile.AddToClassList(k_BadgeClass);

			if (locked) tile.AddToClassList(k_BadgeLockedClass);
			else if (badge.Hidden) tile.AddToClassList("badge--hidden");
			else tile.AddToClassList(BadgeService.HighestTier(badge) switch { 3 => "badge--bronze", 2 => "badge--silver", _ => "badge--gold" });

			var label = new Label(locked ? "?" : badge.Title) { pickingMode = PickingMode.Ignore };
			label.AddToClassList("badge__label");
			tile.Add(label);

			if (!locked && !badge.Hidden)
			{
				tile.AddToClassList("badge--pips");
				var pips = new VisualElement { pickingMode = PickingMode.Ignore };
				pips.AddToClassList("badge__pips");
				for (var i = 0; i < badge.TierCount; i++)
				{
					var pip = new VisualElement { pickingMode = PickingMode.Ignore };
					pip.AddToClassList("badge__pip");
					if (i < earned) pip.AddToClassList("badge__pip--on");
					pips.Add(pip);
				}

				tile.Add(pips);
			}

			// A hidden badge the player has not opened yet carries a pip until they tap it.
			VisualElement newPip = null;
			if (BadgeService.IsUnseen(badge))
			{
				newPip = new VisualElement { pickingMode = PickingMode.Ignore };
				newPip.AddToClassList("badge__new");
				tile.Add(newPip);
			}

			tile.AddManipulator(new PressedClassManipulator());
			var id = badge.Id;
			tile.RegisterCallback<ClickEvent>(_ =>
			{
				if (newPip != null)
				{
					BadgeService.MarkSeen(badge);
					newPip.RemoveFromHierarchy();
					newPip = null;
				}

				BadgeRequested?.Invoke(id);
			});
			return tile;
		}

		// The hidden badge's outline alternates between two golds (a USS transition does the fade) while the screen is open.
		private void UpdateGlowPulse()
		{
			if (m_hiddenBadgeTiles.Count == 0)
			{
				m_glowPulse?.Pause();
				m_glowPulse = null;
				return;
			}

			m_glowPulse ??= m_root.schedule.Execute(() =>
			{
				m_glowOn = !m_glowOn;
				foreach (var tile in m_hiddenBadgeTiles) tile.EnableInClassList("is-glow", m_glowOn);
			}).Every(k_GlowPulseMs);
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
