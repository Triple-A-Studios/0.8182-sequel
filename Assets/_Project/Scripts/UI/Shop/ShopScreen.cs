using System;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Hangar shop (mockup S09): coin chip, PLANES / CRASH FX / BUILDING SKINS tabs and the plane cards. Plain
	/// class over the instance's root, like <see cref="SettingsScreen"/>. The first card is the equipped plane, the others are
	/// teasers; the copy and the coins are static (<see cref="ShopPlaceholderData"/>); the two Beta tabs show the hazard-tape
	/// banner. EQUIPPED raises <see cref="ActionRequested"/> (the owner shows the arrives-later toast), teasers are inert.
	/// </summary>
	public class ShopScreen
	{
		private const int k_PlanesTab = 0;

		private readonly TabBar m_tabs;
		private readonly VisualElement m_grid;
		private readonly VisualElement m_beta;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>EQUIPPED was tapped on the card (the hangar cannot change anything yet).</summary>
		public event Action ActionRequested;

		public int SelectedTab => m_tabs.selectedIndex;

		public ShopScreen(VisualElement root)
		{
			m_tabs = root.Q<TabBar>("shop-tabs");
			m_grid = root.Q<VisualElement>("shop-grid");
			m_beta = root.Q<VisualElement>("shop-beta");

			root.Q<Label>("shop-coins").text = ProfilePlaceholderData.COINS;
			root.Q<Label>("shop-footnote").text = ShopPlaceholderData.FOOTNOTE;
			root.Q<Label>("shop-beta-title").text = ShopPlaceholderData.BETA_TITLE;
			root.Q<Label>("shop-beta-body").text = ShopPlaceholderData.BETA_BODY;

			BuildCards();

			root.Q<LipButton>("shop-back").clicked += () => BackRequested?.Invoke();
			m_tabs.changed += _ => RefreshTab();

			RefreshTab();
		}

		/// <summary>Resets to the PLANES tab. Call when the router shows the screen.</summary>
		public void Open()
		{
			m_tabs.SetSelectedIndexWithoutNotify(k_PlanesTab);
			BuildCards();
			RefreshTab();
		}

		public void SelectTab(int index)
		{
			m_tabs.selectedIndex = index;
		}

		private void RefreshTab()
		{
			var planes = m_tabs.selectedIndex == k_PlanesTab;
			m_grid.style.display = planes ? DisplayStyle.Flex : DisplayStyle.None;
			m_beta.style.display = planes ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void BuildCards()
		{
			m_grid.Clear();

			var plane = PlaneSelection.Equipped;
			m_grid.Add(BuildEquippedCard(plane));
			for (var i = 0; i < ShopPlaceholderData.TEASER_COUNT; i++) m_grid.Add(BuildTeaserCard(plane));
		}

		private VisualElement BuildEquippedCard(PlaneDefinition plane)
		{
			var thumb = MakeThumb(plane, "shop-thumb__picture");

			var pill = new VisualElement();
			pill.AddToClassList("shop-pill");
			pill.AddToClassList("shop-pill--equipped");
			pill.Add(MakeLabel("EQUIPPED", "shop-pill__label shop-pill__label--equipped"));
			pill.RegisterCallback<ClickEvent>(_ => ActionRequested?.Invoke());

			var card = MakeCard("shop-card--equipped", thumb, plane != null ? plane.DisplayName : string.Empty,
				plane != null ? plane.Tagline : string.Empty, pill);
			card.AddToClassList("shop-card--first");
			return card;
		}

		// A dark silhouette with a question mark: no names or prices for planes that do not exist yet.
		private static VisualElement BuildTeaserCard(PlaneDefinition silhouette)
		{
			var thumb = MakeThumb(silhouette, "shop-thumb__silhouette");
			thumb.AddToClassList("shop-thumb--teaser");
			thumb.Add(MakeLabel("?", "shop-thumb__mark"));

			var pill = new VisualElement();
			pill.AddToClassList("shop-pill");
			pill.AddToClassList("shop-pill--teaser");
			pill.Add(MakeLabel(ShopPlaceholderData.TEASER_PILL, "shop-pill__label shop-pill__label--teaser"));

			return MakeCard("shop-card--teaser", thumb, ShopPlaceholderData.TEASER_NAME, ShopPlaceholderData.TEASER_LINE, pill);
		}

		private static VisualElement MakeThumb(PlaneDefinition plane, string pictureClass)
		{
			var thumb = new VisualElement { pickingMode = PickingMode.Ignore };
			thumb.AddToClassList("shop-thumb");

			var picture = new VisualElement { pickingMode = PickingMode.Ignore };
			picture.AddToClassList(pictureClass);
			if (plane != null && plane.Thumbnail != null) picture.style.backgroundImage = new StyleBackground(plane.Thumbnail);
			thumb.Add(picture);
			return thumb;
		}

		private static LipBox MakeCard(string stateClass, VisualElement thumb, string name, string line, VisualElement action)
		{
			var card = new LipBox();
			card.AddToClassList("card");
			card.AddToClassList("shop-card");
			card.AddToClassList(stateClass);

			var teaser = stateClass == "shop-card--teaser";
			card.Add(thumb);
			card.Add(MakeLabel(name, teaser ? "shop-name shop-name--teaser" : "shop-name"));
			card.Add(MakeLabel(line, "shop-desc"));
			card.Add(action);
			return card;
		}

		private static Label MakeLabel(string text, string classes)
		{
			var label = new Label(text) { pickingMode = PickingMode.Ignore };
			foreach (var c in classes.Split(' ')) label.AddToClassList(c);
			return label;
		}
	}
}
