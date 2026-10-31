using UnityEngine;

namespace Opoint8182.Share
{
	public enum ShareResult
	{
		/// <summary>The system share sheet was opened (Android). The player's own app takes it from there.</summary>
		Shared,

		/// <summary>The card was saved as an image (Editor, WebGL download).</summary>
		Saved,

		/// <summary>The card could not be made or handed on; a neutral message says so.</summary>
		Failed,

		/// <summary>Another share is still running; this tap is ignored.</summary>
		Busy,
	}

	/// <summary>
	/// The one entry for every SHARE button (Daily play structure, Pass 8): renders the card and hands it to the platform.
	/// Android opens the share sheet with the image and the one-line text (no storage permission, nothing saved to the gallery;
	/// the sheet offers Save itself), the Editor writes a PNG into <c>ShareCards/</c>, WebGL downloads the PNG (Pass 8c). Sharing is
	/// only ever called from a tap, and one share runs at a time.
	/// </summary>
	public static class ShareService
	{
		public const string SAVED_MESSAGE = "SAVED IMAGE";
		public const string FAILED_MESSAGE = "COULDN'T SHARE";

		private static bool m_s_busy;

		public static async Awaitable<ShareResult> ShareAsync(ShareCardData data)
		{
			if (m_s_busy) return ShareResult.Busy;

			m_s_busy = true;
			try
			{
				var png = await ShareCardRenderer.RenderPngAsync(data);
				if (png == null) return ShareResult.Failed;

#if UNITY_EDITOR
				var path = ShareCardEditorSave.Save(png, "share_" + data.Kind.ToString().ToLowerInvariant());
				Debug.Log($"[Share] saved {path} ({png.Length} bytes): {data.Text}");
				return ShareResult.Saved;
#elif UNITY_ANDROID
				return AndroidShareSheet.Share(png, data.Text) ? ShareResult.Shared : ShareResult.Failed;
#else
				return ShareResult.Failed;
#endif
			}
			finally
			{
				m_s_busy = false;
			}
		}

		/// <summary>The toast for a result, or null when nothing needs saying (the sheet opened, or a repeat tap).</summary>
		public static string ToastFor(ShareResult result)
		{
			switch (result)
			{
				case ShareResult.Saved: return SAVED_MESSAGE;
				case ShareResult.Failed: return FAILED_MESSAGE;
				default: return null;
			}
		}
	}
}
