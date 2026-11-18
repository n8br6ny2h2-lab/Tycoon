// SaveManager — sürümlü JSON, sağlama toplamı, yedek kayıt, eski sürümlerden göç. Depolama arayüzü dışarıdan verilir
// (tarayıcıda localStorage, iOS'ta Capacitor Preferences, ileride bulut).
import { BAL } from "../data/balance.js";
import { REGIONS } from "../data/regions.js";
import { RESOURCES } from "../data/resources.js";
import { POSITIONS } from "../data/employees.js";

export const KEY = "fabrika-patronu-save", BACKUP = "fabrika-patronu-backup";
export function checksum(str) { let h = 2166136261; for (let i = 0; i < str.length; i++) { h ^= str.charCodeAt(i); h = Math.imul(h, 16777619); } return (h >>> 0).toString(36); }
export function serialize(s) { const body = JSON.stringify(s); return JSON.stringify({ v: BAL.version, saved: Date.now(), sum: checksum(body), body }); }
export function deserialize(text) {
  const w = JSON.parse(text); if (!w || typeof w.body !== "string") throw new Error("Kayıt biçimi bozuk");
  if (checksum(w.body) !== w.sum) throw new Error("Kayıt sağlaması tutmuyor");
  return migrate(JSON.parse(w.body), w.v);
}
// Eski kayıtlara sonradan eklenen alanları doldurur; yeni kaynak, bölge ve pozisyonları ekler.
export const MIGRATIONS = { /* 2: s => { ... } */ };
export function migrate(s, from = s.v || 1) {
  for (let v = from + 1; v <= BAL.version; v++) if (MIGRATIONS[v]) MIGRATIONS[v](s);
  for (const r of REGIONS) { if (!s.regions[r.id]) s.regions[r.id] = { owned: false, plots: Array.from({ length: r.cols * r.rows }, () => ({ owned: false, b: null })) }; if (!s.inventory[r.id]) s.inventory[r.id] = {}; }
  for (const r of RESOURCES) if (!s.market[r.id]) s.market[r.id] = { stock: r.cons * BAL.targetStockDays, price: r.base, prev: r.base, open: r.base, hist: { h: [r.base], q: [r.base], d: [r.base] }, vol: 0 };
  for (const p of POSITIONS) if (!s.staff[p.id]) s.staff[p.id] = { n: 0, lvl: 1, training: 0 };
  for (const k of ["orders", "shipments", "fleet", "routes", "autoStock", "contracts", "offers", "loans", "events", "notes"]) if (!Array.isArray(s[k])) s[k] = [];
  s.v = BAL.version; return s;
}
// storage: { get(key) → string|null, set(key, value) } (eşzamanlı ya da Promise döndürebilir)
export async function saveGame(s, storage) {
  const data = serialize(s), prev = await storage.get(KEY);
  if (prev) { try { deserialize(prev); await storage.set(BACKUP, prev); } catch { /* bozuk eski kayıt yedeğe yazılmaz */ } }
  await storage.set(KEY, data); return data.length;
}
export async function loadGame(storage) {
  const main = await storage.get(KEY);
  if (main) { try { return { state: deserialize(main), from: "main" }; } catch (e) { var err = e; } }
  const bak = await storage.get(BACKUP);
  if (bak) { try { return { state: deserialize(bak), from: "backup", error: err && err.message }; } catch { } }
  return { state: null, from: main ? "corrupt" : "none", error: err && err.message };
}
