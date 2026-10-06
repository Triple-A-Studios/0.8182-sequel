using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Opoint8182.Leaderboard
{
	/// <summary>
	/// Throwaway spike (Live leaderboard, Pass 2): proves UGS anonymous sign-in, score submit and top-scores fetch
	/// on Android and WebGL. Logs to the console AND an on-screen IMGUI overlay so a phone build needs no adb.
	/// Deleted once the real LeaderboardService lands.
	/// </summary>
	public class LeaderboardSpike : MonoBehaviour
	{
		private const string k_LogPrefix = "[LeaderboardSpike] ";
		private const int k_MaxLines = 25;
		private const float k_ReferenceHeight = 540f;

		[SerializeField] private string m_leaderboardId = "spike_test";
		[SerializeField] private int m_topLimit = 10;

		private readonly List<string> m_lines = new();
		private bool m_running;
		private bool m_rerunRequested;

		private async void Start()
		{
			await RunSequence();
		}

		private async System.Threading.Tasks.Task RunSequence()
		{
			if (m_running) return;
			m_running = true;

			Log($"platform={Application.platform} unity={Application.unityVersion}");

			try
			{
				if (UnityServices.State != ServicesInitializationState.Initialized)
				{
					Log("1. UnityServices.InitializeAsync...");
					await UnityServices.InitializeAsync();
				}
				Log($"1. OK, services state={UnityServices.State}");

				if (!AuthenticationService.Instance.IsSignedIn)
				{
					Log("2. SignInAnonymouslyAsync...");
					await AuthenticationService.Instance.SignInAnonymouslyAsync();
				}
				Log($"2. OK, playerId={AuthenticationService.Instance.PlayerId} name={AuthenticationService.Instance.PlayerName}");

				int score = Random.Range(1, 10000);
				Log($"3. AddPlayerScoreAsync({m_leaderboardId}, {score})...");
				var added = await LeaderboardsService.Instance.AddPlayerScoreAsync(m_leaderboardId, score);
				Log($"3. OK, rank={added.Rank} score={added.Score}");

				Log($"4. GetScoresAsync top {m_topLimit}...");
				var page = await LeaderboardsService.Instance.GetScoresAsync(m_leaderboardId,
					new GetScoresOptions { Limit = m_topLimit });
				foreach (var entry in page.Results)
					Log($"   #{entry.Rank} {entry.PlayerName} {entry.Score}");
				Log($"4. OK, {page.Results.Count} entries");

				Log("5. GetPlayerScoreAsync...");
				var own = await LeaderboardsService.Instance.GetPlayerScoreAsync(m_leaderboardId);
				Log($"5. OK, own rank={own.Rank} score={own.Score}");

				Log("DONE");
			}
			catch (Exception e)
			{
				Log($"FAILED {e.GetType().Name}: {e.Message}");
				Debug.LogException(e);
			}
			finally
			{
				m_running = false;
			}
		}

		private void Log(string message)
		{
			Debug.Log(k_LogPrefix + message);
			m_lines.Add(message);
			if (m_lines.Count > k_MaxLines)
				m_lines.RemoveAt(0);
		}

		private async void Update()
		{
			if (!m_rerunRequested) return;
			m_rerunRequested = false;
			await RunSequence();
		}

		private void OnGUI()
		{
			float scale = Screen.height / k_ReferenceHeight;
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
			float width = Screen.width / scale;

			GUILayout.BeginArea(new Rect(8f, 8f, width - 16f, k_ReferenceHeight - 16f));
			if (GUILayout.Button("Re-run", GUILayout.Width(120f), GUILayout.Height(32f)))
				m_rerunRequested = true;
			foreach (string line in m_lines)
				GUILayout.Label(line);
			GUILayout.EndArea();
		}
	}
}
