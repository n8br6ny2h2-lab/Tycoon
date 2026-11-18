import { BAL } from "../data/balance.js";
import { RESOURCES } from "../data/resources.js";
import { REGIONS } from "../data/regions.js";
import { POSITIONS } from "../data/employees.js";
import { COMPETITORS } from "../data/competitors.js";
import { initMarket } from "./market.js";
import { initRivals } from "./competitors.js";
import { initQuests } from "./quests.js";

// Yeni oyun durumu. Başlangıç: Anadolu'da 3 arsa, birinde genel depo, 6 Mn ₺ nakit.
export function newGame(name = "Patron Sanayi A.Ş.", seed = Date.now() % 2147483647) {
  const s = {
    v: BAL.version, seed: seed | 0, nextId: 1,
    time: { min: 8 * 60, speed: 1, lastReal: 0 },
    company: { name, cash: BAL.startCash, xp: 0, rep: BAL.startRep, credit: 60, overdraftDays: 0, bankrupt: false },
    regions: {}, buildings: {}, inventory: {}, market: {}, orders: [], shipments: [], fleet: [], routes: [], autoStock: [],
    contracts: [], offers: [], staff: {}, research: { done: {}, active: null }, rivals: [], loans: [],
    ledger: { today: {}, days: [], month: {}, months: [], monthProfit: 0 },
    events: [], special: null, quests: null, achievements: {}, notes: [],
    stats: { bought: 0, sold: 0, built: 0, produced: 0, producedBy: {}, deliveries: 0, tendersWon: 0, exports: 0, peakValue: 0, rank: 0, profitToday: 0, upgrades: 0, researchDone: 0, valueWeekStart: 0 },
    grid: { ok: true, until: 0 }, econ: { cycle: 0, interest: BAL.baseInterest },
    settings: { sound: true, haptics: true, notify: true },
    offline: null,
  };
  for (const r of REGIONS) s.regions[r.id] = { owned: r.cost === 0, plots: Array.from({ length: r.cols * r.rows }, () => ({ owned: false, b: null })) };
  for (const p of POSITIONS) s.staff[p.id] = { n: 0, lvl: 1, training: 0 };
  for (const r of REGIONS) s.inventory[r.id] = {};
  s.staff.isci.n = 0;
  initMarket(s);
  initRivals(s, COMPETITORS);
  // Başlangıç arazisi: 3 arsa; ilkinde 600 hacimlik genel depo
  const a = s.regions.anadolu.plots; a[0].owned = a[1].owned = a[4].owned = true;
  const id = "b" + (s.nextId++);
  s.buildings[id] = { id, kind: "warehouse", type: "genel", region: "anadolu", plot: 0, level: 1, security: 0, racks: false, built: s.time.min };
  a[0].b = id;
  initQuests(s);
  s.stats.valueWeekStart = BAL.startCash;
  return s;
}
