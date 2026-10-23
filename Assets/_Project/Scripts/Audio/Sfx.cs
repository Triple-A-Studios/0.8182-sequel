using UnityEngine;

namespace Opoint8182.Audio
{
	/// <summary>
	/// The call site for sound effects: <c>Sfx.Play(SfxId.UiClick)</c>. Safe to call anywhere, any time: with no
	/// <see cref="AudioManager"/> (Edit Mode, a missing library) it does nothing.
	/// </summary>
	public static class Sfx
	{
		/// <summary>Plays a one-shot. <paramref name="volumeScale"/> and <paramref name="pitchMultiplier"/> scale the entry's own values.</summary>
		public static void Play(SfxId id, float volumeScale = 1f, float pitchMultiplier = 1f)
		{
			var manager = AudioManager.Instance;
			if (manager != null) manager.Play(id, volumeScale, pitchMultiplier);
		}

		public static void StartLoop(SfxId id, float volumeScale = 1f)
		{
			var manager = AudioManager.Instance;
			if (manager != null) manager.StartLoop(id, volumeScale);
		}

		public static void StopLoop(SfxId id)
		{
			var manager = AudioManager.Instance;
			if (manager != null) manager.StopLoop(id);
		}

		/// <summary>Stops every gameplay sound (used when a run restarts); UI sounds finish.</summary>
		public static void StopAll()
		{
			var manager = AudioManager.Instance;
			if (manager != null) manager.StopAll();
		}

		/// <summary>
		/// Pauses / resumes every sound that does not ignore pause. Not routed through the manager: it is one global
		/// switch, and it must work even if the manager is missing.
		/// </summary>
		public static void SetPaused(bool paused)
		{
			AudioListener.pause = paused;
		}
	}
}
