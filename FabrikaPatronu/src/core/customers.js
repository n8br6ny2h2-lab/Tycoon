// CustomerManager — müşteri tipleri ve günlük teklif üretimi.
import { CUSTOMER_TYPES, CT, CONTRACT_KINDS, CUSTOMER_NAMES } from "../data/customers.js";
import { MARKET_CITIES } from "../data/regions.js";
import { RES } from "../data/resources.js";
import { range, irange, pick, chance } from "./rng.js";
import { day, level, uid, note } from "./util.js";
import { staffEff } from "./employees.js";
import { offersMul, specialEffect } from "./events.js";

const exportReady = s => Object.entries(s.regions).some(([id, r]) => r.owned && ["liman", "uluslararasi"].includes(id));
export function makeOffer(s, opts = {}) {
  const lv = level(s), types = CUSTOMER_TYPES.filter(t => lv >= t.level && (!t.export || exportReady(s)));
  const t = opts.type ? CT[opts.type] : pick(s, types);
  const kind = opts.kind || pick(s, t.kinds), K = CONTRACT_KINDS[kind];
  const res = opts.res || pick(s, t.products), r = RES[res], m = s.market[res];
  const big = opts.big ? 2.5 : 1, frac = range(s, t.qty[0], t.qty[1]) * big * (kind === "ihale" ? 1.5 : 1);
  let q = r.cons * frac * (1 + lv * 0.08);
  q = r.vol < 0.1 ? Math.max(10, Math.round(q / 10) * 10) : Math.max(1, Math.round(q * (q > 20 ? 1 : 10)) / (q > 20 ? 1 : 10));
  const markup = range(s, t.markup[0], t.markup[1]) + staffEff(s, "satis") + (opts.big ? 0.05 : 0);
  const city = t.export ? "yurtdisi" : pick(s, MARKET_CITIES.filter(c => !c.export)).id;
  const o = { id: uid(s), type: t.id, cust: pick(s, CUSTOMER_NAMES[t.id]), kind, res, q, deliveries: K.deliveries, every: K.every,
    price: Math.round(m.price * (1 + markup)), minQ: Math.round(range(s, t.minQ[0], t.minQ[1])), city, leadDays: irange(s, K.days[0], K.days[1]),
    expires: day(s) + (kind === "ihale" ? 3 : 2), export: !!t.export, big: !!opts.big };
  if (kind === "ihale") { o.price = Math.round(m.price * 1.12); o.tenderDay = day(s) + 3; o.bids = []; }
  return o;
}
export function customersDay(s) {
  s.offers = s.offers.filter(o => o.expires >= day(s) && !(o.kind === "ihale" && o.tenderDay < day(s)));
  const n = Math.max(1, Math.round((2 + s.staff.satis.n * (1 + 0.1 * (s.staff.satis.lvl - 1)) + s.company.rep / 40) * offersMul(s)));
  for (let i = 0; i < n && s.offers.length < 14; i++) s.offers.push(makeOffer(s));
  if (specialEffect(s, "tenders") && level(s) >= 2) s.offers.push(makeOffer(s, { type: "devlet", kind: "ihale" }));
}
export function bigOffer(s) { const o = makeOffer(s, { big: true, kind: chance(s, 0.5) ? "aylik" : "tek" }); s.offers.unshift(o); note(s, `Büyük müşteri: ${o.cust} ${o.q} ${RES[o.res].unit} ${RES[o.res].name} istiyor (piyasanın üstünde).`, "good"); }
