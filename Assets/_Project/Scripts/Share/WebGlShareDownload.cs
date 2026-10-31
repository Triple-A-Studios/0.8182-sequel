#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Opoint8182.Share
{
	/// <summary>
	/// WebGL backend of <see cref="ShareService"/>: downloads the card PNG through <c>ShareDownload.jslib</c> (Assets/Plugins/WebGL).
	/// A page cannot be given a share sheet reliably, so the player posts the saved image themselves.
	/// </summary>
	public static class WebGlShareDownload
	{
		[DllImport("__Internal")]
		private static extern int Opoint8182_DownloadPng(byte[] data, int length, string fileName);

		/// <summary>True when the browser started the download; false when anything failed (nothing is thrown).</summary>
		public static bool Download(byte[] png, string fileName)
		{
			try
			{
				return Opoint8182_DownloadPng(png, png.Length, fileName) == 1;
			}
			catch (Exception e)
			{
				Debug.LogWarning("[Share] WebGL download failed: " + e.Message);
				return false;
			}
		}
	}
}
#endif
