// InventoryManager — bölge stokları. Her kalem: miktar, ağırlıklı ortalama birim maliyet ve ortalama kalite.
import { RES } from "../data/resources.js";
const EPS = 1e-6;
export const qty = (s, region, id) => (s.inventory[region][id] || { q: 0 }).q;
export const item = (s, region, id) => s.inventory[region][id] || { q: 0, cost: 0, ql: 0 };
export function add(s, region, id, q, unitCost, ql) {
  if (!(q > 0)) return;
  const inv = s.inventory[region], it = inv[id] || (inv[id] = { q: 0, cost: 0, ql: 0 });
  const nq = it.q + q;
  it.cost = (it.cost * it.q + unitCost * q) / nq; it.ql = (it.ql * it.q + ql * q) / nq; it.q = nq;
}
// Çıkarılan miktarın birim maliyetini ve kalitesini döndürür. Stok hiçbir zaman eksiye düşmez.
export function remove(s, region, id, q) {
  const inv = s.inventory[region], it = inv[id];
  if (!it || it.q < q - EPS) throw new Error(`Stok yetersiz: ${id} ${region} (${it ? it.q : 0} < ${q})`);
  it.q = Math.max(0, it.q - q); const out = { cost: it.cost, ql: it.ql };
  if (it.q < EPS) delete inv[id];
  return out;
}
export function regionItems(s, region) { return Object.entries(s.inventory[region]).map(([id, it]) => ({ id, ...it, r: RES[id] })); }
export function allItems(s) { const out = []; for (const reg in s.inventory) for (const [id, it] of Object.entries(s.inventory[reg])) out.push({ region: reg, id, ...it }); return out; }
