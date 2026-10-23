using System;
using UnityEngine;

namespace Opoint8182.Audio
{
	/// <summary>One sound effect's clips and how <see cref="AudioManager"/> plays it. Defaults live in <see cref="SfxDefaults"/>.</summary>
	[Serializable]
	public class SfxEntry
	{
		public SfxId id;

		[Tooltip("Variations: one is picked at random per play.")]
		public AudioClip[] clips = Array.Empty<AudioClip>();

		[Range(0f, 1f)] public float volume = 1f;

		[Tooltip("Random pitch multiplier range (x = min, y = max).")]
		public Vector2 pitchRange = Vector2.one;

		[Tooltip("Higher wins: when every voice is busy a new sound steals the lowest-priority voice at or below its own priority, and is dropped if none is.")]
		public int priority = 50;

		[Tooltip("Seconds before the same sound may play again. 0 = no limit.")]
		public float cooldown;

		[Tooltip("How many copies of this sound may play at once. 0 = no limit.")]
		public int maxConcurrent;

		[Tooltip("Played through a dedicated looping source with StartLoop / StopLoop instead of the one-shot pool.")]
		public bool loop;

		[Tooltip("Keeps playing while the game is paused (UI and countdown sounds).")]
		public bool ignorePause;

		[Tooltip("While this plays, lower-priority sounds are scaled to this fraction of their volume. 1 = no ducking.")]
		[Range(0f, 1f)] public float duckAmount = 1f;

		[Tooltip("How long the duck holds before the others ease back up.")]
		public float duckSeconds;
	}

	/// <summary>
	/// The id-to-clips table, one asset at <c>Resources/SfxLibrary</c>. Built (not hand-assembled) by the editor menu item
	/// Tools / Audio / Rebuild SfxLibrary, which maps clip file names to <see cref="SfxId"/>.
	/// </summary>
	[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Opoint8182/Sfx Library")]
	public class SfxLibrary : ScriptableObject
	{
		public SfxEntry[] entries = Array.Empty<SfxEntry>();
	}
}
