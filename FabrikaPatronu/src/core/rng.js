// Tohumlu rastgele sayı üreteci (mulberry32). Durum kayıtta saklanır; aynı tohum aynı oyunu verir.
export function rnd(s) {
  let t = (s.seed = (s.seed + 0x6d2b79f5) | 0);
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}
export const range = (s, a, b) => a + rnd(s) * (b - a);
export const irange = (s, a, b) => Math.floor(range(s, a, b + 1));
export const pick = (s, arr) => arr[Math.floor(rnd(s) * arr.length)];
export const chance = (s, p) => rnd(s) < p;
export function weighted(s, items, w = x => x.weight) {
  const tot = items.reduce((a, x) => a + w(x), 0); let r = rnd(s) * tot;
  for (const x of items) { r -= w(x); if (r <= 0) return x; }
  return items[items.length - 1];
}
