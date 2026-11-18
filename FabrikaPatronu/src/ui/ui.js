// UIManager — ekran yönlendirme, üst bar, alt menü, yan panel, pencereler, eylemler, oyun döngüsü ve kayıt.
import { BAL } from "../data/balance.js";
import { RES } from "../data/resources.js";
import { FT } from "../data/factories.js";
import { WT } from "../data/warehouses.js";
import { RG } from "../data/regions.js";
import { newGame } from "../core/state.js";
import { tickReal, applyOffline, touchReal } from "../core/game.js";
import { saveGame, loadGame } from "../core/save.js";
import * as M from "../core/market.js";
import * as F from "../core/factory.js";
import * as W from "../core/warehouse.js";
import * as Fi from "../core/finance.js";
import * as E from "../core/employees.js";
import * as K from "../core/contracts.js";
import * as L from "../core/logistics.js";
import * as Rs from "../core/research.js";
import { claim } from "../core/quests.js";
import { earn } from "../core/finance.js";
import { level, day } from "../core/util.js";
import { createMap, season, SEASON_NAME } from "./map.js";
import { BUILD, box, truck, train, ship, plane, iso, shadowPass } from "./art.js";
import * as SC from "./screens.js";
import { tl, tlFull, n0, n1, esc, dateTxt, qtyTxt } from "./fmt.js";
import { iconSvg } from "./icons.js";

const $ = s => document.querySelector(s);
let S = null; const V = { tab: "map", region: "anadolu", mapMode: "region", msel: "kereste", qty: 10, side: "buy", otype: "market", sup: "standart", treg: "anadolu", dreg: "anadolu", vreg: "anadolu", co: null, sheet: null, notes: false };
let map, storage, lastHourSeen = -1, dirty = true, lastSave = 0, menuOpen = true;

// ---------- depolama (iOS: Capacitor Preferences, tarayıcı: localStorage) ----------
function makeStorage() {
  const P = window.Capacitor && window.Capacitor.Plugins && window.Capacitor.Plugins.Preferences;
  if (P) return { get: async k => (await P.get({ key: k })).value, set: (k, v) => P.set({ key: k, value: v }) };
  return { get: k => { try { return localStorage.getItem(k); } catch { return null; } }, set: (k, v) => { try { localStorage.setItem(k, v); } catch { } } };
}
async function persist() { if (!S) return; touchReal(S, Date.now()); try { await saveGame(S, storage); lastSave = performance.now(); } catch (e) { toast("Kayıt yapılamadı: " + e.message, "bad"); } }

// ---------- ses ve titreşim ----------
let AC; function beep(kind) { if (!S || !S.settings.sound) return; try { AC = AC || new (window.AudioContext || window.webkitAudioContext)(); const seq = { cash: [[880, 0.05], [1320, 0.09]], ok: [[660, 0.06]], bad: [[220, 0.18]], level: [[523, 0.08], [659, 0.08], [784, 0.14]] }[kind] || [[600, 0.05]]; let t = AC.currentTime; for (const [f, d] of seq) { const o = AC.createOscillator(), g = AC.createGain(); o.type = "triangle"; o.frequency.value = f; g.gain.setValueAtTime(0.07, t); g.gain.exponentialRampToValueAtTime(0.001, t + d); o.connect(g).connect(AC.destination); o.start(t); o.stop(t + d + 0.02); t += d * 0.9; } } catch { } }
function haptic() { if (S && S.settings.haptics && navigator.vibrate) try { navigator.vibrate(10); } catch { } }
export function toast(text, kind = "info") { if (S && !S.settings.notify && kind === "info") return; const t = document.createElement("div"); t.className = "toast " + kind; t.textContent = text; $("#toasts").prepend(t); while ($("#toasts").children.length > 3) $("#toasts").lastChild.remove(); setTimeout(() => t.remove(), 3800); t.onclick = () => t.remove(); }
const result = (r, okText) => { if (r && r.ok) { if (okText) toast(okText, "good"); beep("ok"); haptic(); } else if (r) { toast(r.msg || "Yapılamadı", "bad"); beep("bad"); } dirty = true; return r; };

// ---------- üst bar ve alt menü ----------
const TABS = [["map", "Harita", '<path d="M3 6l6-2 6 2 6-2v14l-6 2-6-2-6 2z"/><path d="M9 4v14M15 6v14"/>'], ["market", "Piyasa", '<path d="M3 17l5-6 4 3 6-8 3 3"/><path d="M3 21h18"/>'], ["factories", "Fabrikalar", '<path d="M3 21V10l5 3v-3l5 3v-3l5 3V4h3v17z"/>'], ["depots", "Depolar", '<path d="M3 10l9-6 9 6v11H3z"/><path d="M7 21v-6h10v6M7 18h10"/>'], ["logistics", "Lojistik", '<path d="M2 16V7h11v9M13 10h4l3 3v3h-7"/><circle cx="6" cy="17.5" r="1.8"/><circle cx="16.5" cy="17.5" r="1.8"/>'], ["company", "Şirket", '<path d="M4 21V5h10v16M14 9h6v12"/><path d="M7 9h4M7 13h4M7 17h4"/>']];
function renderTop() {
  const lv = level(S), x0 = BAL.xpLevels[lv - 1], x1 = BAL.xpLevels[lv] || x0 * 2, d = dateTxt(S.time.min), unread = S.notes.filter(n => !n.read).length, se = season(S);
  $("#top").innerHTML = `<div class="tco"><b>${esc(S.company.name)}</b><span class="tlv"><i>${lv}</i>${BAL.levelNames[lv - 1]}<span class="xp"><i style="width:${Math.min(100, (S.company.xp - x0) / (x1 - x0) * 100)}%"></i></span></span></div>
    <div class="cash ${S.company.cash < 0 ? "neg" : ""}"><span class="coin">₺</span><b>${tlFull(S.company.cash)}</b></div>
    <div class="tstat" title="İtibar"><span>İtibar</span><b class="${S.company.rep < 30 ? "bad" : ""}">${n0(S.company.rep)}</b></div>
    <div class="speed">${BAL.speeds.map((v, i) => `<button data-a="speed" data-v="${i}" class="${S.time.speed === i ? "on" : ""}" aria-label="Hız ${v}">${i === 0 ? "❚❚" : "×" + v}</button>`).join("")}</div>
    <div class="tdate"><b>Gün ${d.day}</b><span>${d.date} · ${d.clock} · ${SEASON_NAME[se]}</span></div>
    <button class="tb" data-a="notes" aria-label="Bildirimler">🔔${unread ? `<i class="bd">${Math.min(99, unread)}</i>` : ""}</button>
    <button class="tb" data-a="menu" aria-label="Menü">☰</button>`;
}
function renderBottom() { $("#bottom").innerHTML = TABS.map(([k, l, ic]) => `<button data-a="tab" data-v="${k}" class="${V.tab === k ? "on" : ""}"><svg viewBox="0 0 24 24" aria-hidden="true">${ic}</svg><span>${l}</span></button>`).join(""); }
function renderView() {
  const v = $("#view"), keep = v.querySelector(".pane.right")?.scrollTop, keepL = v.querySelector(".pane.left")?.scrollTop, keepV = v.scrollTop;
  $("#app").dataset.tab = V.tab;
  if (V.tab === "map") { $("#overlay").innerHTML = SC.mapOverlay(S, V); v.innerHTML = ""; }
  else { $("#overlay").innerHTML = ""; v.innerHTML = V.tab === "market" ? SC.marketView(S, V) : V.tab === "factories" ? SC.factoriesView(S, V) : V.tab === "depots" ? SC.depotsView(S, V) : V.tab === "logistics" ? SC.logisticsView(S, V) : SC.companyView(S, V); }
  const r = v.querySelector(".pane.right"), l = v.querySelector(".pane.left"); if (r && keep) r.scrollTop = keep; if (l && keepL) l.scrollTop = keepL; v.scrollTop = keepV;
  renderSheet(); paintArts();
}
function renderSheet() {
  const sh = $("#sheet"); if (!V.sheet) { sh.hidden = true; sh.innerHTML = ""; return; }
  const [kind, a, b] = V.sheet.split(":"); let html = "";
  if (kind === "b") { const bd = S.buildings[a]; if (!bd) { V.sheet = null; sh.hidden = true; return; } html = bd.kind === "warehouse" ? SC.warehouseSheet(S, bd) : SC.factorySheet(S, bd); }
  else if (kind === "build") html = SC.buildCatalog(S, V, a || V.region, b !== undefined && b !== "" ? +b : undefined);
  else if (kind === "plot") { const reg = S.regions[a], p = reg.plots[+b];
    if (!reg.owned) html = `<h3>${RG[a].name}</h3><div class="hint">Bu bölge kilitli. ${RG[a].perk}.</div><button class="b gold" data-a="unlock" data-v="${a}" ${level(S) < RG[a].level ? "disabled" : ""}>${level(S) < RG[a].level ? `Şirket seviyesi ${RG[a].level} gerekir` : `Bölgeyi aç · ${tl(RG[a].cost)}`}</button>`;
    else if (!p.owned) html = `<h3>Satılık arsa</h3><div class="small muted">${RG[a].name} · arsa ${+b + 1}. Arsa sayısı arttıkça fiyat %15 artar.</div><button class="b gold big" data-a="buyPlot" data-v="${a}:${b}">Satın al · ${tl(F.plotCost(S, a))}</button>`; }
  else if (kind === "sell") html = SC.sellSheet(S, V, a, b);
  else if (kind === "move") html = SC.moveSheet(S, V, a, b);
  else if (kind === "auto") html = SC.autoSheet(S, V, a, b);
  sh.innerHTML = `<div class="shbar"><button class="close" data-a="closeSheet" aria-label="Kapat">✕</button></div>${html}`; sh.hidden = false;
}
// Küçük bina/araç çizimleri (data-art canvas).
function paintArts(root = document) {
  for (const cv of root.querySelectorAll("canvas[data-art]")) {
    const look = cv.dataset.art, w = cv.width, h = cv.height, d = 2; if (!cv._p) { cv.width = w * d; cv.height = h * d; cv.style.width = w + "px"; cv.style.height = h + "px"; cv._p = 1; cv._w = w; cv._h = h; }
    const g = cv.getContext("2d"), W2 = cv._w, H2 = cv._h; g.setTransform(d, 0, 0, d, 0, 0); g.clearRect(0, 0, W2, H2);
    const gr = g.createLinearGradient(0, 0, 0, H2); gr.addColorStop(0, "#22384a"); gr.addColorStop(1, "#16283a"); g.fillStyle = gr; g.fillRect(0, 0, W2, H2);
    const veh = look.startsWith("v_"), sc = veh ? Math.min(W2 / 190, H2 / 120) * 1.05 : Math.min((H2 - 4) / 160, (W2 - 4) / 150); g.save(); if (veh) g.translate(W2 / 2, H2 * 0.32); else g.translate(W2 / 2, 2 + 98 * sc); g.scale(sc, sc);
    if (look.startsWith("v_")) { const v = look.slice(2); g.translate(0, 40); if (v === "tren") train(g, 0.7, -0.3, 4, 0); else if (v === "gemi") ship(g, -0.1, -0.5, 0); else if (v === "ucak") plane(g, 0, -10, 1.8); else { g.scale(2.4, 2.4); truck(g, 0, 0, 1, v === "tir" ? "#c94a3a" : v === "agir" ? "#e0a83a" : "#3f7fa8", "#c9d3dc", v !== "kamyon"); } }
    else { g.translate(0, -40); const gr2 = g.createLinearGradient(0, 0, 0, 80); gr2.addColorStop(0, "#6f7f74"); gr2.addColorStop(1, "#4d5b52"); g.fillStyle = gr2; g.beginPath(); [iso(-0.05, -0.05), iso(1.05, -0.05), iso(1.05, 1.05), iso(-0.05, 1.05)].forEach((p, i) => (i ? g.lineTo(...p) : g.moveTo(...p))); g.closePath(); g.fill();
      const ctx = gg => ({ g: gg, x: 0, y: 0, t: performance.now() / 1000, run: cv.dataset.run === "1", night: false, lvl: +(cv.dataset.lvl || 1), m: 1, snow: false, seed: 3 }), fn = BUILD[look] || BUILD.shed;
      shadowPass(g, ng => fn(ctx(ng))); fn(ctx(g)); }
    g.restore();
  }
}

// ---------- eylemler ----------
const val = f => { const el = document.querySelector(`[data-f="${f}"]`); return el ? el.value : undefined; };
const A = {
  tab: v => { V.tab = v; V.co = null; V.sheet = null; if (v === "map") setTimeout(() => map.fit(), 0); },
  go: v => { const map2 = { orders: ["company", "orders"], quests: ["company", "quests"], staff: ["company", "staff"], finance: ["company", "finance"], company: ["company", null] }[v];
    if (map2) { V.tab = map2[0]; V.co = map2[1]; } else if (["finance", "loans", "research", "rivals", "regions", "ach", "events", "settings"].includes(v)) { V.tab = "company"; V.co = v; } else { V.tab = v; V.co = null; } V.sheet = null; if (V.tab === "map") setTimeout(() => map.fit(), 0); },
  speed: v => { S.time.speed = +v; },
  notes: () => { V.notes = !V.notes; renderNotes(); },
  menu: () => showMenu(),
  closeSheet: () => { V.sheet = null; map.sel = null; },
  sheet: v => { V.sheet = v; },
  mapRegion: v => { V.region = v; V.mapMode = "region"; map.region = v; map.mode = "region"; map.fit(); },
  mapRegionGo: v => { V.tab = "map"; V.co = null; A.mapRegion(v); },
  mapWorld: () => { V.mapMode = "world"; map.mode = "world"; map.fit(); },
  zoom: v => (v === "fit" ? map.fit() : map.zoom(+v)),
  // piyasa
  mcat: v => { V.mcat = v; }, msel: v => { V.msel = v; V.limit = null; }, mwin: v => { V.mwin = v; }, side: v => { V.side = v; if (v === "sell" && V.otype === "bulk") V.otype = "market"; }, otype: v => { V.otype = v; V.limit = null; },
  qty: v => { V.qty = +v; }, qtyMul: v => { V.qty = Math.max(0.1, Math.round(V.qty * +v * 10) / 10); }, qtyAll: v => { V.qty = +v; }, sup: v => { V.sup = v; },
  trade: () => { const id = V.msel, q = +V.qty, reg = V.treg;
    if (V.otype === "market") return result(V.side === "buy" ? M.buy(S, id, q, reg, V.sup) : M.sell(S, id, q, reg), V.side === "buy" ? `${qtyTxt(id, q)} ${RES[id].name} alındı. Teslim sevkiyatlarda.` : `${qtyTxt(id, q)} ${RES[id].name} satıldı.`);
    if (V.otype === "limit") return result(M.placeLimit(S, id, V.side, q, +(V.limit || val("limit")), reg, V.sup), "Limit emri verildi.");
    return result(M.placeBulk(S, id, q, +(V.bdays || 5), reg, V.sup), "Toplu sipariş başladı. İlk parti yarın."); },
  cancelOrder: v => { M.cancelLimit(S, +v); toast("Emir iptal edildi."); },
  // fabrika ve depo
  buildF: v => { const [t, reg, p] = v.split(":"); const r = result(F.buildFactory(S, t, reg, +p), `${FT[t].name} kuruldu.`); if (r.ok) { const b = S.buildings[r.id]; b.autoBuy = b.autoSell = true; b.autoMaint = true; V.sheet = "b:" + r.id; if (E.workersNeeded(S) > S.staff.isci.n) toast(`İşçi gerekli: ${E.workersNeeded(S) - S.staff.isci.n} işçi daha al (Fabrikalar ekranında).`, "warn"); } },
  buildW: v => { const [t, reg, p] = v.split(":"); const r = result(W.buildWarehouse(S, t, reg, +p), `${WT[t].name} kuruldu.`); if (r.ok) V.sheet = "b:" + r.id; },
  buyPlot: v => { const [reg, i] = v.split(":"); const r = result(F.buyPlot(S, reg, +i), "Arsa satın alındı."); if (r.ok) V.sheet = `build:${reg}:${i}`; },
  unlock: v => { const r = result(F.unlockRegion(S, v), `${RG[v].name} açıldı.`); if (r.ok) { beep("level"); A.mapRegionGo(v); V.sheet = null; } },
  upF: v => { const [id, w] = v.split(":"); result(F.upgrade(S, id, w), "Geliştirildi."); },
  upW: v => { const [id, w] = v.split(":"); result(W.upgradeWarehouse(S, id, w), "Depo geliştirildi."); },
  maint: v => result(F.maintain(S, v), "Bakım yapıldı."),
  setRecipe: v => { const [id, r] = v.split(":"); result(F.setRecipe(S, id, r)); },
  prio: v => { const [id, p] = v.split(":"); F.setPriority(S, id, +p); },
  toggleF: v => F.toggle(S, v),
  autoBuy: v => { S.buildings[v].autoBuy = !S.buildings[v].autoBuy; }, autoSell: v => { S.buildings[v].autoSell = !S.buildings[v].autoSell; }, autoMaint: v => { S.buildings[v].autoMaint = !S.buildings[v].autoMaint; },
  dreg: v => { V.dreg = v; },
  sq: v => { V.sq = +v; },
  doSell: v => { const [reg, id, q] = v.split(":"); const r = result(M.sell(S, id, +q, reg), `${RES[id].name} satıldı.`); if (r.ok) { V.sheet = null; V.sq = null; beep("cash"); } },
  doMove: v => { const [reg, id, to, mode, q] = v.split(":"); const r = result(L.dispatch(S, { from: reg, to, items: [{ res: id, q: +q }], mode }), `Sevkiyat yola çıktı (${RG[to].short}).`); if (r.ok) V.sheet = null; },
  setAuto: v => { const [reg, id] = v.split(":"); W.setAutoStock(S, reg, id, +val("amin") || 0, +val("amax") || 0); toast("Otomatik stok kaydedildi.", "good"); V.sheet = null; },
  // lojistik
  buyV: v => { const [t, home] = v.split(":"); result(L.buyVehicle(S, t, home), "Araç filoya katıldı."); },
  sellV: v => result(L.sellVehicle(S, +v), "Araç satıldı."),
  addRoute: () => { const rf = V.rf || {}; const regs = Object.keys(S.regions).filter(r => S.regions[r].owned); const r = { from: rf.from || regs[0], to: rf.to || regs[1], res: rf.res || "kereste", q: +(rf.q || 20), every: +(rf.every || 1), keep: +(rf.keep || 0), mode: rf.mode || "nakliyeci" };
    if (r.from === r.to) return toast("Başlangıç ve varış aynı olamaz.", "bad"); result(L.addRoute(S, r), "Rota eklendi."); },
  delRoute: v => L.removeRoute(S, +v),
  // şirket
  accept: v => result(K.accept(S, +v), "Sözleşme kabul edildi. Teslim tarihine dikkat."),
  decline: v => K.decline(S, +v),
  bid: v => result(K.bid(S, +v, +val("bid." + v)), "İhale teklifin verildi."),
  deliver: v => { const reg = val("dr" + v) || V["dr" + v] || "anadolu"; result(K.deliver(S, +v, reg), "Teslimat yola çıktı."); },
  autoC: v => { const c = S.contracts.find(x => x.id === +v); c.auto = !c.auto; },
  loanAmt: v => { const [id, a] = v.split(":"); V["la" + id] = +a; },
  loan: v => { const [id, a, t] = v.split(":"); result(Fi.takeLoan(S, id, +a, +(val("lt" + id) || t)), "Kredi hesabına geçti."); },
  repay: v => result(Fi.repayLoan(S, +v), "Kredi kapatıldı."),
  rescue: v => result(Fi.takeRescue(S, v)),
  hire: v => { const [p, n] = v.split(":"); result(E.hire(S, p, +n), `${n} kişi işe alındı.`); },
  fire: v => result(E.fire(S, v, 1)),
  train: v => result(E.train(S, v), "Eğitim başladı."),
  research: v => result(Rs.startResearch(S, v), "Araştırma başladı."),
  claim: v => { const r = claim(S, v); if (r.ok) { toast(`Ödül: ${tl(r.reward)}`, "good"); beep("cash"); } },
  setting: v => { S.settings[v] = !S.settings[v]; },
  saveNow: async () => { await persist(); toast("Kaydedildi.", "good"); },
  confirm: v => { const [what, arg] = v.split(":"); const text = { demolish: "Fabrika satılsın mı? Değerinin %60'ı geri gelir.", sellW: "Depo satılsın mı? İçindeki mallar başka depoya sığmazsa zarar görür.", newGame: "Yeni oyuna başlanacak; mevcut şirket silinecek. Emin misin?" }[what];
    modal(`<h3>Emin misin?</h3><p>${text}</p><div class="row end"><button class="b ghost" data-a="modalClose">Vazgeç</button><button class="b danger" data-a="do_${what}" data-v="${arg}">Evet</button></div>`); },
  do_demolish: v => { result(F.demolish(S, v), "Fabrika satıldı."); V.sheet = null; closeModal(); },
  do_sellW: v => { result(F.sellWarehouse(S, v, WT), "Depo satıldı."); V.sheet = null; closeModal(); },
  do_newGame: () => { closeModal(); showMenu("new"); },
  modalClose: () => closeModal(),
  // ana menü
  mNew: () => showMenu("new"),
  mStart: async () => { const name = (document.querySelector("#coName")?.value || "").trim() || "Patron Sanayi A.Ş."; S = newGame(name); S.time.lastReal = Date.now(); S.tut = 0; await persist(); hideMenu(); V.tab = "map"; V.region = "anadolu"; map.region = "anadolu"; map.mode = "region"; setTimeout(() => map.fit(), 0); dirty = true; },
  mContinue: () => { hideMenu(); dirty = true; setTimeout(() => map.fit(), 0); },
  mHelp: () => showMenu("help"), mBack: () => showMenu(),
};
function renderNotes() {
  const p = $("#notes"); if (!V.notes) { p.hidden = true; return; }
  p.innerHTML = `<div class="nhead"><b>Bildirimler</b><button class="b sm ghost" data-a="notes">Kapat</button></div>${S.notes.slice(0, 40).map(n => `<div class="note ${n.kind}"><span>${esc(n.text)}</span><small>Gün ${Math.floor(n.t / 1440) + 1} · ${dateTxt(n.t).clock}</small></div>`).join("") || `<div class="muted">Bildirim yok.</div>`}`;
  p.hidden = false; for (const n of S.notes) n.read = true;
}
function modal(html) { $("#modal").innerHTML = `<div class="mbox">${html}</div>`; $("#modal").hidden = false; }
function closeModal() { $("#modal").hidden = true; $("#modal").innerHTML = ""; }

// ---------- ana menü ----------
function showMenu(view = "main") {
  menuOpen = true; const m = $("#menu"); m.hidden = false;
  const has = !!S && !S.company.bankrupt;
  if (view === "new") m.innerHTML = `<div class="minner sub"><h2>Yeni şirket</h2><label>Şirket adı <input id="coName" maxlength="28" placeholder="Patron Sanayi A.Ş." value=""></label>
    <p class="muted">Anadolu Sanayi Bölgesi'nde 3 arsa, küçük bir genel depo ve ${tl(BAL.startCash)} sermayeyle başlarsın. Hammadde al, sat, ilk fabrikanı kur, üretim zincirini büyüt; yeni bölgeler, araştırmalar ve rakiplerle sanayi imparatorluğu kur.</p>
    ${has ? `<p class="bad small">Mevcut şirketin silinecek.</p>` : ""}<div class="row end"><button class="b ghost" data-a="mBack">Geri</button><button class="b gold big" data-a="mStart">Şirketi kur</button></div></div>`;
  else if (view === "help") m.innerHTML = `<div class="minner sub help"><h2>Nasıl oynanır</h2><div class="cols2">
    <p><b>Piyasa.</b> Fiyatlar arz ve talebe göre değişir. Aldığında fiyat yükselir, sattığında düşer. Komisyon, vergi ve nakliye vardır; ortalama maliyetin ve kâr/zararın her an görünür. Limit emri ve toplu sipariş verebilirsin.</p>
    <p><b>Depolar.</b> Her ürün kendi sınıfında saklanır: petrol tank, kimyasal özel depo, gıda soğuk hava ister. Depo dolarsa mal kabul edilmez, üretim durur.</p>
    <p><b>Fabrikalar.</b> 15 fabrika ve maden, kuyu, santral, tarım tesisleri. Her reçetenin girdisi, süresi, enerjisi ve firesi vardır. Girdi yoksa ya da depo doluysa üretim durur. Makine, seviye, otomasyon, kalite ve fire hatlarıyla geliştir.</p>
    <p><b>Enerji.</b> Fabrikalar elektriği şebekeden alır. Kendi santralin varsa önce oradan beslenir, fazlası satılır. Kesintide yalnızca kendi enerjisi olanlar çalışır.</p>
    <p><b>Sözleşmeler.</b> Müşteriler miktar, fiyat ve kalite ister. Zamanında teslim itibarı artırır; gecikme ve kaçırma ceza getirir. Devlet ihalelerinde en düşük teklif kazanır.</p>
    <p><b>Lojistik.</b> Bölgeler arası mal taşı, kendi filonu kur (kamyon, tır, tren, gemi, uçak), otomatik rotalar tanımla.</p>
    <p><b>Şirket.</b> Personel işe al ve eğit, araştırma yap, kredi çek, rakipleri izle, yeni bölgeler aç. Ay sonunda kârdan vergi kesilir.</p>
    <p><b>Zaman.</b> ×1 hızda 1 oyun günü 12 dakikadır. Oyundan çıkınca üretim devam eder (en fazla 3 oyun günü); dönüşte rapor görürsün.</p></div><div class="row end"><button class="b gold" data-a="mBack">Tamam</button></div></div>`;
  else m.innerHTML = `<div class="minner"><div class="mleft"><h1 class="logo">FABRİKA<span>PATRONU</span></h1><p class="sub">Hammadde İmparatorluğu</p><canvas id="menuArt" aria-hidden="true"></canvas></div>
    <div class="mright">${has ? `<button class="mbtn gold" data-a="mContinue"><b>Devam et</b><small>${esc(S.company.name)} · Gün ${day(S) + 1} · ${BAL.levelNames[level(S) - 1]} · ${tl(Fi.companyValue(S))}</small></button>` : ""}
      <button class="mbtn ${has ? "" : "gold"}" data-a="mNew"><b>Yeni oyun</b><small>Şirketini kur ve sıfırdan başla</small></button>
      <button class="mbtn" data-a="mHelp"><b>Nasıl oynanır</b><small>Piyasa, fabrikalar, lojistik, sözleşmeler</small></button>
      <p class="muted small">Kayıt bu cihazda tutulur. Reklam yok.</p></div></div>`;
  paintMenuArt();
}
function hideMenu() { menuOpen = false; $("#menu").hidden = true; }
function paintMenuArt() {
  const cv = document.querySelector("#menuArt"); if (!cv) return; const r = cv.getBoundingClientRect(), d = 2; cv.width = r.width * d; cv.height = r.height * d;
  const g = cv.getContext("2d"), t = performance.now() / 1000; g.setTransform(d, 0, 0, d, 0, 0); const gr = g.createLinearGradient(0, 0, 0, r.height); gr.addColorStop(0, "#2a4c66"); gr.addColorStop(1, "#14283a"); g.fillStyle = gr; g.fillRect(0, 0, r.width, r.height);
  const sc = Math.min(r.width / 520, r.height / 300); g.save(); g.translate(r.width / 2, r.height * 0.28); g.scale(sc, sc);
  const looks = [["steel", 0, 0], ["refinery", 1.2, -0.2], ["power", -1.2, 0.2], ["chem", 0.6, 1.1], ["logistics", -0.6, 1.2]];
  g.fillStyle = "#5d8a4e"; g.beginPath(); [iso(-1.6, -0.6), iso(2.4, -0.6), iso(2.4, 2.4), iso(-1.6, 2.4)].forEach((p, i) => (i ? g.lineTo(...p) : g.moveTo(...p))); g.fill();
  for (const [lk, x, y] of looks.sort((a, b) => a[1] + a[2] - b[1] - b[2])) BUILD[lk]({ g, x, y, t, run: true, night: false, lvl: 2, m: 2, snow: false, seed: 5 });
  g.restore();
  if (menuOpen) requestAnimationFrame(paintMenuArt);
}
function offlineReport(o) {
  if (!o) return; if (o.skipped) return modal(`<h3>Çevrimdışı ilerleme verilmedi</h3><p>${esc(o.reason)}</p><div class="row end"><button class="b gold" data-a="modalClose">Tamam</button></div>`);
  const b = o.before, a = o.after, made = Object.entries(a.produced).map(([id, q]) => [id, q - (b.produced[id] || 0)]).filter(([, q]) => q > 0.01);
  const cats = {}; for (const k of new Set([...Object.keys(a.cats), ...Object.keys(b.cats)])) { const d = (a.cats[k] || 0) - (b.cats[k] || 0); if (Math.abs(d) > 1) cats[k] = d; }
  modal(`<h3>Sen yokken</h3><p class="muted">${n1(o.realHours)} saat geçti · ${n0(o.gameHours / 24 * 10) / 10} oyun günü işlendi${o.capped ? " (en fazla 3 gün sayılır)" : ""}.</p>
    <div class="kpis small"><div class="kpi"><span>Nakit</span><b class="${a.cash >= b.cash ? "good" : "bad"}">${a.cash >= b.cash ? "+" : ""}${tl(a.cash - b.cash)}</b></div><div class="kpi"><span>Şirket değeri</span><b>${tl(a.value)}</b></div><div class="kpi"><span>Teslimat</span><b>${a.deliveries - b.deliveries}</b></div><div class="kpi"><span>İtibar</span><b>${n0(a.rep)}</b></div></div>
    ${made.length ? `<div class="lbl">ÜRETİLEN</div><div class="chips">${made.map(([id, q]) => `<span class="chip">${iconSvg(id, 16)} ${qtyTxt(id, q)}</span>`).join("")}</div>` : ""}
    <div class="lbl">GELİR VE GİDERLER</div>${Object.entries(cats).sort((x, y) => y[1] - x[1]).map(([k, v]) => `<div class="line"><span>${k}</span><b class="${v >= 0 ? "good" : "bad"}">${tl(v)}</b></div>`).join("") || `<div class="muted small">Hareket yok.</div>`}
    <div class="row end"><button class="b gold" data-a="modalClose">Devam</button></div>`);
}

// ---------- döngü ----------
function loop(now) {
  const dt = Math.min(0.25, (now - (loop.last || now)) / 1000); loop.last = now;
  if (S && !menuOpen && !S.company.bankrupt) {
    const before = S.notes.length; tickReal(S, dt);
    if (S.notes.length > before) for (const n of S.notes.slice(0, Math.min(2, S.notes.length - before))) { if (n.kind === "level") beep("level"); toast(n.text, n.kind === "bad" ? "bad" : n.kind === "good" || n.kind === "level" ? "good" : "info"); }
    const step = SC.tutorialStep(S); if (step && step.done(S)) { S.tut = (S.tut || 0) + 1; earn(S, 250000, "Ödül"); toast(`İlk adım tamamlandı: ${step.t} (+250 bin ₺)`, "good"); dirty = true; }
    const hr = Math.floor(S.time.min / 60); if (hr !== lastHourSeen) { lastHourSeen = hr; if (!document.activeElement || !["INPUT", "SELECT"].includes(document.activeElement.tagName)) dirty = true; }
    if (S.company.bankrupt) modal(`<h3>Şirket iflas etti</h3><p>Eksi bakiye uzun süre düzelmedi ve kurtarma seçenekleri kullanılmadı. Tecrübenle yeniden başlayabilirsin.</p><div class="row end"><button class="b gold" data-a="do_newGame">Yeni şirket kur</button></div>`);
    if (now - lastSave > 20000) persist();
  }
  if (S && (now - (loop.topT || 0) > 400)) { loop.topT = now; renderTop(); }
  if (dirty && S) { dirty = false; renderBottom(); renderView(); }
  if (S && V.tab === "map" && !menuOpen) map.draw(now / 1000);
  if (S && V.tab !== "map" && now - (loop.artT || 0) > 100) { loop.artT = now; for (const cv of document.querySelectorAll("canvas[data-art][data-run='1']")) paintArts(cv.parentElement); }
  requestAnimationFrame(loop);
}
export async function boot() {
  storage = makeStorage();
  map = createMap($("#mapc"), () => S, hit => {
    if (!hit) { V.sheet = null; map.sel = null; dirty = true; return; }
    if (hit.region) { V.region = hit.region; if (S.regions[hit.region].owned) { V.mapMode = "region"; map.mode = "region"; map.region = hit.region; map.fit(); } else V.sheet = `plot:${hit.region}:0`; dirty = true; return; }
    const p = S.regions[map.region].plots[hit.plot]; map.sel = hit; V.sheet = p.b ? "b:" + p.b : p.owned ? `build:${map.region}:${hit.plot}` : `plot:${map.region}:${hit.plot}`; dirty = true; haptic();
  });
  document.addEventListener("click", e => { const b = e.target.closest("[data-a]"); if (!b || b.disabled) return; const f = A[b.dataset.a]; if (!f) return; const r = f(b.dataset.v, e); if (r && r.then) r.then(() => { dirty = true; }); dirty = true; });
  document.addEventListener("change", e => { const f = e.target.dataset && e.target.dataset.f; if (!f) return; const v = e.target.value;
    if (f.startsWith("rf.")) { V.rf = V.rf || {}; V.rf[f.slice(3)] = v; } else if (f === "qty") V.qty = Math.max(0, +v || 0); else if (f === "sq") V.sq = +v; else if (f === "mq") V.mq = +v; else if (f === "limit") V.limit = +v; else if (f === "bdays") V.bdays = +v;
    else if (f === "breg") V.sheet = "build:" + v; else V[f] = v; dirty = true; });
  document.addEventListener("visibilitychange", () => { if (document.hidden) persist(); else if (S) { offlineReport(applyOffline(S, Date.now())); dirty = true; } });
  window.addEventListener("pagehide", () => persist());
  window.addEventListener("resize", () => { if (V.tab === "map") map.fit(); dirty = true; });
  const r = await loadGame(storage);
  if (r.state) { S = r.state; if (r.from === "backup") toast("Ana kayıt bozuktu; yedekten kurtarıldı.", "warn"); const o = applyOffline(S, Date.now()); showMenu(); if (o) setTimeout(() => offlineReport(o), 50); }
  else { showMenu(); if (r.from === "corrupt") toast("Kayıt okunamadı. Yeni oyun başlatabilirsin.", "bad"); }
  requestAnimationFrame(loop);
  window.__fp = { get S() { return S; }, V, A, persist, map };
}
