using UnityEngine;

namespace Opoint8182.Common
{
	/// <summary>
	/// Sets the target frame rate on Android, where it would otherwise stay at the platform default (30). Android only:
	/// WebGL is left at -1 on purpose, so the browser's <c>requestAnimationFrame</c> paces it (any other value switches
	/// WebGL to a timer loop). The Editor and desktop builds are not touched.
	/// <para>
	/// Vsync is 0 on both quality tiers, so a software limiter paces the frame: a value just above the display's refresh rate
	/// (61 on a 60 Hz panel) can pace better than exactly 60. Development builds read an override from <c>PlayerPrefs</c>
	/// (<see cref="SetDevOverride"/>, used by the perf overlay) to try that on the phone without a rebuild; release builds
	/// always use <see cref="ANDROID_TARGET"/>.
	/// </para>
	/// </summary>
	public static class FrameRateSetup
	{
		public const int ANDROID_TARGET = 60;

		private const string k_DevOverrideKey = "dev.targetFrameRate";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Apply()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			var target = ANDROID_TARGET;
			if (Debug.isDebugBuild) target = PlayerPrefs.GetInt(k_DevOverrideKey, ANDROID_TARGET);
			Application.targetFrameRate = target;
#endif
		}

		/// <summary>
		/// Development builds only: changes the target now and keeps it for the next launch (any value, -1 included, which is
		/// the platform default; 0 clears the override).
		/// </summary>
		public static void SetDevOverride(int fps)
		{
			if (!Debug.isDebugBuild) return;

			if (fps != 0)
			{
				PlayerPrefs.SetInt(k_DevOverrideKey, fps);
				Application.targetFrameRate = fps;
			}
			else
			{
				PlayerPrefs.DeleteKey(k_DevOverrideKey);
				Application.targetFrameRate = ANDROID_TARGET;
			}

			PlayerPrefs.Save();
		}
	}
}
