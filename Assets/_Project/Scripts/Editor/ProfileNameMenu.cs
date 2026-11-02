using Opoint8182.UI;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.ProfileEditor
{
	/// <summary>Editor shortcuts for checking the saved display name and its sync to Unity Services.</summary>
	public static class ProfileNameMenu
	{
		[MenuItem("Tools/Profile Name/Log Name")]
		private static void LogName()
		{
			Debug.Log($"[ProfileName] '{UiProfileState.DisplayName}', synced to the service: {UiProfileState.IsSynced}");
		}

		[MenuItem("Tools/Profile Name/Reset Name (generate a new one)")]
		private static void ResetName()
		{
			UiProfileState.ResetName();
			Debug.Log("[ProfileName] reset.");
			LogName();
		}

		[MenuItem("Tools/Profile Name/Sync Now")]
		private static async void SyncNow()
		{
			await UiProfileState.SyncToServiceAsync();
			LogName();
		}
	}
}
