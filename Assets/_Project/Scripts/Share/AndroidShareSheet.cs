#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace Opoint8182.Share
{
	/// <summary>
	/// Android backend of <see cref="ShareService"/>: writes the card PNG into the app's cache share folder and opens the system
	/// share sheet through <c>ShareHelper.java</c> (Assets/Plugins/Android). The file reaches the chosen app through
	/// <c>ShareFileProvider</c>, declared in the custom manifest, so no permission is involved.
	/// </summary>
	public static class AndroidShareSheet
	{
		private const string k_HelperClass = "com.tripleastudios.opoint8182.share.ShareHelper";
		private const string k_FileName = "opoint8182_card.png";
		private const string k_SheetTitle = "Share";

		/// <summary>True when the share sheet was started; false when anything on the way failed (nothing is thrown).</summary>
		public static bool Share(byte[] png, string text)
		{
			try
			{
				using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
				using var helper = new AndroidJavaClass(k_HelperClass);

				var folder = helper.CallStatic<string>("prepareShareDir", activity);
				var path = Path.Combine(folder, k_FileName);
				File.WriteAllBytes(path, png);

				return helper.CallStatic<bool>("share", activity, path, text, k_SheetTitle);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Share] Android share failed: " + e.Message);
				return false;
			}
		}
	}
}
#endif
