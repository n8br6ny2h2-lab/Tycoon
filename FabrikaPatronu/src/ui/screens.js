// Ekranlar: harita katmanı, piyasa, fabrikalar, depolar, lojistik, şirket ve alt panelleri. Her buton gerçek bir eyleme bağlıdır (data-a).
import { RESOURCES, RES, CATEGORIES } from "../data/resources.js";
import { FACTORY_TYPES, FT, RECIPE } from "../data/factories.js";
import { WAREHOUSE_TYPES, WT, STORAGE_CLASSES } from "../data/warehouses.js";
import { VEHICLES, VH, SUPPLIERS } from "../data/vehicles.js";
import { REGIONS, RG, CITY } from "../data/regions.js";
import { POSITIONS, POS, TRAINING } from "../data/employees.js";
import { RESEARCH, RS } from "../data/research.js";
import { ACHIEVEMENTS } from "../data/quests.js";
import { CONTRACT_KINDS, CT } from "../data/customers.js";
import { BAL } from "../data/balance.js";
import * as M from "../core/market.js";
import * as F from "../core/factory.js";
import * as W from "../core/warehouse.js";
import * as Fi from "../core/finance.js";
import * as E from "../core/employees.js";
import * as Rs from "../core/research.js";
import * as L from "../core/logistics.js";
import { recipeEconomics, speedOf, STATUS } from "../core/production.js";
import { ranking, marketShare } from "../core/competitors.js";
import { activeEvents } from "../core/events.js";
import { qty, item } from "../core/inventory.js";
import { level, day, has } from "../core/util.js";
import { iconSvg } from "./icons.js";
import { tl, tlFull, n0, n1, n2, pct, qtyTxt, esc, dateTxt, spark, chart } from "./fmt.js";

const ownedRegions = s => REGIONS.filter(r => s.regions[r.id].owned);
const btn = (a, v, label, cls = "", dis = false, title = "") => `<button class="b ${cls}" data-a="${a}" data-v="${esc(v)}" ${dis ? "disabled" : ""} ${title ? `title="${esc(title)}"` : ""}>${label}</button>`;
const chip = (t, cls = "") => `<span class="chip ${cls}">${t}</span>`;
const STATUS_CLS = { "Çalışıyor": "g", "Üretim tamamlandı": "g", "Hammadde bekliyor": "w", "Bakım gerekiyor": "b", "Depo dolu": "p", "Enerji kesintisi": "w", "Durduruldu": "", "İşçi yok": "s", "Hazır": "" };
export const statusChip = st => chip(st, STATUS_CLS[st] || "");
const ioLine = rec => `${Object.entries(rec.in).map(([id, q]) => `<span class="io">${iconSvg(id, 18)}${n2(q)}</span>`).join("") || `<span class="muted">girdi yok</span>`} <span class="arrow">→</span> ${Object.entries(rec.out).map(([id, q]) => `<span class="io">${iconSvg(id, 18)}${n2(q)}</span>`).join("")}`;
const art = (look, w = 120, h = 84, extra = "") => `<canvas class="art" data-art="${look}" ${extra} width="${w}" height="${h}" style="width:${w}px;height:${h}px" aria-hidden="true"></canvas>`;
const bar = (f, cls = "") => `<span class="bar ${cls}"><i style="width:${Math.max(0, Math.min(1, f)) * 100}%"></i></span>`;
const regionSelect = (s, name, cur, onlyOwned = true) => `<select data-f="${name}">${(onlyOwned ? ownedRegions(s) : REGIONS).map(r => `<option value="${r.id}" ${r.id === cur ? "selected" : ""}>${r.short}</option>`).join("")}</select>`;

// ---------------- HARİTA KATMANI ----------------
export function mapOverlay(s, V) {
  const regs = ownedRegions(s), facs = Object.values(s.buildings).filter(b => b.kind !== "warehouse");
  const counts = {}; for (const b of facs) counts[b.status] = (counts[b.status] || 0) + 1;
  const e = s.energyLast || {}, offers = s.offers.length, qDone = [...s.quests.daily, ...s.quests.weekly].filter(q => q.done && !q.claimed).length;
  const ra = s.research.active;
  let h = `<div class="mapbar">${regs.map(r => btn("mapRegion", r.id, r.short, V.mapMode === "region" && V.region === r.id ? "on" : "")).join("")}${btn("mapWorld", "", "🌍 Dünya", V.mapMode === "world" ? "on" : "")}</div>
  <div class="mapzoom">${btn("zoom", "1.2", "+", "round")}${btn("zoom", "0.83", "−", "round")}${btn("zoom", "fit", "⤢", "round")}</div>
  <div class="mapside">
    <div class="mcard"><span class="lbl">${V.mapMode === "world" ? "DÜNYA" : esc(RG[V.region].name).toLocaleUpperCase("tr")}</span>
      ${V.mapMode === "world" ? `<div class="muted small">Bir bölgeye dokun. Kilitli bölgeler seviye ve sermayeyle açılır.</div>` : `<div class="muted small">${esc(RG[V.region].perk)}</div>`}
      <div class="stline">${Object.entries(counts).map(([k, v]) => `${statusChip(k)}<b>${v}</b>`).join(" ") || `<span class="muted small">Henüz fabrika yok. Boş arsaya (+) dokun.</span>`}</div>
      <div class="kv2"><span>Enerji</span><b>${n0(e.own || 0)} kendi · ${n0(e.bought || 0)} şebeke MWh/sa</b></div>
      ${ra ? `<div class="kv2"><span>Araştırma</span><b>${RS[ra.id].name}</b></div>${bar(1 - ra.left / (RS[ra.id].days * 24))}` : ""}</div>
    <div class="mbtns">${btn("go", "orders", `📦 Siparişler${offers ? `<i class="bd">${offers}</i>` : ""}`, "tile")}${btn("go", "quests", `🎯 Görevler${qDone ? `<i class="bd g">${qDone}</i>` : ""}`, "tile")}</div>
  </div>`;
  const tut = tutorialStep(s);
  if (tut) h += `<div class="tut"><span class="lbl">İLK ADIMLAR · ${tut.i + 1}/${TUT.length}</span><b>${tut.t}</b><span class="muted small">${tut.d}</span>${tut.go ? btn("go", tut.go, tut.btn || "Git", "gold sm") : ""}</div>`;
  return h;
}
export const TUT = [
  { t: "Hammadde al", d: "Piyasa'dan 20 t kereste al. Mal, tedarikçinin teslim süresi sonunda deponda olur.", done: s => s.stats.bought > 0, go: "market", btn: "Piyasaya git" },
  { t: "Malın depoya gelsin", d: "Lojistik'te sevkiyatı izleyebilirsin. Hızlı tedarikçi 6 saatte getirir.", done: s => Object.values(s.inventory.anadolu).length > 0, go: "logistics", btn: "Sevkiyatlar" },
  { t: "İlk satışını yap", d: "Depolar'dan ya da Piyasa'dan bir miktar sat. Ortalama maliyetine göre kâr ya da zarar görünür.", done: s => s.stats.sold > 0, go: "depots", btn: "Depolara git" },
  { t: "İlk fabrikanı kur", d: "Haritada boş arsaya (+) dokun ve Mobilya fabrikası kur. Keresteyi işlenmiş ahşaba çevirir.", done: s => s.stats.built > 0, go: "map", btn: "Haritaya dön" },
  { t: "İşçi işe al", d: "Fabrikanın makine başına işçisi olmalı. Şirket › Personel'den ya da fabrika ekranından işe al.", done: s => s.staff.isci.n > 0, go: "staff", btn: "Personel" },
  { t: "İlk üretim", d: "Fabrikada otomatik tedarik ve otomatik satışı açarsan zincir kendi kendine döner.", done: s => s.stats.produced > 0, go: "factories", btn: "Fabrikalar" },
  { t: "İlk sözleşme", d: "Siparişler'den bir müşteri teklifi kabul et ve zamanında teslim et. İtibarın artar.", done: s => s.contracts.length > 0 || s.stats.deliveries > 0, go: "orders", btn: "Siparişler" },
];
export function tutorialStep(s) { const i = s.tut || 0; if (i >= TUT.length) return null; return { i, ...TUT[i] }; }

// ---------------- PİYASA ----------------
export function marketView(s, V) {
  const cat = V.mcat || "all", list = RESOURCES.filter(r => (cat === "all" || r.cat === cat));
  const sel = RES[V.msel] || RES.kereste, mk = s.market[sel.id], p = M.price(s, sel.id), ch = M.change(s, sel.id);
  let left = `<div class="filters">${[["all", "Tümü"], ...Object.entries(CATEGORIES)].map(([k, n]) => btn("mcat", k, n, k === cat ? "on" : "")).join("")}</div><div class="rlist">`;
  for (const r of list) { const c = M.change(s, r.id), mine = ownedRegions(s).reduce((a, g) => a + qty(s, g.id, r.id), 0);
    left += `<button class="rrow ${r.id === sel.id ? "on" : ""}" data-a="msel" data-v="${r.id}">${iconSvg(r.id, 26)}<span class="rn"><b>${r.name}</b><small>${CATEGORIES[r.cat]}${mine > 0 ? ` · stok ${qtyTxt(r.id, mine)}` : ""}</small></span>${spark(M.history(s, r.id, "24s"), 54, 20)}<span class="rp"><b>${n0(M.price(s, r.id))}</b><small class="${c >= 0 ? "good" : "bad"}">${pct(c)}</small></span></button>`; }
  left += `</div>`;
  const win = V.mwin || "24s", hist = M.history(s, sel.id, win);
  const labels = { "24s": [[0, "24 sa önce"], [1, "şimdi"]], "7g": [[0, "7 gün önce"], [1, "bugün"]], "30g": [[0, "30 gün önce"], [1, "bugün"]], "1y": [[0, hist.length > 300 ? "1 yıl önce" : `${hist.length} gün önce`], [1, "bugün"]] }[win];
  const T = sel.cons * BAL.targetStockDays, held = ownedRegions(s).map(g => ({ g, it: item(s, g.id, sel.id) })).filter(x => x.it.q > 0);
  let right = `<div class="mhead">${iconSvg(sel.id, 40)}<div><h2>${sel.name}</h2><span class="muted small">${CATEGORIES[sel.cat]} · ${STORAGE_CLASSES[sel.storage]} depo · birim ${sel.unit}</span></div>
    <div class="bigp"><b>${n0(p)} ₺</b><small class="${ch >= 0 ? "good" : "bad"}">${pct(ch)} bugün</small></div></div>
    <div class="filters">${[["24s", "Son 24 saat"], ["7g", "Son 7 gün"], ["30g", "Son 30 gün"], ["1y", "Son 1 yıl"]].map(([k, n]) => btn("mwin", k, n, k === win ? "on" : "")).join("")}</div>
    ${chart(hist, 560, 150, labels)}
    <div class="stats4"><div><span>Dünya üretimi</span><b>${n0(sel.prod)}/gün</b></div><div><span>Dünya tüketimi</span><b>${n0(sel.cons)}/gün</b></div><div><span>Piyasa stoğu</span><b class="${mk.stock < T * 0.85 ? "bad" : mk.stock > T * 1.15 ? "good" : ""}">${n0(mk.stock / T * 100)}%</b></div><div><span>Fiyat bandı</span><b>${n0(sel.min)}–${n0(sel.max)}</b></div></div>
    ${held.length ? `<div class="held">${held.map(({ g, it }) => `<span>${g.short}: <b>${qtyTxt(sel.id, it.q)}</b> · ort. ${n0(it.cost)} ₺ · kalite ${n0(it.ql)} · <b class="${p >= it.cost ? "good" : "bad"}">${pct(p / it.cost - 1)}</b></span>`).join("")}</div>` : ""}`;
  right += M.tradeable(sel) ? tradePanel(s, V, sel) : `<div class="hint">Elektrik depolanmaz. Fabrikaların şebekeden bu fiyatla otomatik alır; kendi santralin varsa önce oradan beslenir, fazlası şebekeye satılır.</div>`;
  const ords = s.orders.filter(o => o.res === sel.id);
  if (ords.length) right += `<div class="card"><b>Açık emirler</b>${ords.map(o => `<div class="line">${o.side === "bulk" ? `Toplu sipariş: günde ${qtyTxt(o.res, o.q)} × ${o.left} gün · ${n0(o.unit)} ₺` : `${o.side === "buy" ? "Limit alım" : "Limit satış"} ${qtyTxt(o.res, o.q)} · sınır ${n0(o.limit)} ₺ · ${RG[o.region].short}`}${btn("cancelOrder", o.id, "İptal", "ghost sm")}</div>`).join("")}</div>`;
  return `<div class="split"><div class="pane left">${left}</div><div class="pane right">${right}</div></div>`;
}
function tradePanel(s, V, r) {
  const side = V.side || "buy", ot = V.otype || "market", reg = s.regions[V.treg] && s.regions[V.treg].owned ? V.treg : "anadolu", q = V.qty || 10, sup = V.sup || "standart";
  const have = qty(s, reg, r.id), qt = M.quote(s, r.id, q, side, reg, sup), free = W.freeFor(s, reg, r.id) / Math.max(r.vol, 1e-6);
  const presets = r.vol < 0.05 ? [100, 1000, 10000, 50000] : r.unit === "kg" ? [1, 5, 20, 100] : [1, 10, 100, 1000];
  let h = `<div class="card trade"><div class="row">
    <div class="seg">${btn("side", "buy", "Al", side === "buy" ? "on buy" : "")}${btn("side", "sell", "Sat", side === "sell" ? "on sell" : "")}</div>
    <div class="seg">${btn("otype", "market", "Piyasa emri", ot === "market" ? "on" : "")}${btn("otype", "limit", "Limit emir", ot === "limit" ? "on" : "")}${side === "buy" ? btn("otype", "bulk", "Toplu sipariş", ot === "bulk" ? "on" : "") : ""}</div>
    <label>Depo ${regionSelect(s, "treg", reg)}</label></div>
    <div class="row">${presets.map(v => btn("qty", v, n0(v) + " " + r.unit, v === q ? "on" : "")).join("")}${btn("qtyMul", "0.5", "÷2", "ghost")}${btn("qtyMul", "2", "×2", "ghost")}
      <input data-f="qty" type="text" inputmode="decimal" min="0" step="any" value="${q}" aria-label="Miktar"><span class="muted small">${side === "buy" ? `boş yer ${qtyTxt(r.id, Math.max(0, free))}` : `stok ${qtyTxt(r.id, have)}`}</span>
      ${side === "sell" && have > 0 ? btn("qtyAll", have, "Tümü", "ghost") : ""}${side === "buy" && free > 0 ? btn("qtyAll", Math.floor(Math.min(free, 1e7) * 10) / 10, "Depoyu doldur", "ghost") : ""}</div>`;
  if (side === "buy") h += `<div class="row sups">${SUPPLIERS.map(x => btn("sup", x.id, `<b>${x.name}</b><small>${x.desc}</small>`, x.id === sup ? "on" : "")).join("")}</div>`;
  if (ot === "limit") h += `<div class="row"><label>Birim fiyat sınırı (₺, tüm giderler dahil) <input data-f="limit" type="text" inputmode="decimal" value="${V.limit || Math.round(qt.perUnit * (side === "buy" ? 0.97 : 1.03))}"></label><span class="muted small">Fiyat ${side === "buy" ? "bu değere inince alınır" : "bu değere çıkınca satılır"}. Emir 7 gün geçerli.</span></div>`;
  if (ot === "bulk") h += `<div class="row"><label>Gün sayısı <select data-f="bdays">${[3, 5, 7, 14, 30].map(d => `<option ${d === (V.bdays || 5) ? "selected" : ""}>${d}</option>`).join("")}</select></label><span class="muted small">Bugünkü fiyattan %2 indirimle her gün ${qtyTxt(r.id, q)} gelir. Bedel her partide ödenir.</span></div>`;
  const cost = side === "buy" ? item(s, reg, r.id).cost : item(s, reg, r.id).cost, pl = side === "sell" && have > 0 ? qt.total - cost * Math.min(q, have) : null;
  h += `<div class="quote"><div><span>Birim (kayma dahil)</span><b>${n0(qt.unit)} ₺</b></div><div><span>Tutar</span><b>${tl(qt.gross)}</b></div><div><span>Komisyon</span><b>${tl(qt.commission)}</b></div><div><span>Vergi</span><b>${tl(qt.tax)}</b></div><div><span>Nakliye</span><b>${tl(qt.fee)}</b></div>
    <div><span>Fiyata etkin</span><b>${pct(qt.impact)}</b></div><div class="tot"><span>${side === "buy" ? "Toplam ödeme" : "Net gelir"}</span><b>${tlFull(qt.total)}</b></div>
    ${side === "buy" ? `<div><span>Teslim</span><b>${qt.hours} saat · kalite ${n0(qt.quality)}</b></div>` : `<div><span>Kâr / zarar</span><b class="${pl >= 0 ? "good" : "bad"}">${pl === null ? "—" : tl(pl)}</b></div>`}</div>
    ${btn("trade", "", ot === "market" ? (side === "buy" ? `Satın al · ${tl(qt.total)}` : `Sat · ${tl(qt.total)}`) : ot === "limit" ? "Limit emri ver" : "Toplu siparişi başlat", side === "buy" ? "gold big" : "sellb big")}</div>`;
  return h;
}

// ---------------- FABRİKALAR ----------------
export function factoriesView(s, V) {
  const facs = Object.values(s.buildings).filter(b => b.kind !== "warehouse"), need = E.workersNeeded(s), have = s.staff.isci.n;
  let h = `<div class="sumbar"><span>İşçi <b class="${have < need ? "bad" : "good"}">${n0(have)} / ${n0(need)}</b></span>${have < need ? btn("hire", "isci:" + (need - have), `+${need - have} işçi al`, "gold sm") : ""}
    <span>Verim <b>${n0(E.workerEfficiency(s) * 100)}%</b></span><span>Enerji <b>${n0((s.energyLast || {}).bought || 0)} MWh/sa şebeke</b></span>${btn("sheet", "build:", "＋ Yeni fabrika kur", "gold sm")}</div><div class="fgrid">`;
  for (const b of facs) {
    const t = FT[b.type], rec = RECIPE[b.recipe], eco = recipeEconomics(s, b);
    h += `<button class="fcard" data-a="sheet" data-v="b:${b.id}">${art(t.look, 112, 78, `data-run="${b.status === "Çalışıyor" ? 1 : 0}" data-lvl="${b.level}"`)}<span class="fb"><b>${t.name}</b><small>${RG[b.region].short} · seviye ${b.level} · ${b.machines} makine</small>
      <span class="io2">${ioLine(rec)}</span>${statusChip(b.status)}${b.cycle ? bar(1 - b.cycle.left / b.cycle.total) : bar(0)}
      <small class="${eco.perDay >= 0 ? "good" : "bad"}">${eco.perDay >= 0 ? "+" : ""}${tl(eco.perDay)}/gün tahmini</small></span></button>`;
  }
  if (!facs.length) h += `<div class="hint">Henüz fabrikan yok. "Yeni fabrika kur"a ya da haritada boş arsaya dokun.</div>`;
  return h + `</div>`;
}
export function buildCatalog(s, V, region, plot) {
  region = region || V.region || "anadolu";
  const freePlots = s.regions[region].plots.map((p, i) => [p, i]).filter(([p]) => p.owned && !p.b);
  plot = plot ?? (freePlots[0] ? freePlots[0][1] : null);
  let h = `<div class="shead"><h3>Kur · ${RG[region].short}${plot !== null ? ` · arsa ${plot + 1}` : ""}</h3>${regionSelect(s, "breg", region)}</div>`;
  if (plot === null) { const i = s.regions[region].plots.findIndex(p => !p.owned); return h + `<div class="hint">Bu bölgede boş arsan yok.${i >= 0 ? ` Yeni arsa: ${tl(F.plotCost(s, region))}` : ""}</div>${i >= 0 ? btn("buyPlot", region + ":" + i, `Arsa satın al · ${tl(F.plotCost(s, region))}`, "gold") : ""}`; }
  h += `<div class="lbl">FABRİKALAR VE TESİSLER</div><div class="cat">`;
  for (const t of FACTORY_TYPES) {
    const why = F.canBuild(s, t.id, region), cost = F.buildCost(s, t.id, region), rec = F.recipesFor(s, t.id, region);
    h += `<div class="citem ${why ? "lock" : ""}">${art(t.look, 92, 64)}<div class="ci"><b>${t.name}</b><small>${t.desc}</small><small>${(rec.length ? rec : t.recipes).slice(0, 3).map(r => r.name).join(" · ")}</small><small>${t.workers} işçi/makine · bakım ${tl(t.maint)}/gün</small></div>
      ${why ? `<span class="chip">${why}</span>` : btn("buildF", `${t.id}:${region}:${plot}`, tl(cost), s.company.cash >= cost ? "gold sm" : "sm", s.company.cash < cost)}</div>`;
  }
  h += `</div><div class="lbl">DEPOLAR</div><div class="cat">`;
  for (const t of WAREHOUSE_TYPES) { const lock = level(s) < t.unlock.level;
    h += `<div class="citem ${lock ? "lock" : ""}">${art(t.look, 92, 64)}<div class="ci"><b>${t.name}</b><small>${t.desc}</small><small>${t.accepts.map(c => STORAGE_CLASSES[c]).join(", ")} · ${n0(t.cap)} hacim · ${tl(t.maint)}/gün</small></div>
      ${lock ? `<span class="chip">Seviye ${t.unlock.level}</span>` : btn("buildW", `${t.id}:${region}:${plot}`, tl(t.cost), s.company.cash >= t.cost ? "gold sm" : "sm", s.company.cash < t.cost)}</div>`; }
  return h + `</div>`;
}
export function factorySheet(s, b) {
  const t = FT[b.type], rec = RECIPE[b.recipe], eco = recipeEconomics(s, b), recs = F.recipesFor(s, b.type, b.region);
  const need = t.workers * b.machines * (1 - 0.2 * b.automation);
  let h = `<div class="shead">${art(t.look, 150, 104, `data-run="${b.status === "Çalışıyor" ? 1 : 0}" data-lvl="${b.level}"`)}<div><h3>${t.name}</h3><span class="muted small">${RG[b.region].name} · arsa ${b.plot + 1}</span><div>${statusChip(b.status)} ${b.autoMsg ? chip(esc(b.autoMsg), "w") : ""}</div>
    ${b.cycle ? `<div class="small">Çevrim: ${b.cycle.k}/${b.machines} makine · kalan ${n1(Math.max(0, b.cycle.left))} sa</div>${bar(1 - b.cycle.left / b.cycle.total)}` : ""}</div></div>
    <div class="lbl">ÜRETİM REÇETESİ</div><div class="recs">${recs.map(r => { const e = recipeEconomics(s, b, r.id); return `<button class="rec ${r.id === b.recipe ? "on" : ""}" data-a="setRecipe" data-v="${b.id}:${r.id}"><b>${r.name}</b><span class="io2">${ioLine(r)}</span><small>${n1(r.hours / speedOf(s, b))} sa · ${n1(r.mwh)} MWh · fire %${n1(r.fire * 100)} · <b class="${e.perDay >= 0 ? "good" : "bad"}">${tl(e.perDay)}/gün</b></small></button>`; }).join("")}</div>
    <div class="kvgrid"><span>Çevrim maliyeti</span><b>${tl(eco.cost)}</b><span>Çıktı değeri</span><b>${tl(eco.outVal)}</b><span>Girdi · enerji · sarf</span><b>${tl(eco.inCost)} · ${tl(eco.energy)} · ${tl(eco.labor)}</b><span>Toplam üretim</span><b>${qtyTxt(Object.keys(rec.out)[0], b.made)}</b>
      <span>İşçi ihtiyacı</span><b>${n0(need)} (${t.workers}/makine)</b><span>Durum / bakım</span><b class="${b.condition < 40 ? "bad" : ""}">${n0(b.condition)}%</b></div>
    <div class="lbl">ÜRETİM HATTI</div><div class="ups">
      ${up(s, b, "machine", `Makine ekle (${b.machines}/${F.maxMachines(b)})`, F.machineCost(s, b), b.machines >= F.maxMachines(b), "Kapasite +1 makine")}
      ${up(s, b, "level", `Fabrika seviyesi ${b.level}/5`, F.levelCost(s, b), b.level >= 5, "Hız +%8, makine sınırı +2")}
      ${up(s, b, "automation", `Otomasyon ${b.automation}/${has(s, "otomasyon") ? 3 : 1}`, F.lineCost(s, b, "automation"), b.automation >= (has(s, "otomasyon") ? 3 : 1), "İşçi −%20, hız +%5")}
      ${up(s, b, "quality", `Kalite hattı ${b.qualityLvl}/3`, F.lineCost(s, b, "quality"), b.qualityLvl >= 3, "Kalite +6")}
      ${up(s, b, "fire", `Fire azaltma ${b.fireLvl}/3`, F.lineCost(s, b, "fire"), b.fireLvl >= 3, "Fire −%25")}
      <div class="upi"><span><b>Bakım</b><small>Durum ${n0(b.condition)}% · 40 altında yavaşlar, 15 altında durur</small></span>${btn("maint", b.id, `Bakım · ${tl(F.maintCost(s, b))}`, "sm", b.condition > 99)}</div></div>
    <div class="lbl">OTOMASYON VE ÖNCELİK</div><div class="toggles">
      ${tog("autoBuy", b.id, b.autoBuy, "Otomatik tedarik", "Girdileri ~1 günlük ihtiyaçta tutar")}${tog("autoSell", b.id, b.autoSell, "Otomatik satış", "Çıktıyı piyasaya satar")}${tog("autoMaint", b.id, b.autoMaint, "Planlı bakım", "Durum %50 altına inince bakım yapar")}
      <div class="upi"><span><b>Öncelik</b><small>Girdi kıtsa önce yüksek öncelikli fabrika alır</small></span><span class="seg">${[1, 2, 3].map(p => btn("prio", b.id + ":" + p, ["Düşük", "Normal", "Yüksek"][p - 1], b.priority === p ? "on" : "")).join("")}</span></div></div>
    <div class="row end">${btn("toggleF", b.id, b.on ? "❚❚ Durdur" : "▶ Çalıştır", "")}${btn("confirm", "demolish:" + b.id, `Sat · ${tl(Fi.factoryValue(s, b) * 0.6)}`, "ghost danger")}</div>`;
  return h;
}
const up = (s, b, what, label, cost, max, desc) => `<div class="upi"><span><b>${label}</b><small>${desc}</small></span>${max ? chip("En üst") : btn("upF", b.id + ":" + what, tl(cost), s.company.cash >= cost ? "gold sm" : "sm", s.company.cash < cost)}</div>`;
const tog = (a, id, on, label, desc) => `<div class="upi"><span><b>${label}</b><small>${desc}</small></span>${btn(a, id, on ? "Açık" : "Kapalı", on ? "on sm" : "sm")}</div>`;

// ---------------- DEPOLAR ----------------
export function depotsView(s, V) {
  const reg = s.regions[V.dreg] && s.regions[V.dreg].owned ? V.dreg : "anadolu", cap = W.capacitySummary(s, reg);
  let h = `<div class="sumbar">${ownedRegions(s).map(r => btn("dreg", r.id, r.short, r.id === reg ? "on" : "")).join("")}<span>Toplam <b>${n0(cap.used)} / ${n0(cap.total)}</b> hacim</span>${btn("sheet", "build:" + reg, "＋ Yeni depo", "gold sm")}</div>
    <div class="split"><div class="pane left"><div class="lbl">KAPASİTE</div>${Object.entries(cap.classes).map(([c, v]) => `<div class="capr"><span>${STORAGE_CLASSES[c]}</span>${bar(v.cap ? v.used / v.cap : v.used ? 1 : 0, v.used > v.cap * 0.9 ? "full" : "")}<small>${n0(v.used)} / ${n0(v.cap)}</small></div>`).join("") || `<div class="hint">Bu bölgede depon yok.</div>`}
      <div class="lbl">DEPOLAR</div>${W.warehousesIn(s, reg).map(b => `<button class="wcard" data-a="sheet" data-v="b:${b.id}">${art(WT[b.type].look, 84, 58)}<span><b>${WT[b.type].name}</b><small>Seviye ${b.level} · ${n0(W.whCap(s, b))} hacim · güvenlik ${b.security}/3${b.racks ? " · raflı" : ""}</small></span></button>`).join("")}</div>
    <div class="pane right"><div class="lbl">STOK · ${RG[reg].short.toLocaleUpperCase("tr")}</div><table class="tbl"><thead><tr><th></th><th>Ürün</th><th>Miktar</th><th>Ort. maliyet</th><th>Kalite</th><th>Değer</th><th>K/Z</th><th></th></tr></thead><tbody>`;
  const items = Object.entries(s.inventory[reg]).sort((a, b) => b[1].q * s.market[b[0]].price - a[1].q * s.market[a[0]].price);
  for (const [id, it] of items) { const p = s.market[id].price, auto = s.autoStock.find(a => a.region === reg && a.res === id);
    h += `<tr><td>${iconSvg(id, 22)}</td><td>${RES[id].name}${auto ? `<br><small class="muted">oto: ${n0(auto.min)}–${n0(auto.max)}</small>` : ""}</td><td>${qtyTxt(id, it.q)}</td><td>${n0(it.cost)}</td><td>${n0(it.ql)}</td><td>${tl(it.q * p)}</td><td class="${p >= it.cost ? "good" : "bad"}">${pct(p / it.cost - 1)}</td>
      <td class="acts">${btn("sheet", `sell:${reg}:${id}`, "Sat", "sm")}${ownedRegions(s).length > 1 ? btn("sheet", `move:${reg}:${id}`, "Taşı", "sm") : ""}${btn("sheet", `auto:${reg}:${id}`, "Oto", "sm ghost")}</td></tr>`; }
  if (!items.length) h += `<tr><td colspan="8" class="muted">Depo boş. Piyasa'dan mal al ya da üret.</td></tr>`;
  const inc = s.shipments.filter(x => x.to === reg);
  h += `</tbody></table>${inc.length ? `<div class="lbl">YOLDA (${inc.length})</div>${inc.map(sh => `<div class="line">${sh.items.map(i => iconSvg(i.res, 18) + qtyTxt(i.res, i.q)).join(" ")} <span class="muted small">${sh.kind === "purchase" ? "tedarikçiden" : RG[sh.from] ? RG[sh.from].short + "'dan" : ""} · ${n1(Math.max(0, (sh.arrive - s.time.min) / 60))} sa</span></div>`).join("")}` : ""}</div></div>`;
  return h;
}
export function warehouseSheet(s, b) {
  const t = WT[b.type], cap = W.capacitySummary(s, b.region);
  return `<div class="shead">${art(t.look, 150, 104)}<div><h3>${t.name}</h3><span class="muted small">${RG[b.region].name} · arsa ${b.plot + 1}</span><div class="small">${t.accepts.map(c => STORAGE_CLASSES[c]).join(", ")} · ${n0(W.whCap(s, b))} hacim · gider ${tl(t.maint * (1 + 0.3 * (b.level - 1)))}/gün + ${n1(t.rate)} ₺/hacim</div></div></div>
    <div class="small muted">${t.desc}</div><div class="lbl">GELİŞTİRME</div><div class="ups">
    <div class="upi"><span><b>Depo seviyesi ${b.level}/5</b><small>Kapasite +%60</small></span>${b.level >= 5 ? chip("En üst") : btn("upW", b.id + ":level", tl(W.whUpgradeCost(s, b)), "gold sm", s.company.cash < W.whUpgradeCost(s, b))}</div>
    <div class="upi"><span><b>Raf sistemi</b><small>Kapasite +%20</small></span>${b.racks ? chip("Kurulu", "g") : btn("upW", b.id + ":racks", tl(t.cost * 0.4), "gold sm", s.company.cash < t.cost * 0.4)}</div>
    <div class="upi"><span><b>Güvenlik ${b.security}/3</b><small>Kamera, bekçi, alarm: hırsızlık −%30/seviye</small></span>${b.security >= 3 ? chip("En üst") : btn("upW", b.id + ":security", tl(300000 * (b.security + 1)), "gold sm", s.company.cash < 300000 * (b.security + 1))}</div></div>
    <div class="lbl">BÖLGE DOLULUĞU</div>${bar(cap.used / Math.max(1, cap.total))}<div class="small">${n0(cap.used)} / ${n0(cap.total)} hacim</div>
    <div class="row end">${btn("go", "depots", "Stoğu gör", "")}${btn("confirm", "sellW:" + b.id, `Depoyu sat · ${tl(t.cost * 0.5)}`, "ghost danger")}</div>`;
}
export function sellSheet(s, V, reg, id) {
  const it = item(s, reg, id), q = Math.min(V.sq ?? it.q, it.q), qt = q > 0 ? M.quote(s, id, q, "sell", reg) : null;
  return `<div class="shead">${iconSvg(id, 40)}<div><h3>${RES[id].name} sat</h3><span class="muted small">${RG[reg].short} · stok ${qtyTxt(id, it.q)} · ort. maliyet ${n0(it.cost)} ₺</span></div></div>
    <div class="row">${[0.25, 0.5, 1].map(f => btn("sq", Math.floor(it.q * f * 10) / 10, f === 1 ? "Tümü" : "%" + f * 100, "")).join("")}<input data-f="sq" type="text" inputmode="decimal" step="any" value="${n2(q).replace(/\./g, "").replace(",", ".")}"></div>
    ${qt ? `<div class="quote"><div><span>Birim</span><b>${n0(qt.unit)} ₺</b></div><div><span>Komisyon+vergi</span><b>${tl(qt.commission + qt.tax)}</b></div><div><span>Nakliye</span><b>${tl(qt.fee)}</b></div><div class="tot"><span>Net</span><b>${tlFull(qt.total)}</b></div><div><span>Kâr/zarar</span><b class="${qt.total - it.cost * q >= 0 ? "good" : "bad"}">${tl(qt.total - it.cost * q)}</b></div></div>` : ""}
    ${btn("doSell", `${reg}:${id}:${q}`, "Sat", "sellb big", !(q > 0))}`;
}
export function moveSheet(s, V, reg, id) {
  const it = item(s, reg, id), to = V.mto && V.mto !== reg && s.regions[V.mto].owned ? V.mto : ownedRegions(s).find(r => r.id !== reg)?.id, q = Math.min(V.mq ?? it.q, it.q);
  const modes = to ? L.modesFor(s, reg, to, RES[id].vol * q) : [];
  return `<div class="shead">${iconSvg(id, 40)}<div><h3>${RES[id].name} taşı</h3><span class="muted small">${RG[reg].short} → ${to ? RG[to].short : "—"} · ${to ? n0(L.km(reg, to)) + " km" : ""}</span></div></div>
    <div class="row"><label>Hedef ${`<select data-f="mto">${ownedRegions(s).filter(r => r.id !== reg).map(r => `<option value="${r.id}" ${r.id === to ? "selected" : ""}>${r.short}</option>`).join("")}</select>`}</label><input data-f="mq" type="text" inputmode="decimal" step="any" value="${Math.round(q * 10) / 10}"> <span class="muted small">en fazla ${qtyTxt(id, it.q)}</span></div>
    <div class="modes">${modes.map(m => `<button class="mode ${m.ok ? "" : "dis"}" data-a="doMove" data-v="${reg}:${id}:${to}:${m.mode}:${q}" ${m.ok ? "" : "disabled"}><b>${m.name}</b><small>${tl(m.cost)} · ${n1(m.hours)} sa${m.mode !== "nakliyeci" ? ` · ${m.n} araç (${m.idle} boşta)` : ""}</small></button>`).join("")}</div>`;
}
export function autoSheet(s, V, reg, id) {
  const a = s.autoStock.find(x => x.region === reg && x.res === id) || { min: 0, max: 0 };
  return `<div class="shead">${iconSvg(id, 40)}<div><h3>Otomatik stok · ${RES[id].name}</h3><span class="muted small">${RG[reg].short}. Her gün kontrol edilir: alt sınırın altına inince alınır, üst sınırı aşınca satılır. 0 = kapalı.</span></div></div>
    <div class="row"><label>Alt sınır <input data-f="amin" type="text" inputmode="decimal" step="any" value="${a.min}"></label><label>Üst sınır <input data-f="amax" type="text" inputmode="decimal" step="any" value="${a.max}"></label></div>
    ${btn("setAuto", `${reg}:${id}`, "Kaydet", "gold")}`;
}

// ---------------- LOJİSTİK ----------------
export function logisticsView(s, V) {
  const home = s.regions[V.vreg] && s.regions[V.vreg].owned ? V.vreg : "anadolu";
  let h = `<div class="cols3"><div class="pane"><div class="lbl">FİLO · ${s.fleet.length} araç</div>
    <div class="row">Yeni araç bölgesi ${regionSelect(s, "vreg", home)}</div>`;
  for (const v of VEHICLES) {
    const mine = s.fleet.filter(f => f.type === v.id), lock = v.unlock && level(s) < v.unlock.level ? `Seviye ${v.unlock.level}` : v.research && !has(s, v.research) ? "Araştırma gerekir" : v.needs && !RG[home][v.needs] ? (v.needs === "rail" ? "Demiryolu yok" : v.needs === "port" ? "Liman yok" : "Havalimanı yok") : null;
    h += `<div class="vitem">${art("v_" + v.id, 74, 44)}<span><b>${v.name}</b><small>${n0(v.cap)} hacim · ${v.speed} km/sa · ${v.fuel} ₺/km · ${tl(v.maint)}/gün</small>${mine.length ? `<small>${mine.length} araç: ${mine.map(f => `${RG[f.home].short}${f.busyUntil > s.time.min ? " (seferde)" : ""}`).join(", ")}</small>` : ""}</span>
      ${lock ? chip(lock) : btn("buyV", `${v.id}:${home}`, tl(v.price), s.company.cash >= v.price ? "gold sm" : "sm", s.company.cash < v.price)}${mine.some(f => !(f.busyUntil > s.time.min)) ? btn("sellV", mine.find(f => !(f.busyUntil > s.time.min)).id, "Sat", "ghost sm") : ""}</div>`;
  }
  h += `</div><div class="pane"><div class="lbl">SEVKİYATLAR · ${s.shipments.length}</div>`;
  for (const sh of s.shipments.slice().sort((a, b) => a.arrive - b.arrive)) {
    const f = Math.max(0, Math.min(1, (s.time.min - sh.depart) / Math.max(1, sh.arrive - sh.depart)));
    const from = sh.kind === "purchase" ? (SUPPLIERS.find(x => x.id === sh.label) || {}).name || "Tedarikçi" : RG[sh.from]?.short, to = RG[sh.to]?.short || CITY[sh.to]?.name;
    h += `<div class="ship"><span>${sh.items.map(i => iconSvg(i.res, 18) + " " + qtyTxt(i.res, i.q)).join(" ")}</span><small>${from} → ${to} · ${sh.mode === "nakliyeci" ? "nakliyeci" : sh.mode === "tedarikci" ? "tedarikçi" : VH[sh.mode]?.name} · ${n1(Math.max(0, (sh.arrive - s.time.min) / 60))} sa kaldı</small>${bar(f)}</div>`;
  }
  if (!s.shipments.length) h += `<div class="hint">Yolda sevkiyat yok.</div>`;
  h += `</div><div class="pane"><div class="lbl">OTOMATİK ROTALAR</div>`;
  for (const r of s.routes) h += `<div class="line">${iconSvg(r.res, 18)} <span>${RG[r.from].short} → ${RG[r.to].short}: ${qtyTxt(r.res, r.q)} / ${r.every} gün · ${r.mode === "nakliyeci" ? "nakliyeci" : VH[r.mode].name}${r.keep ? ` · ${n0(r.keep)} bırak` : ""}<br><small class="muted">${esc(r.lastMsg || "Bekliyor")}</small></span>${btn("delRoute", r.id, "Sil", "ghost sm")}</div>`;
  const regs = ownedRegions(s);
  if (regs.length > 1) { const rf = V.rf || {};
    h += `<div class="card"><b>Yeni rota</b><div class="row"><label>Nereden <select data-f="rf.from">${regs.map(r => `<option value="${r.id}" ${r.id === (rf.from || regs[0].id) ? "selected" : ""}>${r.short}</option>`).join("")}</select></label><label>Nereye <select data-f="rf.to">${regs.map(r => `<option value="${r.id}" ${r.id === (rf.to || regs[1].id) ? "selected" : ""}>${r.short}</option>`).join("")}</select></label></div>
      <div class="row"><label>Ürün <select data-f="rf.res">${RESOURCES.filter(M.tradeable).map(r => `<option value="${r.id}" ${r.id === (rf.res || "kereste") ? "selected" : ""}>${r.name}</option>`).join("")}</select></label></div>
      <div class="row"><label>Miktar <input data-f="rf.q" type="text" inputmode="decimal" value="${rf.q || 20}"></label><label>Her <select data-f="rf.every">${[1, 2, 3, 7].map(d => `<option value="${d}" ${+(rf.every || 1) === d ? "selected" : ""}>${d} gün</option>`).join("")}</select></label><label>Bırak <input data-f="rf.keep" type="text" inputmode="decimal" value="${rf.keep || 0}"></label></div>
      <div class="row"><label>Araç <select data-f="rf.mode"><option value="nakliyeci">Kiralık nakliyeci</option>${VEHICLES.map(v => `<option value="${v.id}" ${rf.mode === v.id ? "selected" : ""}>${v.name}</option>`).join("")}</select></label>${btn("addRoute", "", "Rotayı ekle", "gold sm")}</div></div>`; }
  else h += `<div class="hint">Rota kurmak için ikinci bir bölge aç (Şirket › Bölgeler).</div>`;
  return h + `</div></div>`;
}

// ---------------- ŞİRKET ----------------
const CO = [["orders", "📦", "Siparişler", "Teklifler, sözleşmeler, ihaleler"], ["finance", "📊", "Finans", "Ciro, kâr, değer, vergiler"], ["loans", "🏦", "Krediler", "Banka teklifleri ve borçlar"], ["staff", "👷", "Personel", "İşe alım, eğitim, maaşlar"], ["research", "🔬", "Araştırma", "Teknoloji ağacı"], ["rivals", "🏁", "Rakipler", "Sıralama ve pazar payı"], ["regions", "🌍", "Bölgeler", "Yeni sanayi bölgeleri"], ["quests", "🎯", "Görevler", "Günlük, haftalık, etkinlik"], ["ach", "🏆", "Başarımlar", "Kilometre taşları"], ["events", "⚡", "Olaylar", "Piyasayı etkileyen gelişmeler"], ["settings", "⚙️", "Ayarlar", "Ses, kayıt, yeni oyun"]];
export function companyView(s, V) {
  const sub = V.co;
  if (!sub) return `<div class="cogrid">${CO.map(([k, ic, t, d]) => `<button class="cotile" data-a="go" data-v="${k}"><span class="ic">${ic}</span><span><b>${t}</b><small>${d}</small></span></button>`).join("")}</div>`;
  const head = `<div class="cohead">${btn("go", "company", "‹ Şirket", "ghost sm")}<h2>${(CO.find(c => c[0] === sub) || [])[2]}</h2></div>`;
  const fn = { orders: ordersView, finance: financeView, loans: loansView, staff: staffView, research: researchView, rivals: rivalsView, regions: regionsView, quests: questsView, ach: achView, events: eventsView, settings: settingsView }[sub];
  return head + fn(s, V);
}
function ordersView(s, V) {
  let h = `<div class="split"><div class="pane left"><div class="lbl">TEKLİFLER · ${s.offers.length}</div>`;
  for (const o of s.offers) { const K = CONTRACT_KINDS[o.kind], have = ownedRegions(s).reduce((a, r) => a + qty(s, r.id, o.res), 0), val = o.price * o.q * o.deliveries, mp = s.market[o.res].price;
    h += `<div class="offer ${o.big ? "big" : ""}"><div class="oh">${iconSvg(o.res, 30)}<span><b>${esc(o.cust)}</b><small>${CT[o.type].name} · ${K.name} · ${CITY[o.city].name}</small></span><span class="ov"><b>${tl(val)}</b><small class="${o.price >= mp ? "good" : "bad"}">${n0(o.price)} ₺/${RES[o.res].unit} · piyasa ${pct(o.price / mp - 1)}</small></span></div>
      <div class="chips">${chip(qtyTxt(o.res, o.q) + (o.deliveries > 1 ? ` × ${o.deliveries} teslimat` : ""))}${chip("kalite ≥ " + o.minQ, "s")}${chip(o.leadDays + " gün süre")}${o.export ? chip("İhracat", "p") : ""}${chip(have >= o.q ? "Stokta var" : "Stok: " + qtyTxt(o.res, have), have >= o.q ? "g" : "w")}${o.kind === "ihale" ? chip(`İhale ${o.tenderDay - day(s)} gün sonra · tavan ${n0(o.price)} ₺`, "w") : chip("teklif " + Math.max(0, o.expires - day(s)) + " gün geçerli")}</div>
      <div class="row end">${o.kind === "ihale" ? (o.myBid ? chip(`Teklifin: ${n0(o.myBid)} ₺`, "g") : `<input data-f="bid.${o.id}" type="text" inputmode="decimal" value="${Math.round(mp * 1.02)}" aria-label="Birim teklif">${btn("bid", o.id, "Teklif ver (%2 teminat)", "gold sm")}`) : btn("accept", o.id, "Kabul et", "gold sm")}${btn("decline", o.id, "Reddet", "ghost sm")}</div></div>`; }
  if (!s.offers.length) h += `<div class="hint">Şu an teklif yok. Her gün yeni teklifler gelir; satış uzmanı ve itibar teklif sayısını artırır.</div>`;
  h += `</div><div class="pane right"><div class="lbl">SÖZLEŞMELER</div>`;
  const act = s.contracts.filter(c => c.status === "active");
  for (const c of act) { const left = c.due - day(s), regs = ownedRegions(s).filter(r => qty(s, r.id, c.res) >= c.q), sel = V["dr" + c.id] || (regs[0] && regs[0].id) || "anadolu";
    h += `<div class="offer"><div class="oh">${iconSvg(c.res, 30)}<span><b>${esc(c.cust)}</b><small>${CONTRACT_KINDS[c.kind].name} · ${CITY[c.city].name} · ${c.done}/${c.deliveries} teslim${c.missed ? ` · ${c.missed} kaçırıldı` : ""}</small></span><span class="ov"><b>${qtyTxt(c.res, c.q)}</b><small class="${left < 0 ? "bad" : left <= 1 ? "warn" : ""}">${left < 0 ? `${-left} gün gecikti` : left === 0 ? "bugün son gün" : left + " gün kaldı"}</small></span></div>
      ${bar(c.done / c.deliveries)}<div class="row end">${c.inTransit ? chip("Yolda", "s") : `<select data-f="dr${c.id}">${ownedRegions(s).map(r => `<option value="${r.id}" ${r.id === sel ? "selected" : ""}>${r.short} (${qtyTxt(c.res, qty(s, r.id, c.res))})</option>`).join("")}</select>${btn("deliver", c.id, "Teslim et", "gold sm")}`}${btn("autoC", c.id, c.auto ? "Oto teslim: açık" : "Oto teslim: kapalı", c.auto ? "on sm" : "sm")}</div></div>`; }
  if (!act.length) h += `<div class="hint">Aktif sözleşme yok. Teklif kabul et; teslim edilmezse ceza ve itibar kaybı olur.</div>`;
  const done = s.contracts.filter(c => c.status !== "active");
  if (done.length) h += `<div class="lbl">BİTENLER</div>${done.slice(-5).map(c => `<div class="line"><span>${esc(c.cust)} · ${RES[c.res].name}</span>${chip(c.status === "done" ? "Tamamlandı" : "Feshedildi", c.status === "done" ? "g" : "b")}</div>`).join("")}`;
  return h + `</div></div>`;
}
function financeView(s, V) {
  const a = Fi.assetValues(s), v = Fi.companyValue(s), days = s.ledger.days.slice(-30), last = days[days.length - 1] || { rev: 0, net: 0 };
  const m = {}; for (const d of days) for (const [k, x] of Object.entries(d.cats)) m[k] = (m[k] || 0) + x;
  const inc = Object.entries(m).filter(([, x]) => x > 0 && !["Kredi"].includes(...[])).sort((p, q) => q[1] - p[1]), exp = Object.entries(m).filter(([, x]) => x < 0).sort((p, q) => p[1] - q[1]);
  const share = marketShare(s), mx = Math.max(1, ...days.map(d => Math.abs(d.net)));
  return `<div class="kpis">${[["Toplam nakit", tl(s.company.cash), s.company.cash < 0 ? "bad" : ""], ["Şirket değeri", tl(v)], ["Günlük ciro", tl(last.rev)], ["Günlük net kâr", tl(last.net), last.net >= 0 ? "good" : "bad"], ["Aylık gelir (30 g)", tl(inc.reduce((p, x) => p + x[1], 0))], ["Aylık gider (30 g)", tl(-exp.reduce((p, x) => p + x[1], 0))],
    ["Fabrika değeri", tl(a.fac)], ["Depo değeri", tl(a.wh)], ["Stok değeri", tl(a.stock)], ["Araç ve arsa", tl(a.veh + a.land)], ["Borçlar", tl(Fi.debt(s)), Fi.debt(s) ? "bad" : ""], ["Son ay vergisi", tl(s.ledger.lastTax || 0)], ["Pazar payı", n1(share.me * 100) + "%"], ["Kredi notu", n0(s.company.credit) + "/100"], ["Faiz ortamı", "%" + n2(s.econ.interest * 100) + "/ay · " + (s.econ.state || "")], ["Kredi limiti (işletme)", tl(Fi.creditLine(s))]]
    .map(([k, x, c]) => `<div class="kpi"><span>${k}</span><b class="${c || ""}">${x}</b></div>`).join("")}</div>
    <div class="split"><div class="pane left"><div class="lbl">SON 30 GÜN NET KÂR</div><div class="bars">${days.map(d => `<i class="${d.net >= 0 ? "pos" : "neg"}" style="height:${Math.abs(d.net) / mx * 100}%" title="Gün ${d.day + 1}: ${tl(d.net)}"></i>`).join("")}</div>
      ${s.company.rescue ? `<div class="card danger"><b>İflasın eşiğindesin</b><span class="small">Kasa eksi bakiye sınırının altında. Kurtarma seçenekleri:</span>${Fi.rescueOptions(s).map(o => `<div class="upi"><span><b>${o.name}</b><small>${o.desc}</small></span>${btn("rescue", o.id, tl(o.amount), "gold sm")}</div>`).join("")}</div>` : ""}</div>
    <div class="pane right"><div class="lbl">GELİRLER (30 GÜN)</div>${inc.map(([k, x]) => `<div class="line"><span>${k}</span><b class="good">${tl(x)}</b></div>`).join("") || `<div class="muted small">—</div>`}<div class="lbl">GİDERLER (30 GÜN)</div>${exp.map(([k, x]) => `<div class="line"><span>${k}</span><b class="bad">${tl(x)}</b></div>`).join("") || `<div class="muted small">—</div>`}</div></div>`;
}
function loansView(s, V) {
  return `<div class="split"><div class="pane left"><div class="lbl">BANKA TEKLİFLERİ</div>${Fi.loanOffers(s).map(o => { const amt = Math.min(o.max, V["la" + o.id] || Math.round(o.max / 2 / 100000) * 100000), term = V["lt" + o.id] || o.terms[0];
    return `<div class="card"><div class="line"><b>${o.name}</b><span>aylık faiz %${n2(o.rate * 100)} · en fazla ${tl(o.max)}</span></div>
      <div class="row">${[0.25, 0.5, 1].map(f => btn("loanAmt", o.id + ":" + Math.round(o.max * f / 100000) * 100000, tl(Math.round(o.max * f / 100000) * 100000), Math.abs(amt - Math.round(o.max * f / 100000) * 100000) < 1 ? "on sm" : "sm")).join("")}<select data-f="lt${o.id}">${o.terms.map(t => `<option value="${t}" ${t === +term ? "selected" : ""}>${t} ay</option>`).join("")}</select></div>
      <div class="row end"><span class="small muted">Aylık taksit ≈ ${tl(amt * o.rate / (1 - Math.pow(1 + o.rate, -term)))}</span>${btn("loan", `${o.id}:${amt}:${term}`, "Krediyi çek", "gold sm", !(amt > 0))}</div></div>`; }).join("")}
    <div class="hint">Faiz ekonomik döngüye ve olaylara göre değişir; kredi notun ve finans yöneticin faizi düşürür. Taksitler ay sonunda ödenir.</div></div>
    <div class="pane right"><div class="lbl">KREDİLERİN</div>${s.loans.map(l => `<div class="card"><div class="line"><b>${esc(l.bank)}</b><span>kalan ${tl(l.left)} · %${n2(l.rate * 100)}/ay · ${l.paid}/${l.months} taksit</span></div>${bar(l.paid / l.months)}<div class="row end"><span class="small">Taksit ${tl(l.pay)}</span>${btn("repay", l.id, `Kapat · ${tl(l.left * 1.01)}`, "sm", s.company.cash < l.left * 1.01)}</div></div>`).join("") || `<div class="hint">Borcun yok.</div>`}</div></div>`;
}
function staffView(s, V) {
  const need = E.workersNeeded(s);
  return `<div class="sumbar"><span>İşçi ihtiyacı <b>${n0(need)}</b></span><span>İşçi <b class="${s.staff.isci.n < need ? "bad" : "good"}">${n0(s.staff.isci.n)}</b></span><span>Aylık maaş <b>${tl(E.monthlyPayroll(s))}</b></span><span>Verim <b>${n0(E.workerEfficiency(s) * 100)}%</b></span></div>
    <table class="tbl staff"><thead><tr><th>Pozisyon</th><th>Kişi</th><th>Eğitim</th><th>Maaş</th><th>Etki</th><th></th></tr></thead><tbody>${POSITIONS.map(p => { const st = s.staff[p.id], eff = E.staffEff(s, p.id);
      return `<tr><td><b>${p.name}</b><br><small class="muted">${p.desc}</small></td><td>${st.n}</td><td>${st.lvl}/${TRAINING.maxLevel}${st.training ? ` <small>(${st.training} gün)</small>` : ""}</td><td>${tl(p.salary)}</td><td>${p.per ? (p.effect === "quality" ? "+" + n1(eff) + " kalite" : "%" + n1(eff * 100)) : `${n0(Math.min(1, st.n / Math.max(1, need)) * 100)}% kapasite`}</td>
        <td class="acts">${btn("hire", p.id + ":1", "+1", "sm")}${btn("hire", p.id + ":5", "+5", "sm")}${p.id === "isci" ? btn("hire", p.id + ":" + Math.max(1, need - st.n), "İhtiyaç kadar", "gold sm", need <= st.n) : ""}${btn("fire", p.id, "−1", "ghost sm", !st.n)}${btn("train", p.id, `Eğit · ${tl(E.trainingCost(s, p.id))}`, "sm", !st.n || st.lvl >= TRAINING.maxLevel || !!st.training)}</td></tr>`; }).join("")}</tbody></table>
    <div class="hint">İşe alım gideri çeyrek maaş, çıkarma tazminatı yarım maaştır. Maaşlar her gün tahakkuk eder. Eğitim seviye başına etkiyi %10 artırır.</div>`;
}
function researchView(s, V) {
  const depth = id => { const r = RS[id]; return r.req.length ? 1 + Math.max(...r.req.map(depth)) : 0; }, cols = [];
  for (const r of RESEARCH) { const d = depth(r.id); (cols[d] = cols[d] || []).push(r); }
  const a = s.research.active;
  return `${a ? `<div class="card"><b>Sürüyor: ${RS[a.id].name}</b>${bar(1 - a.left / (RS[a.id].days * 24))}<span class="small">kalan ≈ ${n1(a.left / Rs.researchSpeed(s) / 24)} gün · hız ×${n2(Rs.researchSpeed(s))}</span></div>` : ""}
    <div class="tree">${cols.map(c => `<div class="tcol">${c.map(r => { const done = s.research.done[r.id], av = Rs.available(s, r.id), act = a && a.id === r.id;
      return `<div class="tnode ${done ? "done" : act ? "act" : av ? "av" : "lock"}"><b>${r.name}</b><small>${r.desc}</small><small>${r.req.length ? "Gerekir: " + r.req.map(x => RS[x].name).join(", ") : "Ön koşul yok"}</small>${done ? chip("Tamamlandı", "g") : act ? chip("Sürüyor", "s") : btn("research", r.id, `${tl(Rs.researchCost(s, r.id))} · ${r.days} gün`, av && !a ? "gold sm" : "sm", !av || !!a || s.company.cash < Rs.researchCost(s, r.id))}</div>`; }).join("")}</div>`).join("")}</div>`;
}
function rivalsView(s, V) {
  const rows = ranking(s), sh = marketShare(s), mx = Math.max(...rows.map(r => r.value));
  return `<div class="split"><div class="pane left"><div class="lbl">ŞİRKET DEĞERİ SIRALAMASI</div>${rows.map((r, i) => { const rv = s.rivals.find(x => x.id === r.id);
    return `<div class="rank ${r.me ? "me" : ""}"><span class="no">${i + 1}</span><span><b>${esc(r.name)}</b><small>${r.me ? "Sen" : { ucuz: "Düşük maliyet", kalite: "Yüksek kalite", hacim: "Hacimli ticaret", uzman: "Uzman zincir" }[rv.strategy] + " · " + rv.focus.slice(0, 3).map(x => RES[x].name).join(", ")}</small>${bar(r.value / mx)}</span><b>${tl(r.value)}</b></div>`; }).join("")}
    ${s.rivals.filter(r => !r.alive).map(r => `<div class="rank dead"><span class="no">—</span><span><b>${esc(r.name)}</b><small>İflas etti</small></span></div>`).join("")}</div>
    <div class="pane right"><div class="lbl">PAZAR PAYI (CİRO)</div><div class="share"><i style="width:${sh.me * 100}%;background:var(--gold)"></i>${sh.rows.map(r => `<i style="width:${r.share * 100}%;background:${r.color}"></i>`).join("")}</div>
      <div class="legend"><span><i style="background:var(--gold)"></i>Sen ${n1(sh.me * 100)}%</span>${sh.rows.sort((a, b) => b.share - a.share).map(r => `<span><i style="background:${r.color}"></i>${esc(r.name)} ${n1(r.share * 100)}%</span>`).join("")}</div>
      <div class="hint">Rakipler fiyat yükselince kapasite artırır, düşünce kısar. Tüccar rakipler ucuzken stok yapıp pahalıyken satar. İhalelerde rakiplerle yarışırsın. Batan rakibin arzı piyasadan çekilir.</div></div></div>`;
}
function regionsView(s, V) {
  return `<div class="rgrid">${REGIONS.map(r => { const own = s.regions[r.id].owned, n = Object.values(s.buildings).filter(b => b.region === r.id).length;
    return `<div class="rcard ${own ? "own" : ""}" style="--rc:${r.color}"><b>${r.name}</b><small>${r.perk}</small><small>${r.port ? "Liman · " : ""}${r.rail ? "Demiryolu · " : ""}${r.airport ? "Havalimanı · " : ""}${r.cols * r.rows} arsa · arsa ${tl(r.plotPrice)}</small>
      <small>${r.mines.length ? "Madenler: " + r.mines.map(m => RES[RECIPE[m].res].name).join(", ") : ""}${r.wells.length ? " Kuyular: petrol, gaz" : ""}${r.farm ? " Tarım ve orman" : ""}</small>
      ${own ? `<div class="row">${chip(n + " tesis", "g")}${btn("mapRegionGo", r.id, "Haritada aç", "sm")}</div>` : btn("unlock", r.id, level(s) < r.level ? `Seviye ${r.level} gerekir` : `Aç · ${tl(r.cost)}`, level(s) >= r.level ? "gold sm" : "sm", level(s) < r.level || s.company.cash < r.cost)}</div>`; }).join("")}</div>`;
}
function questsView(s, V) {
  const q = x => `<div class="quest ${x.done ? "done" : ""}"><span><b>${esc(x.text)}</b>${bar(Math.min(1, x.prog / x.n))}<small>${x.kind === "profit" ? tl(Math.min(x.prog, x.n)) + " / " + tl(x.n) : n1(Math.min(x.prog, x.n)) + " / " + n1(x.n)}</small></span>${x.claimed ? chip("Alındı", "g") : btn("claim", x.id, `Ödül · ${tl(x.reward)}`, x.done ? "gold sm" : "sm", !x.done)}</div>`;
  const sp = s.special;
  return `<div class="split"><div class="pane left"><div class="lbl">GÜNLÜK GÖREVLER</div>${s.quests.daily.map(q).join("")}<div class="lbl">HAFTALIK GÖREVLER</div>${s.quests.weekly.map(q).join("")}</div>
    <div class="pane right"><div class="lbl">HAFTANIN ETKİNLİĞİ</div>${sp ? `<div class="card"><b>${sp.name}</b><span class="small">${sp.desc}</span></div>` : `<div class="hint">Etkinlik haftaya başlar.</div>`}
      <div class="hint">Görevler her gün ve her hafta yenilenir; ödüller seviyene göre büyür. Özel etkinlikler her hafta döner: ticaret haftası, sanayi fuarı, büyük ihaleler, üretim yarışması.</div></div></div>`;
}
function achView(s) { return `<div class="agrid">${ACHIEVEMENTS.map(a => `<div class="ach ${s.achievements[a.id] ? "on" : ""}"><span class="ic">${s.achievements[a.id] ? "🏆" : "🔒"}</span><span><b>${a.name}</b><small>${a.desc}</small></span></div>`).join("")}</div>`; }
function eventsView(s) {
  const ev = activeEvents(s);
  return `<div class="split"><div class="pane left"><div class="lbl">ŞU AN</div>${ev.map(e => `<div class="card ${e.good ? "good" : ""}"><b>${e.name}</b><span class="small">${esc(e.desc)}</span><small class="muted">${n1((e.until - s.time.min) / 1440)} gün kaldı</small></div>`).join("") || `<div class="hint">Şu an etkin olay yok. Piyasa olağan seyrinde.</div>`}</div>
    <div class="pane right"><div class="lbl">GEÇMİŞ</div>${s.notes.filter(n => n.kind === "event").slice(0, 15).map(n => `<div class="line"><span class="small">${esc(n.text)}</span><small class="muted">gün ${Math.floor(n.t / 1440) + 1}</small></div>`).join("") || `<div class="muted small">—</div>`}</div></div>`;
}
function settingsView(s, V) {
  const t = (k, l) => `<div class="upi"><span><b>${l}</b></span>${btn("setting", k, s.settings[k] ? "Açık" : "Kapalı", s.settings[k] ? "on sm" : "sm")}</div>`;
  return `<div class="split"><div class="pane left">${t("sound", "Ses efektleri")}${t("haptics", "Titreşim")}${t("notify", "Bildirim baloncukları")}
    <div class="upi"><span><b>Kayıt</b><small>Otomatik kayıt her 20 saniyede ve uygulamadan çıkarken yapılır. Bozuk kayıtta yedekten kurtarılır.</small></span>${btn("saveNow", "", "Şimdi kaydet", "sm")}</div>
    <div class="upi"><span><b>Ana menü</b></span>${btn("menu", "", "Ana menüye dön", "sm")}</div>
    <div class="upi"><span><b>Yeni oyun</b><small>Mevcut şirket silinir.</small></span>${btn("confirm", "newGame:", "Yeni oyun", "ghost danger sm")}</div></div>
    <div class="pane right"><div class="lbl">OYUN</div><div class="small">FABRİKA PATRONU · Hammadde İmparatorluğu<br>Kayıt sürümü ${BAL.version} · 1 gerçek saniye = ${BAL.minutesPerRealSecond} oyun dakikası (×1)<br>Çevrimdışı ilerleme en fazla ${BAL.offlineMaxGameHours / 24} oyun günü.</div></div></div>`;
}
