using System;
using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// The haptic patterns, one asset at <c>Resources/HapticProfiles</c>. Created and reset by the editor menu
	/// Tools / Haptics / Create Or Reset HapticProfiles; tune the arrays in the Inspector afterwards.
	/// In its own file because Unity binds a ScriptableObject asset to its script by file name: with the class inside
	/// HapticProfile.cs the asset's script reference was empty ("Script attached to 'HapticProfiles' ... is missing") and a player
	/// build fell back to the built-in defaults.
	/// </summary>
	[CreateAssetMenu(fileName = "HapticProfiles", menuName = "Opoint8182/Haptic Profiles")]
	public class HapticProfiles : ScriptableObject
	{
		public HapticProfile[] profiles = Array.Empty<HapticProfile>();
	}
}
