using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Opoint8182.Leaderboard;

namespace Opoint8182.Server
{
	/// <summary>What the <c>GetDailyState</c> script said.</summary>
	public readonly struct DailyStateReply
	{
		/// <summary>False when the server could not be reached (offline, timed out, a standalone scene with no service, a script error).</summary>
		public readonly bool Success;
		public readonly int AttemptsUsed;
		public readonly long NowMs;
		public readonly long NextResetMs;

		public DailyStateReply(bool success, int attemptsUsed, long nowMs, long nextResetMs)
		{
			Success = success;
			AttemptsUsed = attemptsUsed;
			NowMs = nowMs;
			NextResetMs = nextResetMs;
		}
	}

	public enum StartRunStatus
	{
		Started,
		NoAttempts,

		/// <summary>Nothing was spent: the server could not be reached, was busy, or failed.</summary>
		Unavailable
	}

	/// <summary>What the <c>StartRun</c> script said.</summary>
	public readonly struct StartRunReply
	{
		public readonly StartRunStatus Status;
		public readonly string TicketId;
		public readonly int AttemptNumber;
		public readonly int AttemptsUsed;
		public readonly long NowMs;
		public readonly long NextResetMs;

		public StartRunReply(StartRunStatus status, string ticketId, int attemptNumber, int attemptsUsed, long nowMs, long nextResetMs)
		{
			Status = status;
			TicketId = ticketId;
			AttemptNumber = attemptNumber;
			AttemptsUsed = attemptsUsed;
			NowMs = nowMs;
			NextResetMs = nextResetMs;
		}

		public static StartRunReply Unavailable => new(StartRunStatus.Unavailable, null, 0, 0, 0, 0);
	}

	/// <summary>
	/// The game's side of the Cloud Code scripts in <c>Assets/_Project/CloudCode</c> (Daily play structure Pass 7): typed calls over
	/// <see cref="LeaderboardService.CallServerAsync"/>, which signs in, fails fast offline and times out. The server reads the player id
	/// from the token, so nothing here is keyed to the device. Every method returns a failure value instead of throwing.
	/// </summary>
	public static class DailyServer
	{
		/// <summary>Today's attempts on the server's UTC day, plus the server clock.</summary>
		public static async Task<DailyStateReply> GetStateAsync()
		{
			var reply = await CallAsync("GetDailyState");
			if (!IsOk(reply)) return default;

			return new DailyStateReply(true, (int)reply["attemptsUsed"], (long)reply["nowMs"], (long)reply["nextResetMs"]);
		}

		/// <summary>
		/// Spends one competitive attempt and returns the ticket the run's score needs. <see cref="StartRunStatus.NoAttempts"/> carries
		/// the server's count; <see cref="StartRunStatus.Unavailable"/> means nothing was spent.
		/// </summary>
		public static async Task<StartRunReply> StartRunAsync()
		{
			var reply = await CallAsync("StartRun");
			if (reply == null) return StartRunReply.Unavailable;

			if (IsOk(reply))
			{
				var number = (int)reply["attemptNumber"];
				return new StartRunReply(StartRunStatus.Started, (string)reply["ticketId"], number, number, (long)reply["nowMs"], (long)reply["nextResetMs"]);
			}

			if ((string)reply["code"] == "NO_ATTEMPTS" && reply["attemptsUsed"] != null)
				return new StartRunReply(StartRunStatus.NoAttempts, null, 0, (int)reply["attemptsUsed"], (long)reply["nowMs"], (long)reply["nextResetMs"]);

			return StartRunReply.Unavailable;
		}

		/// <summary>
		/// Dev only: sets today's server attempts back to 0 and clears the ticket (the <c>DevSetState</c> script refuses in the production
		/// environment). Backs the Tools > Daily Play reset item.
		/// </summary>
		public static async Task<bool> ResetDevStateAsync()
		{
			var service = LeaderboardService.TryGetInstance();
			if (service == null) return false;

			var reply = await service.CallServerAsync("DevSetState", new Dictionary<string, object> { { "used", 0 } });
			return IsOk(reply);
		}

		private static async Task<JToken> CallAsync(string endpoint)
		{
			var service = LeaderboardService.TryGetInstance();
			if (service == null) return null;

			return await service.CallServerAsync(endpoint);
		}

		private static bool IsOk(JToken reply)
		{
			return reply != null && reply["ok"] != null && reply["ok"].Type == JTokenType.Boolean && (bool)reply["ok"];
		}
	}
}
