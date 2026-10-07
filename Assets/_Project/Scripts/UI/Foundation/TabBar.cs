using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	public enum TabAccent
	{
		Amber,
		Coral,
	}

	/// <summary>
	/// Row of mutually exclusive tabs (mode tabs, leaderboard tabs, shop tabs, the settings L/R switch).
	/// Choices come from a comma-separated UXML attribute so a screen can author them in markup.
	/// <see cref="changed"/> fires when the user taps a different tab or when <see cref="selectedIndex"/> is set in code.
	/// </summary>
	[UxmlElement]
	public partial class TabBar : VisualElement
	{
		public const string UssClassName = "tabbar";
		public const string TabUssClassName = "tabbar__tab";
		public const string TabLabelUssClassName = "tabbar__label";
		public const string ActiveUssClassName = "tabbar__tab--active";

		private readonly List<VisualElement> m_tabs = new();
		private string m_choices = string.Empty;
		private int m_selectedIndex;
		private TabAccent m_accent = TabAccent.Amber;

		public event Action<int> changed;

		public int TabCount => m_tabs.Count;

		[UxmlAttribute]
		public string choices
		{
			get => m_choices;
			set
			{
				m_choices = value ?? string.Empty;
				Rebuild();
			}
		}

		[UxmlAttribute]
		public int selectedIndex
		{
			get => m_selectedIndex;
			set => SetSelectedIndex(value, true);
		}

		[UxmlAttribute]
		public TabAccent accent
		{
			get => m_accent;
			set
			{
				RemoveFromClassList(AccentClass(m_accent));
				m_accent = value;
				AddToClassList(AccentClass(m_accent));
			}
		}

		public TabBar()
		{
			AddToClassList(UssClassName);
			AddToClassList(AccentClass(m_accent));
		}

		/// <summary>Selects a tab without raising <see cref="changed"/> (for state restored from elsewhere).</summary>
		public void SetSelectedIndexWithoutNotify(int index)
		{
			SetSelectedIndex(index, false);
		}

		private static string AccentClass(TabAccent accent) => $"{UssClassName}--{accent.ToString().ToLowerInvariant()}";

		private void Rebuild()
		{
			Clear();
			m_tabs.Clear();

			if (m_choices.Length == 0) return;

			var names = m_choices.Split(',');
			for (var i = 0; i < names.Length; i++)
			{
				var index = i;
				var tab = new VisualElement();
				tab.AddToClassList(TabUssClassName);

				var label = new Label(names[i].Trim()) { pickingMode = PickingMode.Ignore };
				label.AddToClassList(TabLabelUssClassName);
				tab.Add(label);

				tab.RegisterCallback<ClickEvent>(_ => SetSelectedIndex(index, true));
				tab.AddManipulator(new PressedClassManipulator());

				Add(tab);
				m_tabs.Add(tab);
			}

			m_selectedIndex = Math.Clamp(m_selectedIndex, 0, m_tabs.Count - 1);
			Refresh();
		}

		private void SetSelectedIndex(int index, bool notify)
		{
			if (m_tabs.Count == 0)
			{
				m_selectedIndex = index;
				return;
			}

			index = Math.Clamp(index, 0, m_tabs.Count - 1);
			var didChange = index != m_selectedIndex;
			m_selectedIndex = index;
			Refresh();

			if (notify && didChange) changed?.Invoke(m_selectedIndex);
		}

		private void Refresh()
		{
			for (var i = 0; i < m_tabs.Count; i++)
			{
				m_tabs[i].EnableInClassList(ActiveUssClassName, i == m_selectedIndex);
			}
		}
	}
}
