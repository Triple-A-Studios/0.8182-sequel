using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// The player's display name as the UI shows it (Profile screen S08 and the main menu's profile chip). Session-only
	/// static state, like <see cref="UiSettingsState"/>: nothing persists it and nothing sends it to Unity Services, so
	/// the leaderboard keeps showing the service's auto-generated name. Storing it and submitting it (with the
	/// moderation that comes with accepting free text) is the later "Player profile and run stats" milestone, which
	/// replaces this class's backing store.
	/// </summary>
	public static class UiProfileState
	{
		/// <summary>Longest display name. The mockup note said 14 with no recorded reason; the developer chose 16.</summary>
		public const int MAX_NAME_LENGTH = 16;

		/// <summary>The main menu's placeholder name until a first-launch name exists.</summary>
		public const string DEFAULT_NAME = "OLIVE";

		// Adjectives are at most 6 letters and planes at most 8, so adjective + plane + a two-digit number never
		// exceeds MAX_NAME_LENGTH (6 + 8 + 2 = 16).
		private static readonly string[] m_S_Adjectives =
		{
			"Rusty", "Brave", "Lucky", "Salty", "Dizzy", "Tipsy", "Speedy", "Cranky", "Fuzzy", "Sneaky", "Wobbly", "Jolly",
		};

		private static readonly string[] m_S_Planes =
		{
			"Biplane", "Cropper", "Zeppelin", "Glider", "Spitfire", "Hornet", "Cessna", "Dakota", "Prop", "Kite", "Triplane", "Jet",
		};

		public static string DisplayName { get; set; } = DEFAULT_NAME;

		/// <summary>"Adjective + plane + number", e.g. RustyBiplane47: nobody is ever blocked by a naming step.</summary>
		public static string GenerateName()
		{
			var adjective = m_S_Adjectives[Random.Range(0, m_S_Adjectives.Length)];
			var plane = m_S_Planes[Random.Range(0, m_S_Planes.Length)];
			var number = Random.Range(10, 100);
			return Sanitize($"{adjective}{plane}{number}");
		}

		/// <summary>Trims, drops control characters, collapses runs of spaces and cuts to <see cref="MAX_NAME_LENGTH"/>.</summary>
		public static string Sanitize(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return string.Empty;

			var builder = new System.Text.StringBuilder(raw.Length);
			foreach (var character in raw)
			{
				if (char.IsControl(character)) continue;
				if (character == ' ' && (builder.Length == 0 || builder[builder.Length - 1] == ' ')) continue;
				builder.Append(character);
			}

			var result = builder.ToString().TrimEnd();
			return result.Length > MAX_NAME_LENGTH ? result.Substring(0, MAX_NAME_LENGTH).TrimEnd() : result;
		}
	}
}
