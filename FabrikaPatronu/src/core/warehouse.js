// WarehouseManager — depo kapasitesi (sınıf bazında), yer ayırma, depolama gideri, otomatik stok, hırsızlık.
import { WT } from "../data/warehouses.js";
import { RES } from "../data/resources.js";
import { RG } from "../data/regions.js";
import { BAL } from "../data/balance.js";
import { staffEff } from "./employees.js";
import { spend } from "./finance.js";
import { chance, range, pick } from "./rng.js";
import { note, uid, level } from "./util.js";
import { questEvent } from "./quests.js";

export const whCap = (s, b) => WT[b.type].cap * (1 + 0.6 * (b.level - 1)) * (b.racks ? 1.2 : 1) * (1 + staffEff(s, "depo"));
export const warehousesIn = (s, region) => Object.values(s.buildings).filter(b => b.kind === "warehouse" && b.region === region);
// Gelmekte olan sevkiyatların yer ayırması (bölge → sınıf → hacim).
export function reserve(s, region, id, q) { /* ayırma sevkiyat listesinden hesaplanır */ }
function incomingVol(s, region) {
  const v = {};
  for (const sh of s.shipments) if (sh.to === region && sh.kind !== "contract") for (const it of sh.items) { const r = RES[it.res]; v[r.storage] = (v[r.storage] || 0) + r.vol * it.q; }
  return v;
}
// Hacimleri depolara açgözlü dağıtır: önce en az sınıf kabul eden (en özel) depolar doldurulur.
export function allocation(s, region, extra = {}) {
  const whs = warehousesIn(s, region).map(b => ({ b, cap: whCap(s, b), used: 0, acc: WT[b.type].accepts }));
  whs.sort((a, b) => a.acc.length - b.acc.length);
  const need = { ...incomingVol(s, region) };
  for (const [id, it] of Object.entries(s.inventory[region])) { const r = RES[id]; if (r.storage !== "enerji") need[r.storage] = (need[r.storage] || 0) + r.vol * it.q; }
  for (const [c, v] of Object.entries(extra)) need[c] = (need[c] || 0) + v;
  let overflow = 0;
  for (const [c, v0] of Object.entries(need).sort((a, b) => whs.filter(w => w.acc.includes(a[0])).length - whs.filter(w => w.acc.includes(b[0])).length)) {
    let v = v0;
    for (const w of whs) if (w.acc.includes(c) && v > 0) { const put = Math.min(v, w.cap - w.used); w.used += put; v -= put; }
    overflow += v;
  }
  return { whs, need, overflow };
}
export function freeFor(s, region, id) {
  const r = RES[id]; if (r.storage === "enerji") return Infinity;
  const { whs } = allocation(s, region);
  return whs.filter(w => w.acc.includes(r.storage)).reduce((a, w) => a + Math.max(0, w.cap - w.used), 0);
}
export function capacitySummary(s, region) {
  const { whs, need } = allocation(s, region), out = {};
  for (const w of whs) for (const c of w.acc) { out[c] = out[c] || { cap: 0, used: 0 }; }
  for (const w of whs) { const share = w.acc.length; for (const c of w.acc) out[c].cap += w.cap / share; }
  for (const [c, v] of Object.entries(need)) { out[c] = out[c] || { cap: 0, used: 0 }; out[c].used = v; }
  const tot = whs.reduce((a, w) => a + w.cap, 0), used = whs.reduce((a, w) => a + w.used, 0);
  return { classes: out, total: tot, used, whs };
}
export function buildWarehouse(s, type, region, plot) {
  const t = WT[type], reg = s.regions[region], p = reg.plots[plot];
  if (!reg.owned || !p || !p.owned) return { ok: false, msg: "Arsa senin değil" };
  if (p.b) return { ok: false, msg: "Arsa dolu" };
  if (level(s) < t.unlock.level) return { ok: false, msg: `${t.unlock.level}. seviyede açılır` };
  if (s.company.cash < t.cost) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, t.cost, "Yatırım"); const id = "b" + uid(s);
  s.buildings[id] = { id, kind: "warehouse", type, region, plot, level: 1, security: 0, racks: false, built: s.time.min };
  p.b = id; return { ok: true, id };
}
export const whUpgradeCost = (s, b) => WT[b.type].cost * 0.5 * b.level;
export function upgradeWarehouse(s, id, what) {
  const b = s.buildings[id], t = WT[b.type];
  let cost;
  if (what === "level") { if (b.level >= 5) return { ok: false, msg: "En üst seviye" }; cost = whUpgradeCost(s, b); }
  else if (what === "security") { if (b.security >= 3) return { ok: false, msg: "En üst güvenlik" }; cost = 300000 * (b.security + 1); }
  else if (what === "racks") { if (b.racks) return { ok: false, msg: "Raf sistemi var" }; cost = t.cost * 0.4; }
  if (s.company.cash < cost) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, cost, "Yatırım");
  if (what === "level") b.level++; else if (what === "security") b.security++; else b.racks = true;
  s.stats.upgrades++; questEvent(s, "upgrade", {});
  return { ok: true };
}
// Günlük depo giderleri: bakım + depolanan hacim × sınıf ücreti.
export function warehouseDay(s) {
  let maint = 0, stor = 0;
  for (const b of Object.values(s.buildings)) if (b.kind === "warehouse") maint += WT[b.type].maint * (1 + 0.3 * (b.level - 1));
  for (const reg in s.inventory) {
    const whs = warehousesIn(s, reg);
    for (const [id, it] of Object.entries(s.inventory[reg])) {
      const r = RES[id], rates = whs.filter(b => WT[b.type].accepts.includes(r.storage)).map(b => WT[b.type].rate);
      stor += r.vol * it.q * (rates.length ? Math.min(...rates) : 4);
    }
  }
  if (maint) spend(s, maint, "Depo bakımı"); if (stor) spend(s, stor, "Depolama");
  // Taşma (örneğin depo satıldıysa): fazla stok zarar görür.
  for (const reg in s.inventory) { const a = allocation(s, reg); if (a.overflow > 0.5) note(s, `${RG[reg].short}: depo kapasitesi aşıldı (${Math.round(a.overflow)} hacim). Yeni depo kur ya da sat.`, "bad"); }
  // Hırsızlık
  for (const b of Object.values(s.buildings)) if (b.kind === "warehouse") {
    const p = BAL.theftBase * (1 - 0.3 * b.security) * (1 - staffEff(s, "depo"));
    const items = Object.entries(s.inventory[b.region]).filter(([id]) => WT[b.type].accepts.includes(RES[id].storage));
    if (items.length && chance(s, p)) {
      const [id, it] = pick(s, items), lost = it.q * range(s, 0.03, 0.1);
      it.q -= lost; note(s, `Hırsızlık: ${RG[b.region].short} ${WT[b.type].name}'ndan ${lost.toFixed(1)} ${RES[id].unit} ${RES[id].name} çalındı. Güvenliği artır.`, "bad");
    }
  }
}
// Otomatik stok kuralları: alt sınırın altına inince al, üst sınırı aşınca sat.
export function setAutoStock(s, region, res, min, max) {
  s.autoStock = s.autoStock.filter(x => !(x.region === region && x.res === res));
  if (min > 0 || max > 0) s.autoStock.push({ id: uid(s), region, res, min, max });
}
export function autoStockDay(s, market) {
  for (const a of s.autoStock) {
    const it = s.inventory[a.region][a.res], q = it ? it.q : 0;
    const incoming = s.shipments.filter(sh => sh.to === a.region).reduce((t, sh) => t + sh.items.filter(i => i.res === a.res).reduce((u, i) => u + i.q, 0), 0);
    if (a.min > 0 && q + incoming < a.min) { const want = Math.max(0, (a.max > a.min ? (a.min + a.max) / 2 : a.min * 1.5) - q - incoming); if (want > 0) market.buy(s, a.res, Math.round(want * 10) / 10, a.region, "standart"); }
    if (a.max > 0 && q > a.max) market.sell(s, a.res, Math.round((q - a.max) * 10) / 10, a.region);
  }
}
