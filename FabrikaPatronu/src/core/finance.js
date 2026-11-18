// FinanceManager — nakit, gelir-gider defteri, kredi, vergi, şirket değeri, iflas ve kurtarma.
import { BAL } from "../data/balance.js";
import { FT } from "../data/factories.js";
import { WT } from "../data/warehouses.js";
import { VH } from "../data/vehicles.js";
import { RES } from "../data/resources.js";
import { RG } from "../data/regions.js";
import { clamp, day, note, uid } from "./util.js";
import { staffEff } from "./employees.js";

export function earn(s, amount, cat) { if (!(amount > 0)) return; s.company.cash += amount; const t = s.ledger.today; t[cat] = (t[cat] || 0) + amount; }
export function spend(s, amount, cat) { if (!(amount > 0)) return; s.company.cash -= amount; const t = s.ledger.today; t[cat] = (t[cat] || 0) - amount; }
export const INCOME_CATS = ["Satış", "Sözleşme", "Enerji satışı", "Ödül", "Kredi", "Varlık satışı"];

export function factoryValue(s, b) {
  const t = FT[b.type], base = t.cost * (RG[b.region].factoryMod || 1);
  return (base + (b.machines - 1) * t.cost * BAL.machineCostRatio + (b.level - 1) * t.cost * 0.3) * 0.7;
}
export function assetValues(s) {
  let fac = 0, wh = 0, stock = 0, veh = 0, land = 0;
  for (const b of Object.values(s.buildings)) {
    if (b.kind === "warehouse") wh += WT[b.type].cost * (1 + 0.25 * (b.level - 1)) * 0.7; else fac += factoryValue(s, b);
  }
  for (const reg in s.inventory) for (const [id, it] of Object.entries(s.inventory[reg])) stock += it.q * s.market[id].price;
  for (const sh of s.shipments) if (sh.kind !== "contract") for (const it of sh.items) stock += it.q * s.market[it.res].price;
  for (const v of s.fleet) veh += VH[v.type].price * 0.6;
  for (const [id, r] of Object.entries(s.regions)) land += r.plots.filter(p => p.owned).length * RG[id].plotPrice * 0.8;
  return { fac, wh, stock, veh, land };
}
export const debt = s => s.loans.reduce((a, l) => a + l.left, 0);
// Kredili hesap: işletme alımlarında (hammadde, enerji) kasa bu kadar eksiye inebilir. Yatırımlar için nakit gerekir.
export const creditLine = s => Math.max(2000000, Math.max(0, s.stats.lastValue || 0) * BAL.overdraftLimit) * Math.max(0.3, s.company.credit / 100);
export const canSpendOperating = (s, amount) => s.company.cash - amount >= -creditLine(s);
export function companyValue(s) { const a = assetValues(s); return s.company.cash + a.fac + a.wh + a.stock + a.veh + a.land - debt(s); }

// Kredi teklifleri: ekonomik faiz ortamı + kredi notu primi + banka farkı − finans yöneticisi indirimi.
const BANKS = [{ id: "devlet", name: "Kalkınma Bankası", spread: 0, maxMul: 0.4, terms: [12, 24] }, { id: "ticari", name: "Ticaret Bankası", spread: 0.004, maxMul: 0.6, terms: [6, 12, 24] }, { id: "hizli", name: "Hızlı Kredi", spread: 0.012, maxMul: 0.3, terms: [3, 6] }];
export function loanOffers(s) {
  const v = Math.max(companyValue(s), 3000000), prem = (100 - s.company.credit) / 100 * 0.012, cut = staffEff(s, "finans");
  const used = debt(s);
  return BANKS.map(b => ({ ...b, rate: Math.max(0.004, s.econ.interest + prem + b.spread - cut), max: Math.max(0, Math.round((v * b.maxMul - used * 0.5) / 100000) * 100000) }));
}
export function takeLoan(s, bankId, amount, months) {
  const o = loanOffers(s).find(x => x.id === bankId); if (!o) return { ok: false, msg: "Banka yok" };
  if (!(amount > 0) || amount > o.max) return { ok: false, msg: `En fazla ${Math.round(o.max).toLocaleString("tr-TR")} ₺` };
  if (!o.terms.includes(months)) return { ok: false, msg: "Geçersiz vade" };
  const r = o.rate, pay = amount * r / (1 - Math.pow(1 + r, -months));
  s.loans.push({ id: uid(s), bank: o.name, amount, left: amount, rate: r, months, paid: 0, pay, start: day(s) });
  earn(s, amount, "Kredi"); return { ok: true, pay };
}
export function repayLoan(s, id) {
  const l = s.loans.find(x => x.id === id); if (!l) return { ok: false };
  const fee = l.left * 0.01; if (s.company.cash < l.left + fee) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, l.left, "Kredi anapara"); spend(s, fee, "Faiz"); s.loans = s.loans.filter(x => x !== l); s.company.credit = clamp(s.company.credit + 3, 0, 100); return { ok: true };
}
// Gün sonu: defter kapanışı, eksi bakiye faizi, iflas takibi.
export function financeDay(s) {
  const t = s.ledger.today, rev = Object.entries(t).filter(([k, v]) => v > 0 && k !== "Kredi").reduce((a, [, v]) => a + v, 0);
  const cost = Object.entries(t).filter(([k, v]) => v < 0 && k !== "Kredi anapara" && k !== "Yatırım").reduce((a, [, v]) => a - v, 0);
  s.ledger.days.push({ day: day(s), rev, cost, net: rev - cost, cats: t }); if (s.ledger.days.length > 90) s.ledger.days.shift();
  for (const [k, v] of Object.entries(t)) s.ledger.month[k] = (s.ledger.month[k] || 0) + v;
  s.ledger.monthProfit += rev - cost; s.ledger.today = {};
  const v = companyValue(s); s.stats.peakValue = Math.max(s.stats.peakValue, v); s.stats.lastValue = v;
  const limit = Math.max(2000000, v * BAL.overdraftLimit);
  if (s.company.cash < 0) { spend(s, -s.company.cash * 0.001, "Faiz"); }
  if (s.company.cash < -limit) {
    s.company.overdraftDays++;
    if (s.company.overdraftDays === 1) note(s, `Kasa eksi bakiye sınırını (−${Math.round(limit / 1e6)} Mn ₺) aştı. ${BAL.overdraftDays} gün içinde düzelmezse iflas süreci başlar.`, "bad");
    if (s.company.overdraftDays >= BAL.overdraftDays && !s.company.rescue) { s.company.rescue = { since: day(s) }; note(s, "İflasın eşiğindesin. Şirket menüsünde kurtarma seçenekleri açıldı.", "bad"); }
    if (s.company.overdraftDays >= BAL.overdraftDays + 5) { s.company.bankrupt = true; note(s, "Şirket iflas etti.", "bad"); }
  } else { s.company.overdraftDays = 0; s.company.rescue = null; }
}
// Ay sonu: kurumlar vergisi, kredi taksitleri, kredi notu.
export function financeMonth(s) {
  const profit = s.ledger.monthProfit;
  if (profit > 0) { const tax = profit * BAL.corporateTax * (1 - Math.min(0.1, staffEff(s, "finans") * 1.7)); spend(s, tax, "Vergi"); s.ledger.lastTax = tax; } else s.ledger.lastTax = 0;
  for (const l of [...s.loans]) {
    const interest = l.left * l.rate, principal = Math.min(l.left, l.pay - interest);
    spend(s, interest, "Faiz"); spend(s, principal, "Kredi anapara"); l.left -= principal; l.paid++;
    if (l.left < 1 || l.paid >= l.months) s.loans = s.loans.filter(x => x !== l);
  }
  s.company.credit = clamp(s.company.credit + (s.company.cash > 0 ? 2 : -6) + (profit > 0 ? 1 : -2), 0, 100);
  s.ledger.months.push({ day: day(s), cats: s.ledger.month, profit, tax: s.ledger.lastTax }); if (s.ledger.months.length > 24) s.ledger.months.shift();
  s.ledger.month = {}; s.ledger.monthProfit = 0;
}
// Kurtarma seçenekleri (iflas eşiğinde): devlet teşvik kredisi (oyunda bir kez), yatırımcı ortaklığı, varlık satışı.
export function rescueOptions(s) {
  const v = Math.max(companyValue(s), 0), o = [];
  if (!s.company.usedStateAid) o.push({ id: "tesvik", name: "Devlet teşvik kredisi", desc: "Faizsiz, 24 ay vadeli kurtarma kredisi. Oyunda bir kez.", amount: Math.max(5000000, -s.company.cash * 1.3) });
  o.push({ id: "ortak", name: "Yatırımcı ortaklığı", desc: "Bir yatırımcı nakit koyar; karşılığında 30 gün boyunca kârının %30'unu alır, itibar −5.", amount: Math.max(4000000, v * 0.25) });
  return o;
}
export function takeRescue(s, id) {
  const o = rescueOptions(s).find(x => x.id === id); if (!o) return { ok: false };
  if (id === "tesvik") { s.company.usedStateAid = true; s.loans.push({ id: uid(s), bank: "Devlet teşviki", amount: o.amount, left: o.amount, rate: 0, months: 24, paid: 0, pay: o.amount / 24, start: day(s) }); }
  if (id === "ortak") { s.company.partner = { until: day(s) + 30, share: 0.3 }; s.company.rep = Math.max(0, s.company.rep - 5); }
  earn(s, o.amount, id === "tesvik" ? "Kredi" : "Ödül"); s.company.overdraftDays = 0; s.company.rescue = null;
  note(s, `${o.name}: ${Math.round(o.amount).toLocaleString("tr-TR")} ₺ kasaya girdi.`, "good"); return { ok: true };
}
export function stockValue(s) { let v = 0; for (const reg in s.inventory) for (const [id, it] of Object.entries(s.inventory[reg])) v += it.q * s.market[id].price; return v; }
