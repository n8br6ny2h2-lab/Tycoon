// EventManager — dinamik olaylar ve özel haftalık etkinlik; diğer sistemler etkileri buradaki çarpanlardan okur.
import { EVENTS, EV } from "../data/events.js";
import { RESOURCES, RES, CATEGORIES } from "../data/resources.js";
import { SPECIALS } from "../data/quests.js";
import { chance, irange, pick, weighted, range } from "./rng.js";
import { day, note } from "./util.js";

const active = s => s.events.filter(e => e.until > s.time.min);
function mulFor(s, key, r) {
  let m = 1;
  for (const e of active(s)) { const f = e.effect[key]; if (!f) continue;
    if (f[r.id] !== undefined) m *= f[r.id]; else if (f[r.cat] !== undefined) m *= f[r.cat]; else if (f["*"] !== undefined) m *= f["*"]; }
  return m;
}
export const prodMul = (s, r) => mulFor(s, "prod", r);
export const consMul = (s, r) => mulFor(s, "cons", r);
export function priceMul(s, id) { const r = RES[id]; let m = mulFor(s, "price", r); return m; }
export const transportMul = s => active(s).reduce((a, e) => a * (e.effect.transport || 1), 1);
export const portMul = s => active(s).reduce((a, e) => a * (e.effect.port || 1), 1);
export const researchMul = s => active(s).reduce((a, e) => a * (e.effect.research || 1), 1);
export const interestAdd = s => active(s).reduce((a, e) => a + (e.effect.interest || 0), 0);
export const offersMul = s => active(s).reduce((a, e) => a * (e.effect.offers || 1), 1) * (specialEffect(s, "offers") || 1);
export const gridOk = s => !active(s).some(e => e.effect.grid === false);
export const activeEvents = active;
export function specialEffect(s, key) { return s.special && s.special.effect ? s.special.effect[key] : undefined; }

// Yeni olay: aktif olay sayısı 2'den azsa günlük %20 olasılıkla.
export function eventsDay(s, hooks = {}) {
  s.events = s.events.filter(e => e.until > s.time.min);
  if (s.events.length >= 2 || !chance(s, 0.2)) return null;
  const ev = weighted(s, EVENTS.filter(e => !s.events.some(x => x.id === e.id)));
  const d = irange(s, ev.days[0], ev.days[1]);
  const e = { id: ev.id, name: ev.name, start: s.time.min, until: s.time.min + (ev.hours ? irange(s, ev.hours[0], ev.hours[1]) * 60 : d * 1440), effect: JSON.parse(JSON.stringify(ev.effect)), desc: ev.desc, good: !!ev.good };
  if (ev.target === "raw") { const r = pick(s, RESOURCES.filter(x => ["metal", "enerji", "endustriyel", "orman"].includes(x.cat) && x.storage !== "enerji")); e.effect.prod = { [r.id]: ev.effect.prod.$ }; e.desc = e.desc.replace("{res}", r.name); e.res = r.id; }
  if (ev.target === "cat") { const c = pick(s, Object.keys(CATEGORIES)); e.effect.cons = { [c]: ev.effect.cons.$cat }; e.desc = e.desc.replace("{cat}", CATEGORIES[c]); }
  if (ev.effect.rivalBankrupt) { const alive = s.rivals.filter(r => r.alive); if (alive.length < 4) return null; const rv = pick(s, alive.slice().sort((a, b) => a.capital - b.capital).slice(0, 4)); rv.alive = false; e.desc = e.desc.replace("{rival}", rv.name); hooks.onRivalBankrupt && hooks.onRivalBankrupt(rv); }
  if (ev.effect.bigOffer) hooks.onBigOffer && hooks.onBigOffer();
  s.events.push(e);
  note(s, `${e.name}: ${e.desc}`, e.good ? "good" : "event", { event: e.id });
  return e;
}
export function specialWeek(s) {
  const w = Math.floor(day(s) / 7), sp = SPECIALS[w % SPECIALS.length];
  s.special = { ...sp, week: w, contest: sp.effect.contest ? { res: null, mine: 0 } : null };
  if (sp.id !== "sakin") note(s, `Haftanın etkinliği: ${sp.name}. ${sp.desc}`, "event");
}
