// Şirket seviyesi ve itibar.
import { BAL } from "../data/balance.js";
import { clamp, level, note } from "./util.js";
import { specialEffect } from "./events.js";
export function addXp(s, x) {
  const before = level(s); s.company.xp += Math.max(0, x);
  const after = level(s); if (after > before) note(s, `Şirket seviyesi ${after}: ${BAL.levelNames[after - 1]}. Yeni fabrikalar, bölgeler ve müşteriler açıldı.`, "level");
}
export function addRep(s, d) { s.company.rep = clamp(s.company.rep + (d > 0 ? d * (specialEffect(s, "rep") || 1) : d), BAL.repMin, BAL.repMax); }
