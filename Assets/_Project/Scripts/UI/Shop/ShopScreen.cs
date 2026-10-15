using System;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Hangar shop (mockup S09): coin chip, PLANES / CRASH FX / BUILDING SKINS tabs and the four plane cards. Plain
	/// class over the instance's root, like <see cref="SettingsScreen"/>. Everything is static placeholder data
	/// (<see cref="ShopPlaceholderData"/>); the two Beta tabs show the hazard-tape banner. BUY and EQUIPPED raise
	/// <see cref="ActionRequested"/> (the owner shows the arrives-later toast), the other two states are inert.
	/// </summary>
	public class ShopScreen
	{
		private const int k_PlanesTab = 0;

		private readonly TabBar m_tabs;
		private readonly VisualElement m_grid;
		private readonly VisualElement m_beta;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>BUY or EQUIPPED was tapped on a card (nothing can be bought yet).</summary>
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

			var planes = ShopPlaceholderData.Planes;
			for (var i = 0; i < planes.Length; i++)
			{
				m_grid.Add(BuildCard(planes[i], i == 0));
			}
		}

		private VisualElement BuildCard(ShopPlaneCard data, bool first)
		{
			var locked = data.State == ShopCardState.Locked;

			var card = new LipBox();
			card.AddToClassList("card");
			card.AddToClassList("shop-card");
			card.AddToClassList($"shop-card--{data.State.ToString().ToLowerInvariant()}");
			if (first) card.AddToClassList("shop-card--first");

			// Transparent slot: the 3D half renders the plane into it. Locked cards show a "?" instead.
			var thumb = new VisualElement { pickingMode = PickingMode.Ignore };
			thumb.AddToClassList("shop-thumb");
			if (locked)
			{
				thumb.AddToClassList("shop-thumb--locked");
				thumb.Add(MakeLabel("?", "shop-thumb__mark"));
			}

			card.Add(thumb);
			card.Add(MakeLabel(data.Name, locked ? "shop-name shop-name--locked" : "shop-name"));
			card.Add(MakeLabel(data.Description, "shop-desc"));
			card.Add(BuildAction(data));
			return card;
		}

		private VisualElement BuildAction(ShopPlaneCard data)
		{
			switch (data.State)
			{
				case ShopCardState.Buy:
				{
					var button = new LipButton { variant = LipVariant.Primary, lipSize = LipSize.Small };
					button.AddToClassList("shop-action");
					button.Add(MakeLabel($"BUY · {data.Detail}", "shop-action__label"));
					button.Add(MakeCoin());
					button.clicked += () => ActionRequested?.Invoke();
					return button;
				}
				case ShopCardState.Equipped:
				{
					var pill = new VisualElement();
					pill.AddToClassList("shop-pill");
					pill.AddToClassList("shop-pill--equipped");
					pill.Add(MakeLabel("EQUIPPED", "shop-pill__label shop-pill__label--equipped"));
					pill.RegisterCallback<ClickEvent>(_ => ActionRequested?.Invoke());
					return pill;
				}
				case ShopCardState.NotEnough:
				{
					var pill = new VisualElement();
					pill.AddToClassList("shop-pill");
					pill.AddToClassList("shop-pill--short");
					pill.Add(MakeLabel(data.Detail, "shop-pill__label"));
					pill.Add(MakeCoin());
					pill.Add(MakeLabel("· NOT ENOUGH", "shop-pill__label shop-pill__label--tail"));
					return pill;
				}
				default:
				{
					var pill = new VisualElement();
					pill.AddToClassList("shop-pill");
					pill.AddToClassList("shop-pill--locked");
					pill.Add(MakeLabel(data.Detail, "shop-pill__label shop-pill__label--locked"));
					return pill;
				}
			}
		}

		private static VisualElement MakeCoin()
		{
			var coin = new VisualElement { pickingMode = PickingMode.Ignore };
			coin.AddToClassList("shop-coin");
			return coin;
		}

		private static Label MakeLabel(string text, string classes)
		{
			var label = new Label(text) { pickingMode = PickingMode.Ignore };
			foreach (var c in classes.Split(' ')) label.AddToClassList(c);
			return label;
		}
	}
}
