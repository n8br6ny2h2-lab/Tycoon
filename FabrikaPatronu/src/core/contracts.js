// ContractManager — sözleşme kabulü, teslimat, gecikme, ceza, ihale.
import { RES } from "../data/resources.js";
import { RG } from "../data/regions.js";
import { BAL } from "../data/balance.js";
import { CONTRACT_KINDS } from "../data/customers.js";
import { dispatch } from "./logistics.js";
import { qty, item } from "./inventory.js";
import { earn, spend } from "./finance.js";
import { addRep, addXp } from "./progress.js";
import { day, note, uid, level } from "./util.js";
import { range } from "./rng.js";
import { questEvent } from "./quests.js";

export function accept(s, offerId) {
  const o = s.offers.find(x => x.id === offerId); if (!o) return { ok: false, msg: "Teklif yok" };
  if (o.kind === "ihale") return { ok: false, msg: "İhaleye teklif verilir" };
  const active = s.contracts.filter(c => c.status === "active").length;
  if (active >= 4 + level(s)) return { ok: false, msg: "Aynı anda en fazla " + (4 + level(s)) + " sözleşme" };
  s.offers = s.offers.filter(x => x !== o);
  const c = { ...o, id: uid(s), status: "active", done: 0, missed: 0, inTransit: 0, due: day(s) + o.leadDays, started: day(s), auto: false, log: [] };
  s.contracts.push(c); return { ok: true, contract: c };
}
export function decline(s, offerId) { s.offers = s.offers.filter(x => x.id !== offerId); }
// İhale: oyuncu birim fiyat önerir; ihale günü rakip tekliflerle karşılaştırılır, en düşük (kalite yeterliyse) kazanır.
export function bid(s, offerId, unitPrice) {
  const o = s.offers.find(x => x.id === offerId && x.kind === "ihale"); if (!o) return { ok: false };
  if (!(unitPrice > 0) || unitPrice > o.price) return { ok: false, msg: "Teklif tavan fiyatın altında olmalı" };
  const deposit = unitPrice * o.q * 0.02; if (s.company.cash < deposit) return { ok: false, msg: "Teminat için nakit yetersiz" };
  spend(s, deposit, "İhale teminatı"); o.myBid = unitPrice; o.deposit = deposit; return { ok: true };
}
export function tendersDay(s, rivalBid) {
  for (const o of [...s.offers]) {
    if (o.kind !== "ihale" || o.tenderDay > day(s)) continue;
    s.offers = s.offers.filter(x => x !== o);
    if (!o.myBid) continue;
    const rivals = rivalBid(s, o), best = Math.min(...rivals.map(b => b.price));
    earn(s, o.deposit, "İhale teminatı");
    if (o.myBid < best) {
      const c = { ...o, id: uid(s), price: o.myBid, status: "active", done: 0, missed: 0, inTransit: 0, due: day(s) + o.leadDays, started: day(s), auto: false, log: [] };
      s.contracts.push(c); s.stats.tendersWon++; addRep(s, 3);
      note(s, `İhaleyi kazandın: ${o.cust}, ${o.q} ${RES[o.res].unit} ${RES[o.res].name}, ${Math.round(o.myBid).toLocaleString("tr-TR")} ₺/${RES[o.res].unit}.`, "good");
    } else note(s, `İhaleyi ${rivals.find(b => b.price === best).name} kazandı (${Math.round(best).toLocaleString("tr-TR")} ₺). Senin teklifin ${Math.round(o.myBid).toLocaleString("tr-TR")} ₺.`, "bad");
  }
}
// Teslimat: bölgeden müşteriye sevkiyat; varışta ödeme.
export function deliver(s, contractId, region, mode = "nakliyeci") {
  const c = s.contracts.find(x => x.id === contractId && x.status === "active"); if (!c) return { ok: false, msg: "Sözleşme yok" };
  if (c.inTransit) return { ok: false, msg: "Bu partinin sevkiyatı yolda" };
  if (c.export && !["liman", "uluslararasi"].includes(region)) return { ok: false, msg: "İhracat liman ya da ticaret merkezinden gönderilir" };
  if (qty(s, region, c.res) < c.q - 1e-9) return { ok: false, msg: `${RG[region].short}: ${RES[c.res].name} yetersiz (${c.q} gerekir)` };
  if (item(s, region, c.res).ql < c.minQ - 0.5) return { ok: false, msg: `Kalite yetersiz: ${Math.round(item(s, region, c.res).ql)} < ${c.minQ}` };
  const r = dispatch(s, { from: region, to: c.city, items: [{ res: c.res, q: c.q }], mode, kind: "contract", contract: c.id });
  if (r.ok) c.inTransit = r.shipment.id;
  return r;
}
export function onContractArrival(s, sh) {
  const c = s.contracts.find(x => x.id === sh.contract); if (!c) return;
  c.inTransit = 0; const it = sh.items[0], d = day(s), late = d > c.due;
  const qBonus = Math.min(0.05, Math.max(0, (it.ql - c.minQ) * 0.002)), exp = c.export ? (RG[sh.from] && RG[sh.from].exportBonus) || 0 : 0;
  let pay = c.price * it.q * (1 + qBonus + exp); if (late) pay *= 1 - BAL.latePenalty;
  earn(s, pay, "Sözleşme"); c.done++; s.stats.deliveries++; if (c.export) { s.stats.exports++; questEvent(s, "export", {}); }
  questEvent(s, "deliveries", {}); addXp(s, pay / 400); addRep(s, late ? -2 : 1);
  const profit = pay - it.cost * it.q; s.stats.profitToday += profit; questEvent(s, "profit", { value: profit });
  c.log.push({ d, pay, late, ql: Math.round(it.ql) });
  note(s, `${c.cust}: ${it.q} ${RES[c.res].unit} ${RES[c.res].name} teslim edildi${late ? " (gecikmeli, %10 ceza)" : ""}. +${Math.round(pay).toLocaleString("tr-TR")} ₺`, late ? "warn" : "good");
  if (c.done + c.missed >= c.deliveries) { c.status = "done"; addRep(s, c.missed ? 0 : 2); }
  else c.due = Math.max(c.due, d) + (c.every || 0);
}
export function contractsDay(s) {
  const d = day(s);
  for (const c of s.contracts) {
    if (c.status !== "active") continue;
    if (c.auto && !c.inTransit && d >= c.due - Math.max(1, Math.floor(c.leadDays / 2))) {
      const regs = Object.keys(s.regions).filter(r => s.regions[r].owned && qty(s, r, c.res) >= c.q && item(s, r, c.res).ql >= c.minQ && (!c.export || ["liman", "uluslararasi"].includes(r)));
      if (regs.length) deliver(s, c.id, regs[0]);
    }
    if (!c.inTransit && d > c.due + BAL.lateGraceDays) {
      c.missed++; const fine = c.price * c.q * BAL.latePenalty; spend(s, fine, "Sözleşme cezası"); addRep(s, -4);
      note(s, `${c.cust}: teslimat kaçırıldı. Ceza ${Math.round(fine).toLocaleString("tr-TR")} ₺, itibar −4.`, "bad");
      if (c.missed >= 2 || c.done + c.missed >= c.deliveries) { c.status = c.missed >= 2 ? "cancelled" : "done"; if (c.missed >= 2) { addRep(s, -6); note(s, `${c.cust} sözleşmeyi feshetti.`, "bad"); } }
      else c.due = d + (c.every || 3);
    }
  }
  s.contracts = s.contracts.filter(c => c.status === "active" || d - (c.log.length ? c.log[c.log.length - 1].d : c.started) < 10);
}
