// ResearchManager — teknoloji ağacı. Aynı anda bir araştırma yürür.
import { RESEARCH, RS } from "../data/research.js";
import { RG } from "../data/regions.js";
import { spend } from "./finance.js";
import { note } from "./util.js";
import { researchMul } from "./events.js";
import { questEvent } from "./quests.js";
import { addXp } from "./progress.js";

export const available = (s, id) => RS[id].req.every(r => s.research.done[r]);
export const researchCost = (s, id) => RS[id].cost * researchMul(s);
export function researchSpeed(s) {
  const reg = Math.max(...Object.entries(s.regions).filter(([, r]) => r.owned).map(([id]) => RG[id].researchMod || 1));
  return reg * (1 + 0.05 * s.staff.muhendis.n * (1 + 0.1 * (s.staff.muhendis.lvl - 1))) / researchMul(s);
}
export function startResearch(s, id) {
  const r = RS[id]; if (!r) return { ok: false };
  if (s.research.done[id]) return { ok: false, msg: "Zaten tamamlandı" };
  if (s.research.active) return { ok: false, msg: "Başka bir araştırma sürüyor" };
  if (!available(s, id)) return { ok: false, msg: "Önce gereken araştırmaları bitir" };
  const c = researchCost(s, id); if (s.company.cash < c) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, c, "Araştırma"); s.research.active = { id, left: r.days * 24 }; return { ok: true };
}
export function researchHour(s) {
  const a = s.research.active; if (!a) return;
  a.left -= researchSpeed(s);
  if (a.left <= 0) { s.research.done[a.id] = s.time.min; s.research.active = null; note(s, `Araştırma tamamlandı: ${RS[a.id].name}. ${RS[a.id].desc}`, "good"); s.stats.researchDone++; questEvent(s, "research", {}); addXp(s, RS[a.id].cost / 1500); }
}
export const researchList = () => RESEARCH;
