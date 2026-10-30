// Daily play structure, Pass 7b. Cloud Code script "GetDailyState". No parameters.
// Returns the caller's attempts for today on the SERVER's UTC day. Nothing is written: a stored count from an earlier
// day simply reads as 0 used (the next StartRun persists the new day).
//
// State lives in ONE Cloud Save private item per player ("daily"), keyed by the authenticated player id only:
//   { day: <server UTC day number>, used: <attempts spent that day>, ticket: { id, startedAtMs, used } | null }
// The helpers below are repeated in each script on purpose: scripts here are deployed one file at a time (no bundling).
const { DataApi } = require("@unity-services/cloud-save-1.4");

const ATTEMPTS_MAX = 3;
const DAY_MS = 86400000;
const KEY = "daily";

const dayOf = (nowMs) => Math.floor(nowMs / DAY_MS);

// A ticket survives the day roll (a run that crosses UTC midnight must still be submittable); the count does not.
function normalize(value, today) {
  if (!value) return { day: today, used: 0, ticket: null };
  return { day: today, used: value.day === today ? (value.used | 0) : 0, ticket: value.ticket || null };
}

module.exports = async ({ params, context, logger }) => {
  try {
    const api = new DataApi(context);
    const nowMs = Date.now();
    const today = dayOf(nowMs);
    const r = await api.getPrivateCustomItems(context.projectId, context.playerId, [KEY]);
    const item = r.data.results && r.data.results[0];
    const state = normalize(item ? item.value : null, today);
    return {
      ok: true,
      attemptsUsed: state.used,
      attemptsLeft: Math.max(0, ATTEMPTS_MAX - state.used),
      attemptsMax: ATTEMPTS_MAX,
      nowMs,
      nextResetMs: (today + 1) * DAY_MS
    };
  } catch (e) {
    logger.error("GetDailyState failed: " + (e && e.message));
    return { ok: false, code: "ERROR", message: e && e.message };
  }
};
