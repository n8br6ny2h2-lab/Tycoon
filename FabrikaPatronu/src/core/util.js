import { BAL } from "../data/balance.js";
export const MIN_DAY = 1440;
export const day = s => Math.floor(s.time.min / MIN_DAY);
export const hourOfDay = s => Math.floor((s.time.min % MIN_DAY) / 60);
export const clamp = (x, a, b) => Math.max(a, Math.min(b, x));
export const round2 = x => Math.round(x * 100) / 100;
export const uid = s => (s.nextId = (s.nextId || 1) + 1);
export function note(s, text, kind = "info", data) {
  s.notes.unshift({ t: s.time.min, text, kind, data, read: false });
  if (s.notes.length > 80) s.notes.length = 80;
}
export function level(s) { let l = 0; BAL.xpLevels.forEach((x, i) => { if (s.company.xp >= x) l = i; }); return l + 1; }
export const has = (s, research) => !!s.research.done[research];
export const EPS = 1e-6;
