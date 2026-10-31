#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Opoint8182.Share
{
	/// <summary>
	/// The Editor's stand-in for the share sheet and the WebGL download: writes the PNG into <c>ShareCards/</c> next to
	/// the project (git-ignored) so a card can be opened and looked at. Editor only.
	/// </summary>
	public static class ShareCardEditorSave
	{
		public static string Save(byte[] png, string label)
		{
			var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ShareCards");
			Directory.CreateDirectory(folder);
			var path = Path.Combine(folder, label + ".png");
			File.WriteAllBytes(path, png);
			return path;
		}
	}
}
#endif
