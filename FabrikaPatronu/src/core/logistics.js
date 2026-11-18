// LogisticsManager — mesafe, nakliye modları (filo ya da kiralık), sevkiyatlar, filo, otomatik rotalar.
import { VEHICLES, VH, CARRIER } from "../data/vehicles.js";
import { RG, CITY, distKm } from "../data/regions.js";
import { RES } from "../data/resources.js";
import { spend, earn } from "./finance.js";
import { freeFor } from "./warehouse.js";
import { add, remove, qty } from "./inventory.js";
import { staffEff } from "./employees.js";
import { transportMul, portMul } from "./events.js";
import { has, level, note, uid, day } from "./util.js";

export const place = id => RG[id] || CITY[id];
export const km = (a, b) => distKm(place(a), place(b));
const infra = (id, need) => { const p = place(id); if (!need) return true; if (p.export) return need === "port" || need === "airport"; return !!(RG[id] && RG[id][need]); };
const costMul = s => transportMul(s) * (1 - staffEff(s, "lojistik")) * (has(s, "lojistik_opt") ? 0.85 : 1);
export function modesFor(s, from, to, vol) {
  const d = km(from, to), out = [];
  const cHours = Math.max(CARRIER.minHours, d / CARRIER.speed + 2);
  out.push({ mode: "nakliyeci", name: CARRIER.name, cost: vol * (CARRIER.perUnit + CARRIER.perUnitKm * d) * costMul(s), hours: cHours, n: 0, ok: true });
  for (const v of VEHICLES) {
    if (!infra(from, v.needs) || !infra(to, v.needs)) continue;
    const idle = s.fleet.filter(f => f.type === v.id && f.home === from && (!f.busyUntil || f.busyUntil <= s.time.min));
    const n = Math.ceil(vol / v.cap);
    const hours = (d / v.speed + 2) * (v.id === "gemi" ? portMul(s) : 1);
    out.push({ mode: v.id, name: v.name, cost: n * v.fuel * d * 2 * costMul(s), hours, n, idle: idle.length, ok: idle.length >= n && n > 0 });
  }
  return out;
}
// Sevkiyat gönder. items: [{res, q}] (kaynak bölgeden çekilir). purpose: transfer | contract.
export function dispatch(s, { from, to, items, mode = "nakliyeci", kind = "transfer", contract = null }) {
  const vol = items.reduce((a, i) => a + RES[i.res].vol * i.q, 0);
  for (const i of items) if (qty(s, from, i.res) < i.q - 1e-9) return { ok: false, msg: `${RES[i.res].name} stoğu yetersiz` };
  if (kind === "transfer") { for (const i of items) if (freeFor(s, to, i.res) < RES[i.res].vol * i.q - 1e-9) return { ok: false, msg: "Hedef depoda yer yok" }; }
  const m = modesFor(s, from, to, vol).find(x => x.mode === mode); if (!m) return { ok: false, msg: "Bu taşıma yolu kullanılamaz" };
  if (!m.ok) return { ok: false, msg: `${m.name}: ${m.n} araç gerekir, ${m.idle || 0} boşta` };
  if (s.company.cash < m.cost) return { ok: false, msg: "Nakit yetersiz" };
  const lots = items.map(i => { const l = remove(s, from, i.res, i.q); return { res: i.res, q: i.q, cost: l.cost + m.cost / vol * RES[i.res].vol, ql: l.ql }; });
  spend(s, m.cost, "Nakliye");
  const arrive = s.time.min + Math.round(m.hours * 60);
  let vehicles = [];
  if (mode !== "nakliyeci") {
    const idle = s.fleet.filter(f => f.type === mode && f.home === from && (!f.busyUntil || f.busyUntil <= s.time.min)).slice(0, m.n);
    for (const f of idle) f.busyUntil = s.time.min + Math.round(m.hours * 2 * 60); vehicles = idle.map(f => f.id);
  }
  const sh = { id: uid(s), kind, from, to, items: lots, depart: s.time.min, arrive, mode, vehicles, contract, cost: m.cost };
  s.shipments.push(sh); return { ok: true, shipment: sh, cost: m.cost, hours: m.hours };
}
export function shipmentsHour(s, onContractArrival) {
  for (const sh of [...s.shipments]) {
    if (sh.arrive > s.time.min) continue;
    s.shipments = s.shipments.filter(x => x !== sh);
    if (sh.kind === "contract") { onContractArrival && onContractArrival(s, sh); continue; }
    for (const i of sh.items) add(s, sh.to, i.res, i.q, i.cost, i.ql);
  }
}
export function buyVehicle(s, type, home) {
  const v = VH[type]; if (!v) return { ok: false };
  if (v.unlock && level(s) < v.unlock.level) return { ok: false, msg: `Şirket seviyesi ${v.unlock.level} gerekir` };
  if (v.research && !has(s, v.research)) return { ok: false, msg: "Araştırma gerekir" };
  if (!s.regions[home].owned) return { ok: false, msg: "Bölge açık değil" };
  if (v.needs && !RG[home][v.needs]) return { ok: false, msg: v.needs === "rail" ? "Bu bölgede demiryolu yok" : v.needs === "port" ? "Bu bölgede liman yok" : "Bu bölgede havalimanı yok" };
  if (s.company.cash < v.price) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, v.price, "Yatırım"); s.fleet.push({ id: uid(s), type, home, busyUntil: 0, bought: s.time.min }); return { ok: true };
}
export function sellVehicle(s, id) {
  const f = s.fleet.find(x => x.id === id); if (!f) return { ok: false };
  if (f.busyUntil > s.time.min) return { ok: false, msg: "Araç seferde" };
  earn(s, VH[f.type].price * 0.55, "Varlık satışı"); s.fleet = s.fleet.filter(x => x !== f); return { ok: true };
}
export function fleetDay(s) { const m = s.fleet.reduce((a, f) => a + VH[f.type].maint, 0); if (m) spend(s, m, "Filo bakımı"); }
// Otomatik rotalar: her N günde bir, kaynakta varsa ve hedefte yer varsa taşır.
export function addRoute(s, r) { const route = { id: uid(s), from: r.from, to: r.to, res: r.res, q: r.q, every: Math.max(1, r.every | 0), mode: r.mode || "nakliyeci", keep: r.keep || 0, last: -99 }; s.routes.push(route); return { ok: true, route }; }
export function removeRoute(s, id) { s.routes = s.routes.filter(r => r.id !== id); }
export function routesTick(s) {
  const d = day(s), out = [], half = has(s, "lojistik_opt") ? 0.5 : 1;
  for (const r of s.routes) {
    if ((s.time.min - (r.lastMin ?? -1e9)) < r.every * 1440 * half - 1) continue;
    const have = qty(s, r.from, r.res) - r.keep, q = Math.min(r.q, Math.max(0, have), freeFor(s, r.to, r.res) / Math.max(RES[r.res].vol, 1e-6));
    if (q < 0.1) { r.lastMsg = have < 0.1 ? "Kaynakta stok yok" : "Hedefte yer yok"; continue; }
    const res = dispatch(s, { from: r.from, to: r.to, items: [{ res: r.res, q: Math.floor(q * 10) / 10 }], mode: r.mode });
    if (!res.ok && r.mode !== "nakliyeci") { const alt = dispatch(s, { from: r.from, to: r.to, items: [{ res: r.res, q: Math.floor(q * 10) / 10 }], mode: "nakliyeci" }); r.lastMsg = alt.ok ? "Filo meşgul, nakliyeciyle gitti" : alt.msg; if (alt.ok) { r.last = d; r.lastMin = s.time.min; } }
    else { r.lastMsg = res.ok ? "Gönderildi" : res.msg; if (res.ok) { r.last = d; r.lastMin = s.time.min; } }
    out.push(r);
  }
  return out;
}
