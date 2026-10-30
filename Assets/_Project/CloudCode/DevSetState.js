// Daily play structure, Pass 7b. Cloud Code script "DevSetState". DEV ONLY: refuses in the production environment.
// Parameters (declared in the .meta, all optional Numeric): used, dayOffset, ticketStartedAgoMs.
// Sets the caller's attempt state directly, for checks and the Tools > Daily Play reset item:
//   used               attempts spent today (default 0)
//   dayOffset          stored day = today + dayOffset (-1 makes the stored count look like yesterday's)
//   ticketStartedAgoMs when given (>= 0) writes an unused ticket that started this long ago (for the expiry / plausibility
//                      checks); otherwise the ticket is cleared
// Result: { ok: true, used, day, ticketId | null, nowMs } or { ok: false, code: "NOT_ALLOWED" | "ERROR" }
const { DataApi } = require("@unity-services/cloud-save-1.4");

const DAY_MS = 86400000;
const KEY = "daily";

module.exports = async ({ params, context, logger }) => {
  if (context.environmentName === "production") return { ok: false, code: "NOT_ALLOWED" };

  try {
    const api = new DataApi(context);
    const nowMs = Date.now();
    const today = Math.floor(nowMs / DAY_MS);
    const used = params.used !== undefined && params.used !== null ? params.used | 0 : 0;
    const day = today + (params.dayOffset | 0);
    const ago = params.ticketStartedAgoMs;
    const ticket = typeof ago === "number" && ago >= 0
      ? { id: "dev-" + nowMs.toString(36), startedAtMs: nowMs - ago, used: false }
      : null;

    await api.setPrivateCustomItem(context.projectId, context.playerId, { key: KEY, value: { day, used, ticket } });
    return { ok: true, used, day, ticketId: ticket ? ticket.id : null, nowMs };
  } catch (e) {
    logger.error("DevSetState failed: " + (e && e.message));
    return { ok: false, code: "ERROR", message: e && e.message };
  }
};
