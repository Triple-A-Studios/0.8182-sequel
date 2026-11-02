using System.Text;

namespace Opoint8182.UI
{
	public enum NameCheck
	{
		Ok,

		/// <summary>Fewer than <see cref="DisplayNameRules.MIN_LENGTH"/> characters (an empty name is not an error: it means "keep the old one").</summary>
		TooShort,

		/// <summary>On the blocklist.</summary>
		NotAllowed,
	}

	/// <summary>
	/// What a display name may be (Player profile & run stats Pass 4). The name goes to Unity Services and shows on the boards, so it is
	/// limited to ASCII letters, digits, <c>_</c> and <c>-</c> (Unity Authentication refuses spaces, and the UI fonts only bake a known
	/// glyph set), 3-16 characters, and must not contain a blocklisted word. The blocklist check is a first, client-side line: it is
	/// cheap and works offline, but a modified client can skip it (a server-side check is the later option).
	/// </summary>
	public static class DisplayNameRules
	{
		public const int MIN_LENGTH = 3;
		public const int MAX_LENGTH = 16;

		// Matched as substrings of the normalized name (lowercase, leetspeak undone, digits / _ / - dropped), so a name cannot dodge
		// a word by padding it. Words that sit inside innocent ones ("spic" in spicy, "coon" in raccoon) are left out on purpose: this is a
		// starting list, extend it freely. Keep every entry lowercase letters only.
		private static readonly string[] m_S_Blocked =
		{
			"fuck", "shit", "cunt", "bitch", "whore", "slut", "nigg", "fagg", "retard", "rapist", "nazi", "hitler", "porn", "pussy",
			"asshole", "bastard", "dildo", "penis", "vagina", "paedo", "pedophile", "molest", "wank", "twat", "blowjob", "jizz", "kike",
			"tranny", "kkk",
		};

		/// <summary>Keeps ASCII letters, digits, <c>_</c> and <c>-</c> (everything else, spaces included, is dropped) and cuts to <see cref="MAX_LENGTH"/>.</summary>
		public static string Sanitize(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return string.Empty;

			var builder = new StringBuilder(raw.Length);
			foreach (var character in raw)
			{
				if (!IsAllowedCharacter(character)) continue;

				builder.Append(character);
				if (builder.Length == MAX_LENGTH) break;
			}

			return builder.ToString();
		}

		/// <summary>Checks an already <see cref="Sanitize"/>d name. An empty name is reported as <see cref="NameCheck.TooShort"/>.</summary>
		public static NameCheck Check(string name)
		{
			if (string.IsNullOrEmpty(name) || name.Length < MIN_LENGTH) return NameCheck.TooShort;

			// The digit 1 reads as an i or an l, so both readings are tried.
			return IsBlocked(Normalize(name, 'i')) || IsBlocked(Normalize(name, 'l')) ? NameCheck.NotAllowed : NameCheck.Ok;
		}

		private static bool IsAllowedCharacter(char character)
		{
			return character is >= 'a' and <= 'z' || character is >= 'A' and <= 'Z' || character is >= '0' and <= '9' || character == '_' || character == '-';
		}

		private static string Normalize(string name, char one)
		{
			var builder = new StringBuilder(name.Length);
			foreach (var character in name)
			{
				var lower = char.ToLowerInvariant(character);
				switch (lower)
				{
					case '0': builder.Append('o'); break;
					case '1': builder.Append(one); break;
					case '3': builder.Append('e'); break;
					case '4': builder.Append('a'); break;
					case '5': builder.Append('s'); break;
					case '7': builder.Append('t'); break;
					case '_':
					case '-':
					case '2':
					case '6':
					case '8':
					case '9':
						break;
					default: builder.Append(lower); break;
				}
			}

			return builder.ToString();
		}

		private static bool IsBlocked(string normalized)
		{
			foreach (var word in m_S_Blocked)
			{
				if (normalized.Contains(word)) return true;
			}

			return false;
		}
	}
}
