import { newGame } from "../src/core/state.js";
export const fresh = (seed = 7) => newGame("Test A.Ş.", seed);
export function rich(seed = 7, cash = 5e8) { const s = fresh(seed); s.company.cash = cash; s.company.xp = 1e8; return s; }
export function memStorage() { const m = new Map(); return { get: k => (m.has(k) ? m.get(k) : null), set: (k, v) => void m.set(k, v), m }; }
export const noNegative = s => { for (const r in s.inventory) for (const [id, it] of Object.entries(s.inventory[r])) if (!(it.q >= -1e-9)) return `${r}/${id}=${it.q}`; return null; };
