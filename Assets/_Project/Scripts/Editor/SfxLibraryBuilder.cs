using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Opoint8182.Audio;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.AudioEditor
{
	/// <summary>
	/// Builds <c>Resources/SfxLibrary.asset</c> from the clips under <c>Assets/_Project/Audio</c>: a clip's file name (minus
	/// a trailing _1 / _2 variation suffix) is the <see cref="SfxId"/> it belongs to. Clips outside the <c>Placeholder</c>
	/// folder win over the generated placeholders of the same id, so real audio is dropped in by name with no code change.
	/// An existing entry keeps its tuning (priority, ducking, ...); only its clips refresh. Ids with no entry get
	/// <see cref="SfxDefaults"/>.
	/// </summary>
	public static class SfxLibraryBuilder
	{
		private const string k_AudioRoot = "Assets/_Project/Audio";
		private const string k_PlaceholderFolder = "/Placeholder/";
		private const string k_LibraryPath = "Assets/_Project/Resources/SfxLibrary.asset";

		private static readonly Regex m_S_VariationSuffix = new(@"_\d+$", RegexOptions.Compiled);

		[MenuItem("Tools/Audio/Rebuild SfxLibrary")]
		private static void RebuildMenu() => Debug.Log(Build(resetTuning: false));

		[MenuItem("Tools/Audio/Reset SfxLibrary Tuning To Defaults")]
		private static void ResetMenu() => Debug.Log(Build(resetTuning: true));

		/// <summary>Rebuilds the asset and returns a one-paragraph summary (also used from the Unity CLI).</summary>
		public static string Build(bool resetTuning)
		{
			var real = new Dictionary<SfxId, List<AudioClip>>();
			var placeholder = new Dictionary<SfxId, List<AudioClip>>();

			foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { k_AudioRoot }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var id = m_S_VariationSuffix.Replace(Path.GetFileNameWithoutExtension(path), string.Empty);
				if (!Enum.TryParse<SfxId>(id, out var sfxId)) continue;

				var target = path.Contains(k_PlaceholderFolder) ? placeholder : real;
				if (!target.TryGetValue(sfxId, out var list)) target[sfxId] = list = new List<AudioClip>();
				list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
			}

			var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(k_LibraryPath);
			if (library == null)
			{
				library = ScriptableObject.CreateInstance<SfxLibrary>();
				AssetDatabase.CreateAsset(library, k_LibraryPath);
			}

			var existing = library.entries.ToDictionary(e => e.id);
			var entries = new List<SfxEntry>();
			var missing = new List<SfxId>();

			foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
			{
				var entry = !resetTuning && existing.TryGetValue(id, out var kept) ? kept : SfxDefaults.Create(id);
				var clips = real.TryGetValue(id, out var realClips) ? realClips : placeholder.TryGetValue(id, out var placeholderClips) ? placeholderClips : null;

				entry.clips = clips == null ? Array.Empty<AudioClip>() : clips.OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
				if (entry.clips.Length == 0) missing.Add(id);
				entries.Add(entry);
			}

			library.entries = entries.ToArray();
			EditorUtility.SetDirty(library);
			AssetDatabase.SaveAssets();

			var summary = $"[SfxLibrary] {entries.Count} entries, {entries.Count - missing.Count} with clips" + (resetTuning ? " (tuning reset to defaults)" : string.Empty);
			return missing.Count == 0 ? summary : $"{summary}. No clip for: {string.Join(", ", missing)}";
		}
	}
}
