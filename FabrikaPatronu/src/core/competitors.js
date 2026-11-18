// CompetitorManager — 10 yapay zekâ rakip: üretim kapasitesi piyasa arzını değiştirir, tüccarlar ucuzdan alıp pahalıdan satar,
// sermaye büyür ya da erir, iflas eder; ihalelere teklif verir; sıralama ve pazar payı hesaplanır.
import { RES } from "../data/resources.js";
import { range, chance, pick } from "./rng.js";
import { note, day } from "./util.js";
import { companyValue } from "./finance.js";

const SHARE = { ucuz: 0.1, kalite: 0.06, hacim: 0.08, uzman: 0.09 };
const MARGIN = { ucuz: 0.05, kalite: 0.13, hacim: 0.04, uzman: 0.09 };
const NEWCOMERS = ["Yeni Ufuk Sanayi", "Doğu Metal", "Atılım Kimya", "Pusula Lojistik ve Ticaret", "Simge Gıda", "Orion Teknoloji"];
export function initRivals(s, defs) {
  s.rivals = defs.map(d => ({ id: d.id, name: d.name, strategy: d.strategy, focus: d.focus, color: d.color, capital: d.capital, alive: true, factor: 1, rev: 0, value: d.capital * 1.3, factories: 3 + Math.round(d.capital / 2.5e8), stock: {} }));
}
export function competitorsDay(s) {
  for (const rv of s.rivals) {
    const share = SHARE[rv.strategy] / Math.max(1, rv.focus.length / 3);
    let rev = 0;
    for (const id of rv.focus) {
      const r = RES[id], m = s.market[id], nominal = r.prod * share, rel = m.price / r.base;
      if (!rv.alive) { m.stock -= nominal * 0.5; continue; } // iflas eden rakibin arzı piyasadan çekilir
      if (rel > 1.05 && rv.capital > 0) rv.factor = Math.min(1.7, rv.factor * 1.012);
      if (rel < 0.92) rv.factor = Math.max(0.5, rv.factor * 0.988);
      const actual = nominal * rv.factor * range(s, 0.94, 1.06);
      m.stock += actual - nominal; rev += actual * m.price;
      if (rv.strategy === "hacim") { // tüccar: ucuzken stok yapar, pahalıyken satar
        const lot = r.prod * 0.03, have = rv.stock[id] || 0;
        if (rel < 0.95 && rv.capital > lot * m.price * 5) { m.stock -= lot; rv.stock[id] = have + lot; rv.capital -= lot * m.price; }
        else if (rel > 1.05 && have > 0) { const q = Math.min(have, lot); m.stock += q; rv.stock[id] = have - q; rv.capital += q * m.price; }
      }
    }
    if (!rv.alive) continue;
    rv.rev = rv.rev * 0.9 + rev * 0.1;
    rv.capital += rev * MARGIN[rv.strategy] - rv.capital * 0.0004 + rv.capital * range(s, -0.004, 0.0042);
    if (rv.capital > rv.value * 0.9 && chance(s, 0.03)) rv.factories++;
    rv.value = rv.capital * 1.15 + rv.factories * 2.5e7 + Object.entries(rv.stock).reduce((a, [id, q]) => a + q * s.market[id].price, 0);
    if (rv.capital < 0) { rv.alive = false; note(s, `${rv.name} iflas etti. Ürettiği ürünlerde arz azalacak.`, "event"); }
  }
  // Yeni rakip girişi
  if (s.rivals.filter(r => r.alive).length < 8 && chance(s, 0.03)) {
    const dead = s.rivals.find(r => !r.alive), name = pick(s, NEWCOMERS.filter(n => !s.rivals.some(r => r.name === n)));
    if (dead && name) { Object.assign(dead, { name, alive: true, capital: 2e8, factor: 0.8, factories: 2, rev: 0, stock: {} }); note(s, `Sektöre yeni bir rakip girdi: ${name}.`, "event"); }
  }
  ranking(s);
}
export function ranking(s) {
  const me = { id: "me", name: s.company.name, value: s.stats.lastValue ?? companyValue(s), me: true };
  const rows = [me, ...s.rivals.filter(r => r.alive)].sort((a, b) => b.value - a.value);
  s.stats.rank = rows.indexOf(me) + 1; return rows;
}
// Pazar payı: son dönem cirosuna göre.
export function marketShare(s) {
  const myRev = s.ledger.days.slice(-30).reduce((a, d) => a + ((d.cats["Satış"] || 0) + (d.cats["Sözleşme"] || 0)), 0) / Math.max(1, Math.min(30, s.ledger.days.length));
  const tot = myRev + s.rivals.filter(r => r.alive).reduce((a, r) => a + r.rev, 0);
  return { me: tot ? myRev / tot : 0, rows: s.rivals.filter(r => r.alive).map(r => ({ name: r.name, color: r.color, share: tot ? r.rev / tot : 0 })) };
}
export function rivalBids(s, o) {
  const m = s.market[o.res].price, out = [{ name: "Diğer firmalar", price: m * range(s, 0.99, 1.1) }];
  for (const rv of s.rivals) if (rv.alive && (rv.focus.includes(o.res) || chance(s, 0.15))) out.push({ name: rv.name, price: m * (1 + range(s, -0.03, 0.08) + (rv.strategy === "ucuz" ? -0.02 : rv.strategy === "kalite" ? 0.03 : 0)) });
  return out;
}
