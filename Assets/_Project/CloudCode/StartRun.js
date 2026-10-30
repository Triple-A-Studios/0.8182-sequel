// Daily play structure, Pass 7b. Cloud Code script "StartRun". No parameters.
// Spends one of the caller's 3 daily competitive attempts and issues the run ticket SubmitScore needs. The attempt is
// spent here, at run start (a kill, a quit or an abandoned run all count), and an unused earlier ticket is replaced, so
// an abandoned run cannot be submitted later.
//
// Counter and ticket are ONE Cloud Save item, so a single write lock guards both: two quick calls cannot both pass.
// Result: { ok: true, ticketId, attemptNumber, attemptsLeft, nowMs, nextResetMs }
//      or { ok: false, code: "NO_ATTEMPTS" | "BUSY" | "ERROR", ... }
const { DataApi } = require("@unity-services/cloud-save-1.4");

const ATTEMPTS_MAX = 3;
const DAY_MS = 86400000;
const KEY = "daily";

const dayOf = (nowMs) => Math.floor(nowMs / DAY_MS);

function normalize(value, today) {
  if (!value) return { day: today, used: 0, ticket: null };
  return { day: today, used: value.day === today ? (value.used | 0) : 0, ticket: value.ticket || null };
}

const newId = () => Date.now().toString(36) + Math.random().toString(36).slice(2, 10);

module.exports = async ({ params, context, logger }) => {
  try {
    const api = new DataApi(context);

    // Two tries: a 409 means another call wrote first; the second read sees its result (and may now be out of attempts).
    for (let attempt = 0; attempt < 2; attempt++) {
      const nowMs = Date.now();
      const today = dayOf(nowMs);
      const r = await api.getPrivateCustomItems(context.projectId, context.playerId, [KEY]);
      const item = r.data.results && r.data.results[0];
      const state = normalize(item ? item.value : null, today);

      if (state.used >= ATTEMPTS_MAX) {
        return { ok: false, code: "NO_ATTEMPTS", attemptsUsed: state.used, attemptsLeft: 0, attemptsMax: ATTEMPTS_MAX, nowMs, nextResetMs: (today + 1) * DAY_MS };
      }

      const ticket = { id: newId(), startedAtMs: nowMs, used: false };
      const next = { day: today, used: state.used + 1, ticket };
      try {
        await api.setPrivateCustomItem(context.projectId, context.playerId, { key: KEY, value: next, writeLock: item ? item.writeLock : undefined });
        return { ok: true, ticketId: ticket.id, attemptNumber: next.used, attemptsLeft: ATTEMPTS_MAX - next.used, nowMs, nextResetMs: (today + 1) * DAY_MS };
      } catch (e) {
        if (e && e.response && e.response.status === 409) continue;
        throw e;
      }
    }
    return { ok: false, code: "BUSY" };
  } catch (e) {
    logger.error("StartRun failed: " + (e && e.message));
    return { ok: false, code: "ERROR", message: e && e.message };
  }
};
