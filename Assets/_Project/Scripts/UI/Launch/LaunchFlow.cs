using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// The pure rules of the launch flow (loading screen, rotate gate), kept free of UI so they can be tested on their
	/// own. <see cref="LaunchFlowHost"/> applies them.
	/// </summary>
	public static class LaunchFlow
	{
		/// <summary>The loading screen stays up at least this long, so the quip can be read and it never flashes.</summary>
		public const float MIN_SECONDS = 1.5f;

		/// <summary>
		/// <see cref="AsyncOperation.progress"/> stops at 0.9 until the scene is activated, so 0.9 counts as 100%.
		/// </summary>
		public const float LOAD_PROGRESS_CEILING = 0.9f;

		/// <summary>Bar speed cap in fractions per second, so a jump to 100% still reads as a fill.</summary>
		public const float BAR_SPEED = 1.5f;

		/// <summary>Raw load progress (0 to 0.9, or 1 when done) to the bar's 0 to 1.</summary>
		public static float MapProgress(float rawProgress, bool done)
		{
			if (done) return 1f;

			return Mathf.Clamp01(rawProgress / LOAD_PROGRESS_CEILING);
		}

		/// <summary>The page / window is taller than it is wide.</summary>
		public static bool IsPortrait(int width, int height)
		{
			return height > width;
		}

		/// <summary>
		/// The rotate gate exists where a phone can be turned: web (browser pages rotate), mobile players, and the
		/// Editor so it can be tested by resizing the Game view. Android builds are locked to landscape already.
		/// </summary>
		public static bool GateApplies()
		{
			return Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer || Application.isEditor;
		}

		/// <summary>The flow may leave the loading screen: the minimum time has passed and the gate is not up.</summary>
		public static bool CanLeave(float elapsedSeconds, bool gateActive)
		{
			return elapsedSeconds >= MIN_SECONDS && !gateActive;
		}
	}
}
