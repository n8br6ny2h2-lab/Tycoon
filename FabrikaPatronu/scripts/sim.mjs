// Uzun süreli ekonomi simülasyonu: bot oyuncu N oyun günü oynar. Kontroller: negatif stok yok, fiyatlar sınırda,
// para kontrolsüz büyümüyor, üretim kilitlenmiyor, iflas yok. Kullanım: node scripts/sim.mjs [gün] [tohum]
import { newGame } from "../src/core/state.js";
import * as M from "../src/core/market.js";
import * as F from "../src/core/factory.js";
import * as W from "../src/core/warehouse.js";
import * as E from "../src/core/employees.js";
import * as K from "../src/core/contracts.js";
import * as R from "../src/core/research.js";
import * as Fi from "../src/core/finance.js";
import { claim } from "../src/core/quests.js";
import { advance } from "../src/core/game.js";
import { workersNeeded } from "../src/core/employees.js";
import { qty, item } from "../src/core/inventory.js";
import { RESOURCES, RES } from "../src/data/resources.js";
import { FT, RECIPE } from "../src/data/factories.js";
import { level, day } from "../src/core/util.js";

const DAYS = +process.argv[2] || 730, SEED = +process.argv[3] || 11;
const s = newGame("Bot Sanayi", SEED);
const reg = "anadolu", fails = [];
const freePlot = r => s.regions[r].plots.findIndex(p => p.owned && !p.b);
function ensureWorkers() { const need = workersNeeded(s) - s.staff.isci.n; if (need > 0) E.hire(s, "isci", need); }
function buyPlotIfNeeded(r) { if (freePlot(r) >= 0) return true; const i = s.regions[r].plots.findIndex(p => !p.owned); return i >= 0 && F.buyPlot(s, r, i).ok; }
// Bot planı: önce mobilya (kereste→ahşap→mobilya), sonra gıda, çimento, cam, kimya, çelik...
const PLAN = [["mobilya", "ahsap"], ["gida", "un"], ["cimento", "cimento"], ["cam", "cam"], ["tekstil", "kumas"], ["kagit", "kagit"], ["kimya", "kimyasal"], ["celik", "ham_demir"], ["plastik", "plastik"], ["aluminyum", "aluminyum"], ["celik", "celik"]];
let step = 0, t0 = Date.now(), maxCash = 0, firstFactoryDay = null, firstSaleDay = null;
for (let d = 0; d < DAYS; d++) {
  // 1) Fabrikalarda otomatik tedarik ve satış açık; teslim edilebilir sözleşmeleri kabul et
  for (const b of Object.values(s.buildings)) if (b.recipe) { b.autoBuy = true; b.autoSell = true; b.buyDays = 1; }
  for (const o of [...s.offers]) if (o.kind !== "ihale" && qty(s, reg, o.res) >= o.q * 1.2 && item(s, reg, o.res).ql >= o.minQ) { const a = K.accept(s, o.id); if (a.ok) { a.contract.auto = true; } }
  if (firstSaleDay === null && s.stats.sold > 0) firstSaleDay = d;
  // 4) Büyüme: yeni fabrika, makine, depo, araştırma, bakım
  const hasFac = Object.values(s.buildings).some(b => b.kind !== "warehouse"), reserve = hasFac ? E.monthlyPayroll(s) + 3e6 : 5e5, cash = s.company.cash - reserve;
  if (step < PLAN.length && cash > F.buildCost(s, PLAN[step][0], reg) * (step ? 1.6 : 1.05) && !F.canBuild(s, PLAN[step][0], reg) && buyPlotIfNeeded(reg)) {
    const r = F.buildFactory(s, PLAN[step][0], reg, freePlot(reg)); if (r.ok) { F.setRecipe(s, r.id, PLAN[step][1]); step++; if (firstFactoryDay === null) firstFactoryDay = d; }
  }
  for (const b of Object.values(s.buildings)) if (b.kind !== "warehouse") {
    b.autoMaint = true;
    const util = W.capacitySummary(s, b.region); if (util.used / Math.max(1, util.total) > 0.6) continue;
    if (cash > F.machineCost(s, b) * 2 && b.machines < F.maxMachines(b)) F.upgrade(s, b.id, "machine");
    else if (cash > F.levelCost(s, b) * 3 && b.machines >= F.maxMachines(b)) F.upgrade(s, b.id, "level");
  }
  const cap = W.capacitySummary(s, reg);
  if (cap.used / Math.max(1, cap.total) > 0.55 && s.company.cash > 4e6 && buyPlotIfNeeded(reg)) W.buildWarehouse(s, level(s) >= 3 ? "lojistik" : "genel", reg, freePlot(reg));
  if (Object.values(s.buildings).some(b => b.recipe && Object.keys(RECIPE[b.recipe].in).some(i => RES[i].storage === "kimya")) && !Object.values(s.buildings).some(b => b.type === "kimya" && b.kind === "warehouse") && s.company.cash > 4e6 && buyPlotIfNeeded(reg)) W.buildWarehouse(s, "kimya", reg, freePlot(reg));
  if (Object.values(s.buildings).some(b => b.type === "celik") && !Object.values(s.buildings).some(b => b.kind === "warehouse" && b.type === "metal") && s.company.cash > 3e6 && buyPlotIfNeeded(reg)) W.buildWarehouse(s, "metal", reg, freePlot(reg));
  if (!s.research.active && s.company.cash > 3e7) for (const r of R.researchList()) if (R.startResearch(s, r.id).ok) break;
  ensureWorkers();
  for (const q of [...s.quests.daily, ...s.quests.weekly]) if (q.done && !q.claimed) claim(s, q.id);
  if (s.company.rescue) Fi.takeRescue(s, Fi.rescueOptions(s)[0].id);
  advance(s, 1440);
  // Kontroller
  for (const r in s.inventory) for (const [id, it] of Object.entries(s.inventory[r])) if (!(it.q >= -1e-9) || !isFinite(it.cost)) fails.push(`gün ${d}: negatif/bozuk stok ${r}/${id} ${it.q}`);
  for (const r of RESOURCES) { const p = s.market[r.id].price; if (!(p >= r.min * 0.99 && p <= r.max * 1.01)) fails.push(`gün ${d}: fiyat sınır dışı ${r.id} ${p}`); }
  if (!isFinite(s.company.cash)) fails.push(`gün ${d}: nakit bozuk`);
  maxCash = Math.max(maxCash, s.company.cash);
  if (s.company.bankrupt) { fails.push(`gün ${d}: iflas`); break; }
  if (d % 90 === 89 || d === DAYS - 1) {
    const facs = Object.values(s.buildings).filter(b => b.kind !== "warehouse");
    console.log(`gün ${d + 1}: seviye ${level(s)} nakit ${(s.company.cash / 1e6).toFixed(1)} Mn değer ${(Fi.companyValue(s) / 1e6).toFixed(1)} Mn fabrika ${facs.length} makine ${facs.reduce((a, b) => a + b.machines, 0)} işçi ${s.staff.isci.n} teslimat ${s.stats.deliveries} sıra ${s.stats.rank} durumlar ${[...new Set(facs.map(b => b.status))].join("/")}`);
  }
}
if (process.env.DUMP) { const cats = {}; for (const dd of s.ledger.days.slice(-30)) for (const [k, v] of Object.entries(dd.cats)) cats[k] = (cats[k] || 0) + v; console.log(Object.entries(cats).sort((a, b) => a[1] - b[1]).map(([k, v]) => k + " " + (v / 1e6).toFixed(2)).join(" | ")); for (const b of Object.values(s.buildings)) if (b.recipe) console.log(b.type, b.recipe, "mak", b.machines, b.status, b.autoMsg || "", "kond", b.condition.toFixed(0)); console.log("fiyatlar", ["kereste","islenmis_ahsap","bugday","un","kirectasi","cimento","cam","kumas","pamuk","kagit"].map(id => id + " " + (s.market[id].price / RES[id].base).toFixed(2)).join(" ")); }
const facs = Object.values(s.buildings).filter(b => b.kind !== "warehouse"), stuck = facs.filter(b => b.status === "Bakım gerekiyor" || b.status === "İşçi yok");
if (stuck.length) fails.push("kilitlenen fabrika: " + stuck.map(b => b.type + " " + b.status).join(", "));
const v = Fi.companyValue(s);
if (v > 5e10) fails.push(`kontrolsüz büyüme: şirket değeri ${(v / 1e9).toFixed(1)} Mlr ₺`);
if (firstFactoryDay === null || firstFactoryDay > 3) fails.push(`ilk fabrika geç kuruldu: gün ${firstFactoryDay}`);
console.log(`ilk fabrika gün ${firstFactoryDay}, ilk satış gün ${firstSaleDay}; ${((Date.now() - t0) / 1000).toFixed(1)} sn`);
console.log(fails.length ? "HATALAR:\n" + fails.slice(0, 20).join("\n") : "SİMÜLASYON TEMİZ");
process.exit(fails.length ? 1 : 0);
