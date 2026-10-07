using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Minimal screen stack plus a single popup layer, shared by the menu, leaderboard, settings, profile, shop and
	/// credits screens. Screens are registered once under an id and shown/hidden with <c>display</c>; every screen
	/// except the root returns one level via <see cref="Back"/>. Popups live in a backdrop element; at most one is
	/// open at a time. It is a plain object (no MonoBehaviour): the owner decides when to call
	/// <see cref="HandleBackRequested"/> (Android back button, Escape, an on-screen back chevron).
	/// </summary>
	public class ScreenRouter
	{
		private readonly Dictionary<string, VisualElement> m_screens = new();
		private readonly Stack<string> m_stack = new();
		private readonly VisualElement m_popupLayer;
		private VisualElement m_openPopup;
		private bool m_dismissOnBackdrop;

		public string CurrentScreenId => m_stack.Count > 0 ? m_stack.Peek() : null;
		public bool CanGoBack => m_stack.Count > 1;
		public bool HasOpenPopup => m_openPopup != null;
		public VisualElement OpenPopup => m_openPopup;

		/// <summary>Raised after the visible screen changes, with the new screen id.</summary>
		public event Action<string> ScreenChanged;

		/// <param name="popupLayer">Full-screen backdrop element that holds popups. Hidden until a popup opens.</param>
		public ScreenRouter(VisualElement popupLayer)
		{
			m_popupLayer = popupLayer;
			m_popupLayer.style.display = DisplayStyle.None;
			m_popupLayer.RegisterCallback<ClickEvent>(OnBackdropClicked);
		}

		/// <summary>Registers a screen (hidden until shown). Throws on a duplicate id.</summary>
		public void Register(string id, VisualElement screen)
		{
			if (m_screens.ContainsKey(id)) throw new ArgumentException($"Screen '{id}' is already registered.", nameof(id));

			m_screens.Add(id, screen);
			screen.style.display = DisplayStyle.None;
		}

		/// <summary>Clears the stack and shows <paramref name="id"/> as the new root (the main menu).</summary>
		public void SetRoot(string id)
		{
			CloseOpenPopup();
			m_stack.Clear();
			m_stack.Push(id);
			RefreshVisibility();
		}

		/// <summary>Pushes <paramref name="id"/> on top of the current screen.</summary>
		public void Show(string id)
		{
			if (!m_screens.ContainsKey(id)) throw new ArgumentException($"Screen '{id}' is not registered.", nameof(id));
			if (CurrentScreenId == id) return;

			CloseOpenPopup();
			m_stack.Push(id);
			RefreshVisibility();
		}

		/// <summary>Returns one level. Returns false (and does nothing) at the root.</summary>
		public bool Back()
		{
			if (!CanGoBack) return false;

			CloseOpenPopup();
			m_stack.Pop();
			RefreshVisibility();
			return true;
		}

		/// <summary>
		/// Shows <paramref name="popup"/> (a child of the popup layer) over the current screen, closing any other popup.
		/// </summary>
		public void ShowPopup(VisualElement popup, bool dismissOnBackdrop = true)
		{
			if (popup.parent != m_popupLayer) m_popupLayer.Add(popup);

			CloseOpenPopup();
			m_openPopup = popup;
			m_dismissOnBackdrop = dismissOnBackdrop;
			popup.style.display = DisplayStyle.Flex;
			m_popupLayer.style.display = DisplayStyle.Flex;
		}

		public void ClosePopup()
		{
			CloseOpenPopup();
		}

		/// <summary>Back-button / Escape behaviour: closes the open popup first, otherwise goes back one screen.</summary>
		public bool HandleBackRequested()
		{
			if (HasOpenPopup)
			{
				CloseOpenPopup();
				return true;
			}

			return Back();
		}

		private void CloseOpenPopup()
		{
			if (m_openPopup != null) m_openPopup.style.display = DisplayStyle.None;
			m_openPopup = null;
			m_popupLayer.style.display = DisplayStyle.None;
		}

		private void RefreshVisibility()
		{
			var current = CurrentScreenId;
			foreach (var pair in m_screens)
			{
				pair.Value.style.display = pair.Key == current ? DisplayStyle.Flex : DisplayStyle.None;
			}

			ScreenChanged?.Invoke(current);
		}

		// Only a click on the backdrop itself dismisses; clicks on the popup card bubble up with a different target.
		private void OnBackdropClicked(ClickEvent evt)
		{
			if (evt.target != m_popupLayer || !m_dismissOnBackdrop) return;
			CloseOpenPopup();
		}
	}
}
