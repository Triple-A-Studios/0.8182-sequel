// Daily play structure, Pass 7b. Cloud Code script "SubmitScore". Parameters (declared in the .meta, see leaderboard.md):
//   ticketId (String, required), score (Numeric, required).
// Validates a finished run's score against the ticket StartRun issued, then writes it to both boards with the SERVICE
// token (client writes to the boards are denied by the access policy). The client's own best-score gate stays a call saver.
//
// Checks, in order:
//   score is a positive whole number within int range       -> else IMPLAUSIBLE
//   the caller has a ticket with this id                    -> else NO_TICKET
//   the ticket is unused                                    -> else TICKET_USED
//   the ticket is younger than MAX_RUN_AGE_MS               -> else TICKET_EXPIRED
//   score <= MAX_SCORE_PER_SECOND * SERVER elapsed seconds + SCORE_SLACK (elapsed since StartRun, never client-reported)
//                                                           -> else IMPLAUSIBLE (the ticket is burned, so the bound cannot be probed)
// Then the ticket is marked used (lock-guarded: two submits cannot both pass), both boards are written and read back
// (the add call returns an empty body). A board error restores the ticket so the client's retry works.
// Result: { ok: true, weekly: { rank (1-based), score }, allTime: { score } | null }
//      or { ok: false, code: "NO_TICKET" | "TICKET_USED" | "TICKET_EXPIRED" | "IMPLAUSIBLE" | "BUSY" | "BOARD_ERROR" | "ERROR" }
const { DataApi } = require("@unity-services/cloud-save-1.4");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

const KEY = "daily";
const WEEKLY_BOARD = "weekly_scores";
const ALLTIME_BOARD = "alltime_scores";

// Start generous and tune from the logged { score, elapsedSec } pairs (Cloud Code logs in the dashboard). Combo multiplier
// tops out at x4 and building values are small, so real runs sit far below this.
const MAX_SCORE_PER_SECOND = 1500;
const SCORE_SLACK = 5000;
const MAX_RUN_AGE_MS = 3 * 60 * 60 * 1000;
const MAX_SCORE = 2147483647;

async function readDaily(api, context) {
  const r = await api.getPrivateCustomItems(context.projectId, context.playerId, [KEY]);
  const item = r.data.results && r.data.results[0];
  return item ? { value: item.value, writeLock: item.writeLock } : { value: null, writeLock: undefined };
}

// Marks the ticket used (or restores it). Returns false when another call wrote first (409) or the ticket changed.
async function setTicketUsed(api, context, ticketId, used) {
  const { value, writeLock } = await readDaily(api, context);
  if (!value || !value.ticket || value.ticket.id !== ticketId) return false;
  const next = Object.assign({}, value, { ticket: Object.assign({}, value.ticket, { used }) });
  try {
    await api.setPrivateCustomItem(context.projectId, context.playerId, { key: KEY, value: next, writeLock });
    return true;
  } catch (e) {
    if (e && e.response && e.response.status === 409) return false;
    throw e;
  }
}

module.exports = async ({ params, context, logger }) => {
  try {
    const api = new DataApi(context);
    const nowMs = Date.now();
    const score = params.score;

    if (!Number.isInteger(score) || score <= 0 || score > MAX_SCORE) {
      logger.warning("SubmitScore rejected: score is not a positive int: " + score);
      return { ok: false, code: "IMPLAUSIBLE" };
    }

    const { value } = await readDaily(api, context);
    const ticket = value && value.ticket;
    if (!ticket || ticket.id !== params.ticketId) return { ok: false, code: "NO_TICKET" };
    if (ticket.used) return { ok: false, code: "TICKET_USED" };

    const elapsedMs = nowMs - ticket.startedAtMs;
    if (elapsedMs > MAX_RUN_AGE_MS) return { ok: false, code: "TICKET_EXPIRED" };

    const elapsedSec = Math.max(0, elapsedMs) / 1000;
    const limit = MAX_SCORE_PER_SECOND * elapsedSec + SCORE_SLACK;
    if (score > limit) {
      // Burn the ticket so the bound cannot be probed with smaller and smaller scores.
      await setTicketUsed(api, context, params.ticketId, true);
      logger.warning("SubmitScore IMPLAUSIBLE score=" + score + " elapsedSec=" + elapsedSec.toFixed(1) + " limit=" + Math.floor(limit));
      return { ok: false, code: "IMPLAUSIBLE" };
    }

    if (!(await setTicketUsed(api, context, params.ticketId, true))) return { ok: false, code: "BUSY" };

    const boards = new LeaderboardsApi({ accessToken: context.serviceToken });
    let weekly;
    try {
      await boards.addLeaderboardPlayerScore(context.projectId, WEEKLY_BOARD, context.playerId, { score });
      const entry = await boards.getLeaderboardPlayerScore(context.projectId, WEEKLY_BOARD, context.playerId);
      weekly = { rank: entry.data.rank + 1, score: entry.data.score };
    } catch (e) {
      logger.error("SubmitScore weekly board failed: " + (e && e.message));
      await setTicketUsed(api, context, params.ticketId, false);
      return { ok: false, code: "BOARD_ERROR" };
    }

    // The weekly entry is in, so the ticket stays used; an all-time failure only means this run is not recorded there.
    let allTime = null;
    try {
      await boards.addLeaderboardPlayerScore(context.projectId, ALLTIME_BOARD, context.playerId, { score });
      const entry = await boards.getLeaderboardPlayerScore(context.projectId, ALLTIME_BOARD, context.playerId);
      allTime = { score: entry.data.score };
    } catch (e) {
      logger.warning("SubmitScore all-time board failed: " + (e && e.message));
    }

    logger.info("SubmitScore ok score=" + score + " elapsedSec=" + elapsedSec.toFixed(1));
    return { ok: true, weekly, allTime };
  } catch (e) {
    logger.error("SubmitScore failed: " + (e && e.message));
    return { ok: false, code: "ERROR", message: e && e.message };
  }
};
