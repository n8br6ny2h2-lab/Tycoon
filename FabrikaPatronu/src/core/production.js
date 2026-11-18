// ProductionManager — reçeteleri saat saat çalıştırır: öncelik, girdi çekme, enerji, aşınma, fire, kalite, durma koşulları.
import { FT, RECIPE } from "../data/factories.js";
import { RES } from "../data/resources.js";
import { BAL } from "../data/balance.js";
import { freeFor } from "./warehouse.js";
import { add, remove, qty, item } from "./inventory.js";
import { drawEnergy, produceEnergy, elecPrice } from "./economy.js";
import { spend, canSpendOperating } from "./finance.js";
import { staffEff, workerEfficiency } from "./employees.js";
import { maintain, maintCost } from "./factory.js";
import { clamp, has, note } from "./util.js";
import { questEvent } from "./quests.js";
import { addXp } from "./progress.js";

export const STATUS = { run: "Çalışıyor", wait: "Hammadde bekliyor", maint: "Bakım gerekiyor", full: "Depo dolu", power: "Enerji kesintisi", done: "Üretim tamamlandı", off: "Durduruldu", noWorkers: "İşçi yok", ready: "Hazır" };
export function speedOf(s, b) {
  return (1 + 0.08 * (b.level - 1)) * (1 + 0.05 * b.automation) * (1 + staffEff(s, "mudur")) * (has(s, "robotik") ? 1.1 : 1) * (has(s, "yz_kontrol") ? 1.05 : 1) * (b.condition < 40 ? 0.7 : 1);
}
export function fireOf(s, b, rec) { return rec.fire * (1 - 0.25 * b.fireLvl) * (has(s, "kalite_kontrol") ? 0.85 : 1) * (has(s, "yz_kontrol") ? 0.7 : 1); }
export function qualityOf(s, b, inQl) {
  const t = FT[b.type], rec = RECIPE[b.recipe], metal = Object.keys(rec.out).some(k => k === "celik" || k === "aluminyum");
  const base = t.kind === "factory" ? 55 : 62;
  return clamp(base + 6 * b.qualityLvl + staffEff(s, "muhendis") / Math.max(1, factoryCount(s)) * 3 + (has(s, "kalite_kontrol") ? 6 : 0) + (metal && has(s, "alasim") ? 10 : 0) + (inQl ? 0.3 * (inQl - 60) : 0), 10, 100);
}
const factoryCount = s => Object.values(s.buildings).filter(b => b.kind !== "warehouse").length;
const energySave = s => (has(s, "enerji_tasarrufu") ? 0.15 : 0);
const outMul = (s, b) => (FT[b.type].kind === "extract" && has(s, "derin_maden") ? 1.2 : 1);
function outputsFor(s, b, rec, k) {
  const f = fireOf(s, b, rec), m = outMul(s, b);
  return Object.entries(rec.out).map(([res, q]) => ({ res, q: q * k * (1 - f) * m }));
}
function fits(s, region, outs) {
  const need = {};
  for (const o of outs) { const r = RES[o.res]; if (r.storage === "enerji") continue; need[o.res] = (need[o.res] || 0) + r.vol * o.q; }
  return Object.entries(need).every(([res, v]) => freeFor(s, region, res) >= v - 1e-9);
}
// Kaç makinelik girdi var ve çıktı nereye sığar?
export function startable(s, b) {
  const rec = RECIPE[b.recipe]; let k = b.machines;
  for (const [res, q] of Object.entries(rec.in)) k = Math.min(k, Math.floor(qty(s, b.region, res) / q + 1e-9));
  if (k <= 0) return { k: 0, why: STATUS.wait };
  while (k > 0 && !fits(s, b.region, outputsFor(s, b, rec, k))) k--;
  if (k <= 0) return { k: 0, why: STATUS.full };
  return { k };
}
function start(s, b, effW) {
  const rec = RECIPE[b.recipe], t = FT[b.type], st = startable(s, b);
  if (!st.k) { b.status = st.why; return false; }
  const used = {}, costs = {}; let cost = 0, qlSum = 0, qlW = 0;
  for (const [res, q] of Object.entries(rec.in)) { const need = q * st.k, lot = remove(s, b.region, res, need); used[res] = need; costs[res] = lot.cost; cost += lot.cost * need; qlSum += lot.ql * need * RES[res].base; qlW += need * RES[res].base; }
  const labor = rec.labor * st.k; spend(s, labor, "Üretim gideri"); cost += labor;
  const dur = rec.hours / speedOf(s, b) / effW;
  b.cycle = { k: st.k, left: dur, total: dur, cost, used, costs, ql: qlW ? qlSum / qlW : 0 };
  return true;
}
function complete(s, b) {
  const rec = RECIPE[b.recipe], c = b.cycle, outs = outputsFor(s, b, rec, c.k), ql = qualityOf(s, b, c.ql);
  const val = outs.reduce((a, o) => a + o.q * RES[o.res].base, 0) || 1;
  b.pending = outs.map(o => ({ ...o, cost: c.cost * (o.q * RES[o.res].base / val) / Math.max(o.q, 1e-9), ql }));
  b.cycle = null; storePending(s, b);
}
function storePending(s, b) {
  if (!b.pending) return true;
  const elec = b.pending.filter(o => RES[o.res].storage === "enerji"), goods = b.pending.filter(o => RES[o.res].storage !== "enerji");
  for (const o of elec) produceEnergy(s, o.q);
  if (goods.length && !fits(s, b.region, goods)) { b.pending = goods; b.status = STATUS.full; return false; }
  for (const o of goods) {
    const before = s.stats.producedBy[o.res] || 0;
    add(s, b.region, o.res, o.q, o.cost, o.ql); s.stats.produced += o.q; s.stats.producedBy[o.res] = before + o.q; b.made += o.q;
    questEvent(s, "produce", { res: o.res, q: o.q }); if (!before) questEvent(s, "newProduct", { res: o.res });
    addXp(s, o.q * RES[o.res].base / 2000);
  }
  b.pending = null; b.status = STATUS.done; return true;
}
export function productionHour(s) {
  const effW = workerEfficiency(s), list = Object.values(s.buildings).filter(b => b.kind !== "warehouse");
  list.sort((a, b) => (b.kind === "power") - (a.kind === "power") || b.priority - a.priority || (a.id < b.id ? -1 : 1));
  const wear = BAL.wearPerDay / 24 * (1 - staffEff(s, "teknisyen")) * (has(s, "yz_kontrol") ? 0.75 : 1);
  for (const b of list) {
    const t = FT[b.type], rec = RECIPE[b.recipe];
    if (!b.on) { b.status = STATUS.off; continue; }
    if (b.condition < 15) {
      if (b.autoMaint && canSpendOperating(s, maintCost(s, b))) maintain(s, b.id);
      else { b.status = STATUS.maint; continue; }
    }
    if (b.pending && !storePending(s, b)) continue;
    if (!b.cycle) {
      if (effW <= 0.001) { b.status = STATUS.noWorkers; continue; }
      if (!start(s, b, effW)) { drawEnergy(s, b.region, t.idleMwh); continue; }
    }
    const c = b.cycle, mwh = rec.mwh * c.k * (1 - energySave(s)) / c.total + t.idleMwh;
    if (!drawEnergy(s, b.region, mwh)) { b.status = STATUS.power; continue; }
    c.cost += mwh * elecPrice(s, b.region);
    c.left -= 1; b.condition = Math.max(0, b.condition - wear * Math.sqrt(c.k)); b.status = STATUS.run;
    if (b.autoMaint && b.condition < 50 && canSpendOperating(s, maintCost(s, b) * 3)) maintain(s, b.id);
    if (c.left <= 1e-9) complete(s, b);
  }
}
// Günlük sabit fabrika giderleri.
export function factoriesDay(s) {
  let m = 0;
  for (const b of Object.values(s.buildings)) if (b.kind !== "warehouse") m += FT[b.type].maint * (1 + 0.15 * (b.machines - 1)) * (1 + 0.1 * (b.level - 1));
  if (m) spend(s, m, "Fabrika bakımı");
}
// Arayüz için: bir çevrimin tahmini birim maliyeti ve kârı (anlık piyasa fiyatlarıyla).
export function recipeEconomics(s, b, recId) {
  const rec = RECIPE[recId || b.recipe], k = 1, price = id => s.market[id].price;
  const inCost = Object.entries(rec.in).reduce((a, [id, q]) => a + q * (item(s, b.region, id).q > 0 ? item(s, b.region, id).cost : price(id)), 0);
  const energy = rec.mwh * (1 - energySave(s)) * elecPrice(s, b.region), labor = rec.labor;
  const outs = outputsFor(s, b, rec, k), outVal = outs.reduce((a, o) => a + o.q * price(o.res), 0);
  const hours = rec.hours / speedOf(s, b);
  return { inCost, energy, labor, cost: inCost + energy + labor, outVal, profit: outVal - inCost - energy - labor, hours, perDay: (outVal - inCost - energy - labor) * 24 / hours * b.machines, outs };
}

// Otomatik tedarik ve satış (her 6 oyun saatinde): girdiyi yaklaşık bir günlük ihtiyaçta tutar, çıktıyı "elde tut" miktarının üstünde satar.
export function automationTick(s, market) {
  for (const b of Object.values(s.buildings)) {
    if (b.kind === "warehouse" || !b.recipe || !b.on) continue;
    const rec = RECIPE[b.recipe], cyclesPerDay = 24 / (rec.hours / speedOf(s, b));
    if (b.autoBuy) for (const [id, q] of Object.entries(rec.in)) {
      const r = RES[id], incoming = s.shipments.filter(x => x.to === b.region).reduce((a, sh) => a + sh.items.filter(i => i.res === id).reduce((u, i) => u + i.q, 0), 0);
      const want = q * b.machines * cyclesPerDay * (b.buyDays || 1) - qty(s, b.region, id) - incoming;
      if (want <= q * 0.5) continue;
      const room = freeFor(s, b.region, id) / Math.max(r.vol, 1e-6) * 0.7, n = Math.floor(Math.min(want, room) * 10) / 10;
      if (n >= q * 0.5) { const low = qty(s, b.region, id) < q * b.machines; const res = market.buy(s, id, n, b.region, low ? "hizli" : "standart"); b.autoMsg = res.ok ? null : res.msg; }
    }
    if (b.autoSell) for (const id of Object.keys(rec.out)) {
      const r = RES[id]; if (r.storage === "enerji") continue;
      const extra = qty(s, b.region, id) - (b.keep?.[id] || 0);
      if (extra >= 1 || (r.vol < 0.1 && extra >= 10)) { const res = market.sell(s, id, Math.floor(extra * 10) / 10, b.region); if (!res.ok) b.autoMsg = res.msg; }
    }
  }
}
