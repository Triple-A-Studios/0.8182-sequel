using UnityEngine;

namespace Opoint8182.UI
{
	public enum CreditsLinkKind
	{
		Normal,
		Support,
	}

	/// <summary>One link chip on the credits screen. An empty <see cref="Url"/> leaves the chip visible but inert.</summary>
	public readonly struct CreditsLink
	{
		public readonly string Label;
		public readonly string Url;
		public readonly CreditsLinkKind Kind;

		public CreditsLink(string label, string url, CreditsLinkKind kind = CreditsLinkKind.Normal)
		{
			Label = label;
			Url = url;
			Kind = kind;
		}
	}

	/// <summary>
	/// Everything the credits screen (mockup S10) shows, in one place. <b>The name, links and the thanks list are
	/// placeholders, the developer fills them in here</b> (UI/art pass, Pass 7a). The version line is real. A link with an
	/// empty URL stays visible and does nothing when tapped; a link with an empty label is not shown.
	/// Use full URLs, or <c>mailto:</c> for the email chip.
	/// </summary>
	public static class CreditsContent
	{
		public const string HEADLINE = "MADE BY ONE PERSON";

		/// <summary>Shown as big lines, one label per entry.</summary>
		public static readonly string[] NameLines = { "YOUR NAME", "HERE" };

		public const string BLURB = "Design, code, art, sound and every questionable physics decision.";

		public static readonly CreditsLink[] Links =
		{
			new("itch.io", ""),
			new("@handle", ""),
			new("email", ""),
			new("SUPPORT THE DEV", "", CreditsLinkKind.Support),
		};

		public const string THANKS_TITLE = "THANKS TO THE CRASH TEST CREW";

		public static readonly string[] Thanks =
		{
			"playtester one · playtester two",
			"playtester three · playtester four",
			"and everyone who asked \"why can't I dodge?\"",
		};

		public const string BUILT_WITH_TITLE = "BUILT WITH";

		// Fonts and engine are real. The SFX and music lines stay TBD until the audio milestone brings assets in.
		public static readonly string[] BuiltWith =
		{
			"Unity 6 · Unity Gaming Services (Leaderboards)",
			"Baloo 2 + Nunito (SIL Open Font License)",
			"SFX pack attribution line (TBD)",
			"Music attribution line (TBD)",
		};

		public static string VersionLine => $"v{Application.version}";
	}
}
