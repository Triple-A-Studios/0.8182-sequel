using System.Collections.Generic;
using UnityEngine;

namespace Opoint8182.Audio
{
	/// <summary>
	/// Plays the game's sound effects (no music, by design). A <c>DontDestroyOnLoad</c> singleton created at startup, so it
	/// exists from Bootstrap, from a standalone scene, in the dev galleries and across a Restart's scene reload. Callers use
	/// the static <see cref="Sfx"/> facade. There is no audio mixer: priorities, voice stealing and ducking are code here,
	/// and the pooled sources could be routed to a mixer group later without changing callers.
	/// <para>
	/// One-shots go through a pool of <see cref="k_PoolSize"/> sources. When all are busy a new sound steals the
	/// lowest-priority voice at or below its own priority (oldest first), or is dropped if every voice outranks it. An
	/// entry can also have a cooldown and a concurrency cap, so a burst of crashes does not stack the same clip. A sound
	/// with a duck scales lower-priority voices down for a moment (a per-source multiplier that eases back).
	/// Looping sounds (the boost) get their own source each (<see cref="StartLoop"/> / <see cref="StopLoop"/>).
	/// </para>
	/// <para>
	/// Master volume is <see cref="AudioListener.volume"/> from <see cref="SettingsService.Sound"/> (squared, so the slider
	/// feels even). Pausing is <see cref="AudioListener.pause"/>; entries marked ignore-pause keep playing.
	/// </para>
	/// </summary>
	public class AudioManager : MonoBehaviour
	{
		private const int k_PoolSize = 8;
		private const string k_LibraryPath = "SfxLibrary";
		private const float k_DuckAttackPerSecond = 20f;
		private const float k_DuckRecoverPerSecond = 3f;

		private static AudioManager m_s_instance;

		private sealed class Voice
		{
			public AudioSource source;
			public SfxId id;
			public int priority;
			public float baseVolume;
			public bool ignorePause;
			public float startTime;
		}

		private readonly Dictionary<SfxId, SfxEntry> m_entries = new();
		private readonly Dictionary<SfxId, float> m_lastPlayed = new();
		private readonly List<Voice> m_voices = new();
		private readonly Dictionary<SfxId, Voice> m_loops = new();

		private float m_duckMultiplier = 1f;
		private float m_duckTarget = 1f;
		private int m_duckPriority;
		private float m_duckUntil;

		/// <summary>The live manager, or null before startup / outside Play Mode.</summary>
		public static AudioManager Instance => m_s_instance != null ? m_s_instance : null;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Create()
		{
			if (m_s_instance != null) return;

			var host = new GameObject("AudioManager");
			DontDestroyOnLoad(host);
			m_s_instance = host.AddComponent<AudioManager>();
		}

		private void Awake()
		{
			var library = Resources.Load<SfxLibrary>(k_LibraryPath);
			if (library == null)
			{
				Debug.LogError("[AudioManager] Resources/SfxLibrary is missing: run Tools > Audio > Rebuild SfxLibrary. Sound is disabled.");
			}
			else
			{
				foreach (var entry in library.entries)
				{
					m_entries[entry.id] = entry;
				}
			}

			for (var i = 0; i < k_PoolSize; i++)
			{
				m_voices.Add(new Voice { source = CreateSource($"Voice {i}") });
			}

			SettingsService.Changed += ApplyMasterVolume;
			ApplyMasterVolume();
		}

		private void OnDestroy()
		{
			SettingsService.Changed -= ApplyMasterVolume;
			if (m_s_instance == this) m_s_instance = null;
		}

		private void Update()
		{
			UpdateDuck(Time.unscaledDeltaTime);
		}

		/// <summary>Plays one sound. Returns false when it was dropped (unknown, no clip, cooldown, concurrency cap or outranked).</summary>
		public bool Play(SfxId id, float volumeScale = 1f, float pitchMultiplier = 1f)
		{
			if (!TryGetClip(id, out var entry, out var clip) || entry.loop) return false;

			var now = Time.unscaledTime;
			if (entry.cooldown > 0f && m_lastPlayed.TryGetValue(id, out var last) && now - last < entry.cooldown) return false;
			if (entry.maxConcurrent > 0 && CountPlaying(id) >= entry.maxConcurrent) return false;

			var voice = AcquireVoice(entry.priority);
			if (voice == null) return false;

			StartVoice(voice, entry, clip, volumeScale, pitchMultiplier, loop: false);
			m_lastPlayed[id] = now;
			StartDuck(entry);
			return true;
		}

		/// <summary>Starts a looping sound on its own source. No-op if it is already running.</summary>
		public void StartLoop(SfxId id, float volumeScale = 1f)
		{
			if (!TryGetClip(id, out var entry, out var clip) || !entry.loop) return;

			if (!m_loops.TryGetValue(id, out var voice))
			{
				voice = new Voice { source = CreateSource($"Loop {id}") };
				m_loops[id] = voice;
			}

			if (voice.source.isPlaying) return;

			StartVoice(voice, entry, clip, volumeScale, 1f, loop: true);
		}

		public void StopLoop(SfxId id)
		{
			if (m_loops.TryGetValue(id, out var voice)) voice.source.Stop();
		}

		/// <summary>
		/// Stops every gameplay sound (a Restart reloads the scene but this manager survives it). Ignore-pause sounds are
		/// the UI's (the click that triggered the restart): they finish.
		/// </summary>
		public void StopAll()
		{
			foreach (var voice in m_voices)
			{
				if (!voice.ignorePause) voice.source.Stop();
			}

			foreach (var voice in m_loops.Values)
			{
				if (!voice.ignorePause) voice.source.Stop();
			}

			m_duckMultiplier = 1f;
			m_duckTarget = 1f;
			m_duckPriority = 0;
		}

		public bool IsLoopPlaying(SfxId id) => m_loops.TryGetValue(id, out var voice) && voice.source.isPlaying;

		private bool TryGetClip(SfxId id, out SfxEntry entry, out AudioClip clip)
		{
			clip = null;
			if (!m_entries.TryGetValue(id, out entry) || entry.clips == null || entry.clips.Length == 0) return false;

			clip = entry.clips[entry.clips.Length == 1 ? 0 : Random.Range(0, entry.clips.Length)];
			return clip != null;
		}

		private AudioSource CreateSource(string sourceName)
		{
			var host = new GameObject(sourceName);
			host.transform.SetParent(transform, false);

			var source = host.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0f;
			return source;
		}

		private void StartVoice(Voice voice, SfxEntry entry, AudioClip clip, float volumeScale, float pitchMultiplier, bool loop)
		{
			voice.id = entry.id;
			voice.priority = entry.priority;
			voice.baseVolume = entry.volume * volumeScale;
			voice.ignorePause = entry.ignorePause;
			voice.startTime = Time.unscaledTime;

			var source = voice.source;
			source.Stop();
			source.clip = clip;
			source.loop = loop;
			source.ignoreListenerPause = entry.ignorePause;
			source.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y) * pitchMultiplier;
			source.volume = voice.baseVolume * DuckFor(voice);
			source.Play();
		}

		// A free voice, else the lowest-priority (oldest on a tie) voice at or below this priority, else null.
		private Voice AcquireVoice(int priority)
		{
			Voice steal = null;
			foreach (var voice in m_voices)
			{
				if (!voice.source.isPlaying) return voice;
				if (voice.priority > priority) continue;

				if (steal == null || voice.priority < steal.priority || (voice.priority == steal.priority && voice.startTime < steal.startTime))
				{
					steal = voice;
				}
			}

			return steal;
		}

		private int CountPlaying(SfxId id)
		{
			var count = 0;
			foreach (var voice in m_voices)
			{
				if (voice.source.isPlaying && voice.id == id) count++;
			}

			return count;
		}

		private void StartDuck(SfxEntry entry)
		{
			if (entry.duckAmount >= 1f || entry.duckSeconds <= 0f) return;

			var active = Time.unscaledTime < m_duckUntil;
			m_duckTarget = active ? Mathf.Min(m_duckTarget, entry.duckAmount) : entry.duckAmount;
			m_duckPriority = active ? Mathf.Max(m_duckPriority, entry.priority) : entry.priority;
			m_duckUntil = Mathf.Max(m_duckUntil, Time.unscaledTime + entry.duckSeconds);
		}

		// Fast attack while a duck holds, slower ease back after; voices below the ducker's priority follow the multiplier.
		private void UpdateDuck(float deltaTime)
		{
			var holding = Time.unscaledTime < m_duckUntil;
			var target = holding ? m_duckTarget : 1f;
			var speed = holding ? k_DuckAttackPerSecond : k_DuckRecoverPerSecond;
			var previous = m_duckMultiplier;

			m_duckMultiplier = Mathf.MoveTowards(m_duckMultiplier, target, speed * deltaTime);
			if (!holding && m_duckMultiplier >= 1f)
			{
				m_duckTarget = 1f;
				m_duckPriority = 0;
			}

			if (Mathf.Approximately(previous, m_duckMultiplier) && m_duckMultiplier >= 1f) return;

			foreach (var voice in m_voices) ApplyVolume(voice);
			foreach (var voice in m_loops.Values) ApplyVolume(voice);
		}

		private void ApplyVolume(Voice voice)
		{
			if (voice.source.isPlaying) voice.source.volume = voice.baseVolume * DuckFor(voice);
		}

		private float DuckFor(Voice voice) => voice.priority < m_duckPriority ? m_duckMultiplier : 1f;

		private static void ApplyMasterVolume()
		{
			var linear = Mathf.Clamp01(SettingsService.Sound / SettingsService.SoundMax);
			AudioListener.volume = linear * linear;
		}
	}
}
