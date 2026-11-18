// EconomyManager — enerji şebekesi (kendi üretim, şebekeden alım, fazla satış), ekonomik döngü ve faiz ortamı.
import { BAL } from "../data/balance.js";
import { RG } from "../data/regions.js";
import { price } from "./market.js";
import { earn, spend } from "./finance.js";
import { gridOk, interestAdd } from "./events.js";
import { day } from "./util.js";

// Saat başında sıfırlanır; santraller ekler, fabrikalar tüketir.
export function energyReset(s) { s.energy = { own: 0, used: 0, bought: 0, sold: 0, cost: 0, byRegion: {} }; }
export const gridAvailable = s => gridOk(s);
export const elecPrice = (s, region) => price(s, "elektrik") * (RG[region].elecMod || 1);
// Bir tüketici talebi: önce kendi üretim havuzundan, yoksa şebekeden. Kesintide şebeke yok → false.
export function drawEnergy(s, region, mwh) {
  const e = s.energy; if (mwh <= 0) return true;
  const fromOwn = Math.min(mwh, e.own - e.used); if (fromOwn > 0) e.used += fromOwn;
  const rest = mwh - Math.max(0, fromOwn);
  if (rest <= 1e-9) return true;
  if (!gridAvailable(s)) { if (fromOwn > 0) e.used -= fromOwn; return false; }
  const c = rest * elecPrice(s, region); spend(s, c, "Enerji"); e.bought += rest; e.cost += c; return true;
}
export function produceEnergy(s, mwh) { s.energy.own += mwh; }
export function energySettle(s) {
  const e = s.energy, surplus = e.own - e.used;
  if (surplus > 0 && gridAvailable(s)) { const v = surplus * price(s, "elektrik") * BAL.gridSellRatio; earn(s, v, "Enerji satışı"); e.sold = surplus; }
  s.energyLast = { ...e };
}
// Ekonomik döngü: 120 günlük dalga + olaylar → aylık faiz.
export function economyDay(s) {
  const d = day(s); s.econ.cycle = Math.sin(d / 120 * Math.PI * 2);
  s.econ.interest = Math.max(0.008, BAL.baseInterest + 0.006 * s.econ.cycle + interestAdd(s));
  s.econ.state = s.econ.cycle > 0.35 ? "Büyüme" : s.econ.cycle < -0.35 ? "Daralma" : "Durgun";
}
