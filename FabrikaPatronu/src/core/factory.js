// FactoryManager — arsa, bölge, fabrika kurma, makine, yükseltme hatları, bakım, reçete ve öncelik.
import { FT, RECIPE } from "../data/factories.js";
import { RG } from "../data/regions.js";
import { BAL } from "../data/balance.js";
import { spend, earn, factoryValue, canSpendOperating } from "./finance.js";
import { level, uid, note, has } from "./util.js";
import { questEvent } from "./quests.js";
import { addXp } from "./progress.js";

export const plotCost = (s, region) => RG[region].plotPrice * (1 + 0.15 * s.regions[region].plots.filter(p => p.owned).length);
export function buyPlot(s, region, idx) {
  const reg = s.regions[region], p = reg.plots[idx];
  if (!reg.owned) return { ok: false, msg: "Önce bölgeyi aç" };
  if (!p || p.owned) return { ok: false, msg: "Arsa satılık değil" };
  const c = plotCost(s, region); if (s.company.cash < c) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, c, "Yatırım"); p.owned = true; return { ok: true, cost: c };
}
export function unlockRegion(s, region) {
  const g = RG[region], reg = s.regions[region];
  if (reg.owned) return { ok: false, msg: "Bölge zaten açık" };
  if (level(s) < g.level) return { ok: false, msg: `Şirket seviyesi ${g.level} gerekir` };
  if (s.company.cash < g.cost) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, g.cost, "Yatırım"); reg.owned = true; reg.plots[0].owned = reg.plots[1].owned = true;
  note(s, `${g.name} açıldı. ${g.perk}.`, "good"); return { ok: true };
}
// Bir tipin bu bölgede çalıştırabileceği reçeteler.
export function recipesFor(s, type, region) {
  const t = FT[type], g = RG[region];
  return t.recipes.filter(r => {
    if (r.research && !has(s, r.research)) return false;
    if (t.id === "maden") return g.mines.includes(r.id);
    if (t.id === "kuyu") return g.wells.includes(r.id);
    if (t.id === "ciftlik") return !!g.farm;
    return true;
  });
}
export function canBuild(s, type, region) {
  const t = FT[type];
  if (level(s) < t.unlock.level) return `Şirket seviyesi ${t.unlock.level} gerekir`;
  if (t.unlock.research && !has(s, t.unlock.research)) return "Araştırma gerekir";
  if (!recipesFor(s, type, region).length) return "Bu bölgede kurulamaz";
  return null;
}
export const buildCost = (s, type, region) => FT[type].cost * (RG[region].factoryMod || 1);
export function buildFactory(s, type, region, plot) {
  const reg = s.regions[region], p = reg.plots[plot], why = canBuild(s, type, region);
  if (why) return { ok: false, msg: why };
  if (!reg.owned || !p || !p.owned) return { ok: false, msg: "Arsa senin değil" };
  if (p.b) return { ok: false, msg: "Arsa dolu" };
  const c = buildCost(s, type, region); if (s.company.cash < c) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, c, "Yatırım"); const id = "b" + uid(s), rec = recipesFor(s, type, region)[0];
  s.buildings[id] = { id, kind: FT[type].kind === "factory" ? "factory" : FT[type].kind, type, region, plot, level: 1, machines: 1, automation: 0, qualityLvl: 0, fireLvl: 0,
    condition: 100, recipe: rec.id, priority: 2, on: true, cycle: null, pending: null, status: "Hazır", made: 0, autoMaint: false, autoBuy: false, autoSell: false, buyDays: 1, keep: {}, built: s.time.min };
  p.b = id; s.stats.built++; questEvent(s, "build", {}); addXp(s, c / 1500);
  return { ok: true, id };
}
export const maxMachines = b => 2 + 2 * b.level;
export const machineCost = (s, b) => FT[b.type].cost * BAL.machineCostRatio * (1 + 0.1 * (b.machines - 1));
export const levelCost = (s, b) => FT[b.type].cost * BAL.upgradeCostRatio * b.level;
export const lineCost = (s, b, what) => FT[b.type].cost * (what === "automation" ? 0.3 : 0.2) * ((b[what === "automation" ? "automation" : what === "quality" ? "qualityLvl" : "fireLvl"]) + 1);
export function upgrade(s, id, what) {
  const b = s.buildings[id]; if (!b || b.kind === "warehouse") return { ok: false };
  let cost, apply;
  if (what === "machine") { if (b.machines >= maxMachines(b)) return { ok: false, msg: `En fazla ${maxMachines(b)} makine; önce fabrika seviyesini yükselt` }; cost = machineCost(s, b); apply = () => b.machines++; }
  else if (what === "level") { if (b.level >= BAL.maxLevel) return { ok: false, msg: "En üst seviye" }; cost = levelCost(s, b); apply = () => b.level++; }
  else if (what === "automation") { const cap = has(s, "otomasyon") ? 3 : 1; if (b.automation >= cap) return { ok: false, msg: cap < 3 ? "Daha fazlası için Otomatik üretim araştırması gerekir" : "En üst seviye" }; cost = lineCost(s, b, what); apply = () => b.automation++; }
  else if (what === "quality") { if (b.qualityLvl >= 3) return { ok: false, msg: "En üst seviye" }; cost = lineCost(s, b, what); apply = () => b.qualityLvl++; }
  else if (what === "fire") { if (b.fireLvl >= 3) return { ok: false, msg: "En üst seviye" }; cost = lineCost(s, b, what); apply = () => b.fireLvl++; }
  else return { ok: false };
  if (s.company.cash < cost) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, cost, "Yatırım"); apply(); s.stats.upgrades++; questEvent(s, "upgrade", {}); addXp(s, cost / 1500);
  return { ok: true, cost };
}
export const maintCost = (s, b) => FT[b.type].cost * BAL.maintCostRatio * (100 - b.condition) / 100 * (1 + 0.15 * (b.machines - 1));
export function maintain(s, id) {
  const b = s.buildings[id], c = maintCost(s, b); if (b.condition >= 99.5) return { ok: false, msg: "Bakım gerekmiyor" };
  if (!canSpendOperating(s, c)) return { ok: false, msg: "Nakit ve kredi limiti yetersiz" };
  spend(s, c, "Bakım"); b.condition = 100; if (b.status === "Bakım gerekiyor") b.status = "Hazır"; return { ok: true, cost: c };
}
export function setRecipe(s, id, rid) {
  const b = s.buildings[id]; if (!recipesFor(s, b.type, b.region).some(r => r.id === rid)) return { ok: false, msg: "Bu reçete kullanılamaz" };
  if (b.cycle) { // yarım çevrimin girdileri iade edilir
    for (const [res, q] of Object.entries(b.cycle.used || {})) { const inv = s.inventory[b.region]; const it = inv[res] || (inv[res] = { q: 0, cost: b.cycle.costs[res] || 0, ql: 60 }); it.q += q; }
    b.cycle = null;
  }
  b.recipe = rid; b.status = "Hazır"; return { ok: true };
}
export function setPriority(s, id, p) { s.buildings[id].priority = Math.max(1, Math.min(3, p | 0)); return { ok: true }; }
export function toggle(s, id) { const b = s.buildings[id]; b.on = !b.on; b.status = b.on ? "Hazır" : "Durduruldu"; return { ok: true }; }
export function demolish(s, id) {
  const b = s.buildings[id]; if (!b) return { ok: false };
  if (b.kind === "warehouse") return { ok: false, msg: "Depolar bu menüden satılır" };
  if (b.cycle) setRecipe(s, id, b.recipe);
  const v = factoryValue(s, b) * 0.6; earn(s, v, "Varlık satışı");
  s.regions[b.region].plots[b.plot].b = null; delete s.buildings[id]; s.routes = s.routes.filter(r => r.factory !== id);
  return { ok: true, value: v };
}
export function sellWarehouse(s, id, WT) {
  const b = s.buildings[id]; if (!b || b.kind !== "warehouse") return { ok: false };
  if (Object.values(s.buildings).filter(x => x.kind === "warehouse").length <= 1) return { ok: false, msg: "Son deponu satamazsın" };
  const v = WT[b.type].cost * 0.5; earn(s, v, "Varlık satışı");
  s.regions[b.region].plots[b.plot].b = null; delete s.buildings[id]; return { ok: true, value: v };
}
