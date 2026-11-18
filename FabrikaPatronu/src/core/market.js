// MarketManager — arz-talebe bağlı dünya piyasası, emirler ve fiyat geçmişi.
import { RESOURCES, RES } from "../data/resources.js";
import { BAL } from "../data/balance.js";
import { RG } from "../data/regions.js";
import { SUPPLIERS } from "../data/vehicles.js";
import { range, rnd } from "./rng.js";
import { clamp, day, note, uid } from "./util.js";
import { prodMul, consMul, priceMul, transportMul, specialEffect } from "./events.js";
import { freeFor, reserve } from "./warehouse.js";
import { add, remove, qty } from "./inventory.js";
import { earn, spend, canSpendOperating } from "./finance.js";
import { staffEff } from "./employees.js";
import { questEvent } from "./quests.js";
import { addXp } from "./progress.js";

export const tradeable = r => r.storage !== "enerji";
const target = r => r.cons * BAL.targetStockDays;
export function rawPrice(r, stock) { return clamp(r.base * Math.pow(target(r) / Math.max(stock, target(r) * 0.05), r.elastic), r.min, r.max); }

export function initMarket(s) {
  for (const r of RESOURCES) {
    const p = r.base;
    s.market[r.id] = { stock: target(r), price: p, prev: p, open: p, hist: { h: [p], q: [p], d: [p] }, vol: 0 };
  }
}
export const price = (s, id) => s.market[id].price * priceMul(s, id);
export const change = (s, id) => price(s, id) / (s.market[id].open * priceMul(s, id)) - 1;

// Bir saatlik piyasa adımı: üretici ve tüketicinin fiyata tepkisi, gürültü, olaylar, yumuşatılmış fiyat.
export function marketHour(s) {
  const h = (s.time.min / 60) | 0;
  for (const r of RESOURCES) {
    const m = s.market[r.id], T = target(r), dev = (T - m.stock) / T;
    const prod = r.prod * prodMul(s, r) * Math.max(0.2, 1 + BAL.supplyResponse * dev) * (1 + (rnd(s) - 0.5) * 2 * BAL.noise);
    const cons = r.cons * consMul(s, r) * Math.max(0.2, 1 - BAL.demandResponse * dev) * (1 + (rnd(s) - 0.5) * 2 * BAL.noise);
    m.stock = Math.max(T * 0.05, m.stock + (prod - cons) / 24);
    const want = rawPrice(r, m.stock);
    m.price = m.price + (want - m.price) * BAL.priceSmoothing;
    m.hist.h.push(m.price); if (m.hist.h.length > 24) m.hist.h.shift();
    if (h % 4 === 0) { m.hist.q.push(m.price); if (m.hist.q.length > 42) m.hist.q.shift(); }
  }
}
export function marketDay(s) {
  for (const r of RESOURCES) { const m = s.market[r.id]; m.hist.d.push(m.price); if (m.hist.d.length > 365) m.hist.d.shift(); m.prev = m.open; m.open = m.price; m.vol = 0; }
}
// Emrin piyasayı itmesi: stok parça parça değişir, fiyat oranı aynı ölçüde kayar.
function impact(s, id, dq) {
  const r = RES[id], m = s.market[id], before = rawPrice(r, m.stock);
  m.stock = Math.max(target(r) * 0.05, m.stock + dq);
  m.price *= rawPrice(r, m.stock) / before; m.vol += Math.abs(dq);
}
function walk(s, id, q, side, apply) {
  const r = RES[id], m = s.market[id], n = BAL.slippageChunks, dq = q / n;
  let st = m.stock, p = m.price, sum = 0;
  for (let i = 0; i < n; i++) {
    const before = rawPrice(r, st); st = Math.max(target(r) * 0.05, st + (side === "buy" ? -dq : dq));
    const after = rawPrice(r, st), mid = p * (1 + (after / before - 1) / 2); sum += mid * dq; p *= after / before;
  }
  if (apply) impact(s, id, side === "buy" ? -q : q);
  return sum / q * priceMul(s, id);
}
const commissionRate = (s, region) => BAL.commission * (RG[region].commissionMod || 1) * (specialEffect(s, "commission") || 1);
export function deliveryFee(s, id, q, region, perUnit = 180) {
  const r = RES[id], g = RG[region], fm = g.feeMod[id] ?? g.feeMod["*"] ?? 1;
  return r.vol * q * perUnit * fm * transportMul(s) * (1 - staffEff(s, "lojistik")) * (s.research.done.lojistik_opt ? 0.85 : 1);
}
// Teklif hesabı. side: buy|sell. Kayma dahil ortalama birim fiyat, komisyon, vergi, nakliye ve toplam.
export function quote(s, id, q, side, region, supplierId = "standart") {
  const sup = SUPPLIERS.find(x => x.id === supplierId) || SUPPLIERS[0], mid = price(s, id);
  const avg = walk(s, id, q, side, false), half = BAL.spread / 2;
  const unit = side === "buy" ? avg * (1 + half) * (1 + sup.priceMod) : avg * (1 - half);
  const gross = unit * q, commission = gross * commissionRate(s, region), tax = gross * BAL.tradeTax;
  const fee = deliveryFee(s, id, q, region, side === "buy" ? 110 : 70);
  const total = side === "buy" ? gross + commission + tax + fee : gross - commission - tax - fee;
  const hours = side === "buy" ? Math.max(2, Math.round(sup.hours * (RG[region].hourMod || 1))) : 0;
  return { id, q, side, mid, unit, gross, commission, tax, fee, total, perUnit: total / q, impact: avg / mid - 1, hours, supplier: sup.id, quality: clamp(RES[id].quality + sup.quality, 10, 100) };
}
export function buy(s, id, q, region, supplierId = "standart") {
  const r = RES[id]; if (!r || !tradeable(r) || !(q > 0)) return { ok: false, msg: "Geçersiz emir" };
  if (!s.regions[region].owned) return { ok: false, msg: "Bu bölge açık değil" };
  if (freeFor(s, region, id) < r.vol * q - 1e-9) return { ok: false, msg: "Depoda yer yok" };
  const qt = quote(s, id, q, "buy", region, supplierId);
  if (!canSpendOperating(s, qt.total)) return { ok: false, msg: "Nakit ve kredi limiti yetersiz" };
  walk(s, id, q, "buy", true);
  spend(s, qt.gross, "Hammadde alımı"); spend(s, qt.commission + qt.tax, "Komisyon ve vergi"); spend(s, qt.fee, "Nakliye");
  const unitCost = qt.total / q;
  const sh = { id: uid(s), kind: "purchase", from: "piyasa", to: region, items: [{ res: id, q, cost: unitCost, ql: qt.quality }], depart: s.time.min, arrive: s.time.min + qt.hours * 60, mode: "tedarikci", label: qt.supplier };
  s.shipments.push(sh); reserve(s, region, id, q);
  s.stats.bought += q; questEvent(s, "buy", { res: id, q });
  return { ok: true, quote: qt, shipment: sh };
}
export function sell(s, id, q, region) {
  const r = RES[id]; if (!r || !tradeable(r) || !(q > 0)) return { ok: false, msg: "Geçersiz emir" };
  if (qty(s, region, id) < q - 1e-9) return { ok: false, msg: "Stok yetersiz" };
  const qt = quote(s, id, q, "sell", region);
  if (qt.total <= 0) return { ok: false, msg: "Satış bedeli nakliyeyi karşılamıyor" };
  const lot = remove(s, region, id, q); walk(s, id, q, "sell", true);
  earn(s, qt.gross, "Satış"); spend(s, qt.commission + qt.tax, "Komisyon ve vergi"); spend(s, qt.fee, "Nakliye");
  const profit = qt.total - lot.cost * q;
  s.stats.sold += q; s.stats.profitToday += profit; addXp(s, Math.max(0, profit) / 40 + qt.gross / 2000);
  questEvent(s, "sell", { res: id, q }); questEvent(s, "profit", { value: profit });
  return { ok: true, quote: qt, profit, cost: lot.cost };
}
// Limit emirleri: fiyat sınırı geçilince gerçekleşir, 7 gün sonra düşer.
export function placeLimit(s, id, side, q, limit, region, supplierId = "standart") {
  if (!(q > 0) || !(limit > 0)) return { ok: false, msg: "Geçersiz emir" };
  const o = { id: uid(s), res: id, side, q, limit, region, supplier: supplierId, expires: s.time.min + 7 * 1440, made: s.time.min };
  s.orders.push(o); return { ok: true, order: o };
}
export function cancelLimit(s, oid) { s.orders = s.orders.filter(o => o.id !== oid); }
// Toplu sipariş: bugünkü fiyattan %2 indirimle, her gün bir parti teslim. Bedel teslim günü ödenir.
export function placeBulk(s, id, perDay, days, region, supplierId = "standart") {
  const qt = quote(s, id, perDay, "buy", region, supplierId);
  const o = { id: uid(s), res: id, side: "bulk", q: perDay, left: days, unit: qt.perUnit * 0.98, region, supplier: supplierId, made: s.time.min };
  s.orders.push(o); return { ok: true, order: o };
}
export function ordersHour(s) {
  for (const o of [...s.orders]) {
    if (o.side === "bulk") continue;
    if (s.time.min > o.expires) { s.orders = s.orders.filter(x => x !== o); note(s, `${RES[o.res].name} limit emrin süresi doldu.`, "info"); continue; }
    const qt = quote(s, o.res, o.q, o.side, o.region, o.supplier);
    const hit = o.side === "buy" ? qt.perUnit <= o.limit : qt.perUnit >= o.limit;
    if (!hit) continue;
    const res = o.side === "buy" ? buy(s, o.res, o.q, o.region, o.supplier) : sell(s, o.res, o.q, o.region);
    if (res.ok) { s.orders = s.orders.filter(x => x !== o); note(s, `Limit emri gerçekleşti: ${o.q} ${RES[o.res].unit} ${RES[o.res].name} ${o.side === "buy" ? "alındı" : "satıldı"}.`, "good"); }
  }
}
export function bulkDay(s) {
  for (const o of [...s.orders]) {
    if (o.side !== "bulk") continue;
    const r = RES[o.res], cost = o.unit * o.q;
    if (freeFor(s, o.region, o.res) < r.vol * o.q || !canSpendOperating(s, cost)) { note(s, `Toplu sipariş partisi bekletildi (${r.name}): ${!canSpendOperating(s, cost) ? "nakit" : "depo yeri"} yetersiz.`, "warn"); continue; }
    spend(s, cost, "Hammadde alımı");
    const sup = SUPPLIERS.find(x => x.id === o.supplier) || SUPPLIERS[0];
    s.shipments.push({ id: uid(s), kind: "purchase", from: "piyasa", to: o.region, items: [{ res: o.res, q: o.q, cost: o.unit, ql: clamp(r.quality + sup.quality, 10, 100) }], depart: s.time.min, arrive: s.time.min + Math.max(2, sup.hours * (RG[o.region].hourMod || 1)) * 60, mode: "tedarikci", label: sup.id });
    reserve(s, o.region, o.res, o.q); s.stats.bought += o.q; questEvent(s, "buy", { res: o.res, q: o.q });
    if (--o.left <= 0) s.orders = s.orders.filter(x => x !== o);
  }
}
// Geçmişten pencere: "24s" saatlik, "7g" 4 saatlik, "30g" ve "1y" günlük.
export function history(s, id, win) {
  const h = s.market[id].hist;
  if (win === "24s") return h.h.slice(-24);
  if (win === "7g") return h.q.slice(-42);
  if (win === "30g") return h.d.slice(-30).concat([s.market[id].price]);
  return h.d.slice(-365).concat([s.market[id].price]);
}
