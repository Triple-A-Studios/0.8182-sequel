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

	public enum SubmitStatus
	{
		Ok,

		/// <summary>The server refused the score (no or used or expired ticket, an implausible score): retrying cannot help.</summary>
		Rejected,

		/// <summary>The score did not get through (offline, timed out, busy, a board error): the ticket is still valid, a retry can work.</summary>
		Failed
	}

	/// <summary>What the <c>SubmitScore</c> script said.</summary>
	public readonly struct SubmitScoreReply
	{
		public readonly SubmitStatus Status;
		/// <summary>1-based weekly rank after the submit.</summary>
		public readonly int WeeklyRank;
		/// <summary>The player's best weekly score after the submit (the server keeps the best), not necessarily the one sent.</summary>
		public readonly int WeeklyScore;
		/// <summary>The player's best all-time score after the submit, or 0 when that board could not be written this time.</summary>
		public readonly int AllTimeScore;

		public SubmitScoreReply(SubmitStatus status, int weeklyRank, int weeklyScore, int allTimeScore)
		{
			Status = status;
			WeeklyRank = weeklyRank;
			WeeklyScore = weeklyScore;
			AllTimeScore = allTimeScore;
		}

		public static SubmitScoreReply Failed => new(SubmitStatus.Failed, 0, 0, 0);
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
		/// Sends a finished competitive run's score with the ticket <c>StartRun</c> issued. The server checks the ticket and the score against
		/// the time since the ticket, then writes both boards itself (the access policy denies the client's own writes).
		/// </summary>
		public static async Task<SubmitScoreReply> SubmitScoreAsync(string ticketId, int score)
		{
			var service = LeaderboardService.TryGetInstance();
			if (service == null) return SubmitScoreReply.Failed;

			var reply = await service.CallServerAsync("SubmitScore", new Dictionary<string, object> { { "ticketId", ticketId }, { "score", score } });
			var status = Classify(reply);
			if (status != SubmitStatus.Ok) return new SubmitScoreReply(status, 0, 0, 0);

			var weekly = reply["weekly"];
			var allTime = reply["allTime"];
			var allTimeScore = allTime != null && allTime.Type == JTokenType.Object ? (int)allTime["score"] : 0;
			return new SubmitScoreReply(SubmitStatus.Ok, (int)weekly["rank"], (int)weekly["score"], allTimeScore);
		}

		/// <summary>Sorts a <c>SubmitScore</c> reply: accepted, refused for good, or failed in a way a retry can fix.</summary>
		public static SubmitStatus Classify(JToken reply)
		{
			if (reply == null) return SubmitStatus.Failed;
			if (IsOk(reply)) return reply["weekly"] != null ? SubmitStatus.Ok : SubmitStatus.Failed;

			switch ((string)reply["code"])
			{
				case "NO_TICKET":
				case "TICKET_USED":
				case "TICKET_EXPIRED":
				case "IMPLAUSIBLE":
					return SubmitStatus.Rejected;
				default:
					return SubmitStatus.Failed;
			}
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
