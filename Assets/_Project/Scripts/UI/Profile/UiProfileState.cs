using System;
using System.Threading.Tasks;
using Opoint8182.Leaderboard;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Opoint8182.UI
{
	/// <summary>
	/// The player's display name (Profile screen S08, the main menu's profile chip, the share cards and, once synced, the boards).
	/// Persisted locally in <see cref="PlayerPrefs"/> like <see cref="SettingsService"/> and the player stats; the first launch picks a
	/// generated name so nobody is ever blocked by a naming step. A name must satisfy <see cref="DisplayNameRules"/>. It is pushed to
	/// Unity Services (the leaderboards show it) by <see cref="SyncToServiceAsync"/>, which the menu and the Profile screen call: the
	/// name last accepted by the service is remembered, so a name that could not be sent (offline) is retried on the next menu visit.
	/// </summary>
	public static class UiProfileState
	{
		/// <summary>Longest display name, see <see cref="DisplayNameRules.MAX_LENGTH"/>.</summary>
		public const int MAX_NAME_LENGTH = DisplayNameRules.MAX_LENGTH;

		private const string k_NameKey = "profile.name";
		private const string k_SyncedNameKey = "profile.name_synced";
		private const string k_TagKey = "profile.name_tag";

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

		private static string m_s_name;
		private static bool m_s_syncing;
		private static bool m_s_tagFetched;

		/// <summary>Raised after the name was changed.</summary>
		public static event Action NameChanged;

		/// <summary>
		/// The current name. Read from storage on first use; a missing or invalid stored name is replaced by a generated one and saved.
		/// Setting it does not validate (callers use <see cref="DisplayNameRules"/> first) but saves at once and marks it as not yet synced.
		/// </summary>
		public static string DisplayName
		{
			get
			{
				if (m_s_name != null) return m_s_name;

				var stored = DisplayNameRules.Sanitize(PlayerPrefs.GetString(k_NameKey, string.Empty));
				if (DisplayNameRules.Check(stored) != NameCheck.Ok)
				{
					stored = GenerateName();
					PlayerPrefs.SetString(k_NameKey, stored);
					PlayerPrefs.Save();
				}

				m_s_name = stored;
				return m_s_name;
			}
			set
			{
				if (value == null || value == DisplayName) return;

				m_s_name = value;
				PlayerPrefs.SetString(k_NameKey, value);
				PlayerPrefs.DeleteKey(k_TagKey); // the tag belonged to the old name; the next sync brings the new one
				PlayerPrefs.Save();
				NameChanged?.Invoke();
			}
		}

		/// <summary>Unity's "#1234" tag for the current name ("" until the service has given one), shown muted beside the name.</summary>
		public static string NameTag => PlayerPrefs.GetString(k_TagKey, string.Empty);

		/// <summary>True when the name the service last accepted is the current one.</summary>
		public static bool IsSynced => PlayerPrefs.GetString(k_SyncedNameKey, string.Empty) == DisplayName;

		/// <summary>"Adjective + plane + number", e.g. RustyBiplane47: always a valid name.</summary>
		public static string GenerateName()
		{
			var adjective = m_S_Adjectives[Random.Range(0, m_S_Adjectives.Length)];
			var plane = m_S_Planes[Random.Range(0, m_S_Planes.Length)];
			var number = Random.Range(10, 100);
			return DisplayNameRules.Sanitize($"{adjective}{plane}{number}");
		}

		/// <summary>
		/// Pushes the current name to Unity Services when the service has not accepted it yet. Does nothing when already synced, when
		/// there is no leaderboard service in the scene, or when the device is offline (the next call retries). A name the service
		/// refuses for good is logged and not retried. Safe to fire and forget; a second call while one runs is ignored.
		/// </summary>
		public static async Task SyncToServiceAsync()
		{
			if (m_s_syncing) return;

			m_s_syncing = true;
			try
			{
				// A name changed while a push was in flight is picked up by the second round.
				for (var round = 0; round < 2 && !IsSynced; round++)
				{
					var service = LeaderboardService.TryGetInstance();
					if (service == null) return;

					var name = DisplayName;
					var result = await service.SetPlayerNameAsync(name);
					if (result == NameSyncResult.Failed) return;

					PlayerPrefs.SetString(k_SyncedNameKey, name);
					PlayerPrefs.SetString(k_TagKey, result == NameSyncResult.Synced ? service.PlayerNameTag : string.Empty);
					PlayerPrefs.Save();
				}

				// A name synced before the tag was kept (or one the tag was cleared for) reads it once per session.
				if (IsSynced && NameTag.Length == 0 && !m_s_tagFetched)
				{
					var service = LeaderboardService.TryGetInstance();
					if (service != null)
					{
						m_s_tagFetched = true;
						if (await service.FetchPlayerNameTagAsync() && service.PlayerNameTag.Length > 0)
						{
							PlayerPrefs.SetString(k_TagKey, service.PlayerNameTag);
							PlayerPrefs.Save();
						}
					}
				}
			}
			finally
			{
				m_s_syncing = false;
			}
		}

		/// <summary>Forgets the saved name and what the service last accepted, so the next read generates a new one (Editor test menu).</summary>
		public static void ResetName()
		{
			PlayerPrefs.DeleteKey(k_NameKey);
			PlayerPrefs.DeleteKey(k_SyncedNameKey);
			PlayerPrefs.DeleteKey(k_TagKey);
			PlayerPrefs.Save();
			m_s_name = null;
			m_s_tagFetched = false;
			NameChanged?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache and the handlers.
			m_s_name = null;
			m_s_syncing = false;
			m_s_tagFetched = false;
			NameChanged = null;
		}
	}
}
