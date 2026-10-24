using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.AudioEditor
{
	/// <summary>
	/// Creates <c>Resources/HapticProfiles.asset</c> with the default pattern for every <see cref="HapticTier"/>.
	/// "Create" keeps any pattern already tuned in the asset and only adds missing tiers; "Reset" overwrites them all.
	/// </summary>
	public static class HapticProfilesBuilder
	{
		private const string k_AssetPath = "Assets/_Project/Resources/HapticProfiles.asset";

		[MenuItem("Tools/Haptics/Create HapticProfiles (keep tuning)")]
		private static void CreateMenu() => Debug.Log(Build(reset: false));

		[MenuItem("Tools/Haptics/Reset HapticProfiles To Defaults")]
		private static void ResetMenu() => Debug.Log(Build(reset: true));

		/// <summary>Creates or updates the asset and returns a one-line summary (also used from the Unity CLI).</summary>
		public static string Build(bool reset)
		{
			var asset = AssetDatabase.LoadAssetAtPath<HapticProfiles>(k_AssetPath);
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<HapticProfiles>();
				AssetDatabase.CreateAsset(asset, k_AssetPath);
			}

			var existing = asset.profiles.Where(p => p != null).ToDictionary(p => p.tier);
			var profiles = new List<HapticProfile>();

			foreach (HapticTier tier in Enum.GetValues(typeof(HapticTier)))
			{
				profiles.Add(!reset && existing.TryGetValue(tier, out var kept) ? kept : HapticProfile.CreateDefault(tier));
			}

			asset.profiles = profiles.ToArray();
			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();

			return $"[HapticProfiles] {profiles.Count} profiles" + (reset ? " (reset to defaults)" : string.Empty);
		}
	}
}
