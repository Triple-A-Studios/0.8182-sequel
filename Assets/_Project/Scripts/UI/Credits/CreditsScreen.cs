using System;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Credits (mockup S10): maker card with link chips, thanks, built-with and the version line. Plain class over the
	/// instance's root, like <see cref="SettingsScreen"/>. Content is <see cref="CreditsContent"/>. A tapped chip raises
	/// <see cref="LinkRequested"/> and the owner decides what to do with it (the menu calls <c>Application.OpenURL</c>
	/// for a non-empty URL).
	/// </summary>
	public class CreditsScreen
	{
		/// <summary>The back chevron or BACK TO MENU was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>A link chip was tapped.</summary>
		public event Action<CreditsLink> LinkRequested;

		public CreditsScreen(VisualElement root)
		{
			root.Q<Label>("credits-headline").text = CreditsContent.HEADLINE;
			root.Q<Label>("credits-blurb").text = CreditsContent.BLURB;
			root.Q<Label>("credits-thanks-title").text = CreditsContent.THANKS_TITLE;
			FillLines(root.Q<VisualElement>("credits-thanks"), CreditsContent.Thanks, false);
			root.Q<Label>("credits-built-title").text = CreditsContent.BUILT_WITH_TITLE;
			FillLines(root.Q<VisualElement>("credits-built"), CreditsContent.BuiltWith, true);
			root.Q<Label>("credits-version").text = CreditsContent.VersionLine;

			var nameBlock = root.Q<VisualElement>("credits-name");
			nameBlock.Clear();
			foreach (var line in CreditsContent.NameLines)
			{
				var label = new Label(line) { pickingMode = PickingMode.Ignore };
				label.AddToClassList("credits-name__line");
				nameBlock.Add(label);
			}

			var links = root.Q<VisualElement>("credits-links");
			links.Clear();
			foreach (var link in CreditsContent.Links)
			{
				if (string.IsNullOrEmpty(link.Label)) continue;
				links.Add(BuildLinkChip(link));
			}

			root.Q<LipButton>("credits-back").clicked += () => BackRequested?.Invoke();
			root.Q<LipButton>("credits-back-bottom").clicked += () => BackRequested?.Invoke();
		}

		private static void FillLines(VisualElement container, string[] lines, bool small)
		{
			container.Clear();
			foreach (var line in lines)
			{
				var label = new Label(line) { pickingMode = PickingMode.Ignore };
				label.AddToClassList("credits-note__line");
				if (small) label.AddToClassList("credits-note__line--small");
				container.Add(label);
			}
		}

		private LipButton BuildLinkChip(CreditsLink link)
		{
			var support = link.Kind == CreditsLinkKind.Support;

			var chip = new LipButton
			{
				variant = support ? LipVariant.Primary : LipVariant.Secondary,
				lipSize = LipSize.Small,
			};
			chip.AddToClassList("credits-link");
			if (support) chip.AddToClassList("credits-link--support");

			var label = new Label(link.Label) { pickingMode = PickingMode.Ignore };
			label.AddToClassList("credits-link__label");
			chip.Add(label);

			if (support)
			{
				var heart = new VisualElement { pickingMode = PickingMode.Ignore };
				heart.AddToClassList("credits-heart");
				chip.Add(heart);
			}

			chip.clicked += () => LinkRequested?.Invoke(link);
			return chip;
		}
	}
}
