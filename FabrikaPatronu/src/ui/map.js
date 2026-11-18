// İzometrik bölge haritası ve dünya haritası. Kamera (kaydır/yakınlaştır), dokunma testi, gece-gündüz, mevsim, araç trafiği.
import { RG, REGIONS } from "../data/regions.js";
import { FT, RECIPE } from "../data/factories.js";
import { WT } from "../data/warehouses.js";
import { RES } from "../data/resources.js";
import { HW, HH, iso, fillPoly, box, cyl, truck, train, ship, plane, person, glow, BUILD } from "./art.js";
import { shade } from "./icons.js";
import { monthOf } from "../core/util.js";

export const STEP = 1.35, GAP = 0.35, MARGIN = 1.6;
export const plotOrigin = (c, r) => [GAP + c * STEP, GAP + r * STEP];
export function season(s) { const m = monthOf(Math.floor(s.time.min / 1440)); return m === 11 || m < 2 ? "kis" : m < 5 ? "ilkbahar" : m < 8 ? "yaz" : "sonbahar"; }
export const SEASON_NAME = { kis: "Kış", ilkbahar: "İlkbahar", yaz: "Yaz", sonbahar: "Sonbahar" };
const GRASS = { ilkbahar: "#5d8a4e", yaz: "#7f9a4a", sonbahar: "#9a7a44", kis: "#dfe7ee" };
const daylight = s => { const h = (s.time.min % 1440) / 60; return h < 5 || h > 21 ? 0 : h < 7 ? (h - 5) / 2 : h > 19 ? (21 - h) / 2 : 1; };

export function createMap(canvas, getState, onTap) {
  const M = { canvas, cam: { x: 0, y: 0, z: 0.8 }, fitted: null, region: "anadolu", mode: "region", sel: null, hover: null, drag: null, pinch: null, t: 0 };
  const g = canvas.getContext("2d");
  function size() { const r = canvas.getBoundingClientRect(), d = Math.min(2, window.devicePixelRatio || 1); const w = Math.round(r.width * d), h = Math.round(r.height * d); if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; } return [r.width, r.height, d]; }
  function bounds(regId) { const R = RG[regId], W = R.cols * STEP + GAP, D = R.rows * STEP + GAP; return { W, D }; }
  M.fit = function () {
    const [w, h] = size(); if (M.mode === "world") { M.cam = { x: 0, y: 0, z: Math.min(w / 1700, h / 900) }; return; }
    const { W, D } = bounds(M.region), R = RG[M.region], st = getState(); let x0 = 0, y0 = 0, x1 = W, y1 = D;
    if (st && st.regions[M.region].owned) { const own = st.regions[M.region].plots.map((p, i) => [p, i]).filter(([p]) => p.owned || p.b);
      if (own.length && own.length < R.cols * R.rows) { const cs = own.map(([, i]) => i % R.cols), rs = own.map(([, i]) => Math.floor(i / R.cols));
        x0 = Math.max(0, Math.min(...cs) * STEP - 0.6); y0 = Math.max(0, Math.min(...rs) * STEP - 0.6); x1 = Math.min(W, (Math.max(...cs) + 1) * STEP + GAP + 0.8); y1 = Math.min(D, (Math.max(...rs) + 1) * STEP + GAP + 0.8); } }
    const pts = [iso(x0 - 0.2, y0 - 0.2), iso(x1 + 0.2, y0 - 0.2), iso(x1 + 0.2, y1 + 0.2), iso(x0 - 0.2, y1 + 0.2)];
    const minX = Math.min(...pts.map(p => p[0])), maxX = Math.max(...pts.map(p => p[0])), minY = Math.min(...pts.map(p => p[1])) - 70, maxY = Math.max(...pts.map(p => p[1]));
    const z = Math.min(w * 0.72 / (maxX - minX), h / (maxY - minY)) * 0.96; M.cam = { x: -(minX + maxX) / 2 + (w * 0.12) / z, y: -(minY + maxY) / 2, z }; M.fitted = { z, x: M.cam.x, y: M.cam.y };
  };
  const toWorld = (sx, sy) => { const [w, h] = size(); return [(sx - w / 2) / M.cam.z - M.cam.x, (sy - h / 2) / M.cam.z - M.cam.y]; };
  const toGrid = (X, Y) => [(X / HW + Y / HH) / 2, (Y / HH - X / HW) / 2];
  M.pick = function (sx, sy) {
    const [X, Y] = toWorld(sx, sy);
    if (M.mode === "world") { for (const r of REGIONS) { const [x, y] = worldPos(r); if (Math.hypot(X - x, (Y - y) * 1.6) < 120) return { region: r.id }; } return null; }
    const [gx, gy] = toGrid(X, Y), R = RG[M.region];
    for (let r = 0; r < R.rows; r++) for (let c = 0; c < R.cols; c++) { const [ox, oy] = plotOrigin(c, r); if (gx >= ox && gx <= ox + 1 && gy >= oy && gy <= oy + 1) return { plot: r * R.cols + c }; }
    // binanın yüksek kısmına dokunulduysa: yukarıdaki arsayı da dene
    for (let dz = 20; dz <= 80; dz += 20) { const [gx2, gy2] = toGrid(X, Y + dz); for (let r = 0; r < R.rows; r++) for (let c = 0; c < R.cols; c++) { const [ox, oy] = plotOrigin(c, r); if (gx2 >= ox && gx2 <= ox + 1 && gy2 >= oy && gy2 <= oy + 1) { const p = getState().regions[M.region].plots[r * R.cols + c]; if (p.b) return { plot: r * R.cols + c }; } } }
    return null;
  };
  const worldPos = r => [r.x * 1.25, r.y * 0.95];
  // --- girişler ---
  const pts = new Map();
  canvas.addEventListener("pointerdown", e => { canvas.setPointerCapture(e.pointerId); pts.set(e.pointerId, [e.offsetX, e.offsetY]); if (pts.size === 1) M.drag = { x: e.offsetX, y: e.offsetY, cx: M.cam.x, cy: M.cam.y, moved: 0 }; else if (pts.size === 2) { const [a, b] = [...pts.values()]; M.pinch = { d: Math.hypot(a[0] - b[0], a[1] - b[1]), z: M.cam.z }; } });
  canvas.addEventListener("pointermove", e => {
    if (!pts.has(e.pointerId)) return; pts.set(e.pointerId, [e.offsetX, e.offsetY]);
    if (pts.size === 2 && M.pinch) { const [a, b] = [...pts.values()]; M.cam.z = clampZ(M.pinch.z * Math.hypot(a[0] - b[0], a[1] - b[1]) / M.pinch.d); if (M.drag) M.drag.moved = 99; return; }
    if (M.drag) { const dx = e.offsetX - M.drag.x, dy = e.offsetY - M.drag.y; M.drag.moved = Math.max(M.drag.moved, Math.hypot(dx, dy)); M.cam.x = M.drag.cx + dx / M.cam.z; M.cam.y = M.drag.cy + dy / M.cam.z; }
  });
  const up = e => { const d = M.drag; pts.delete(e.pointerId); if (pts.size < 2) M.pinch = null; if (!pts.size) { M.drag = null; if (d && d.moved < 8) { const hit = M.pick(e.offsetX, e.offsetY); onTap(hit); } } };
  canvas.addEventListener("pointerup", up); canvas.addEventListener("pointercancel", e => { pts.delete(e.pointerId); M.drag = null; M.pinch = null; });
  canvas.addEventListener("wheel", e => { e.preventDefault(); M.cam.z = clampZ(M.cam.z * (e.deltaY < 0 ? 1.1 : 0.9)); }, { passive: false });
  const clampZ = z => Math.max(M.mode === "world" ? 0.2 : 0.25, Math.min(2.6, z));
  M.zoom = f => { M.cam.z = clampZ(M.cam.z * f); };

  // --- çizim ---
  M.draw = function (t) {
    M.t = t; const s = getState(); if (!s) return;
    const [w, h, d] = size(); g.setTransform(d, 0, 0, d, 0, 0); g.clearRect(0, 0, w, h);
    const dl = daylight(s), sky = g.createLinearGradient(0, 0, 0, h);
    sky.addColorStop(0, dl > 0.5 ? "#2a5470" : dl > 0 ? "#3a3550" : "#0b1424"); sky.addColorStop(1, dl > 0.5 ? "#16324a" : "#0a1220"); g.fillStyle = sky; g.fillRect(0, 0, w, h);
    g.save(); g.translate(w / 2, h / 2); g.scale(M.cam.z, M.cam.z); g.translate(M.cam.x, M.cam.y);
    if (M.mode === "world") drawWorld(s, t); else drawRegion(s, t, dl);
    g.restore();
    // gece ve hava
    if (dl < 1) { g.fillStyle = `rgba(8,16,40,${(1 - dl) * 0.42})`; g.fillRect(0, 0, w, h); }
    const se = season(s);
    if (se === "kis") { g.fillStyle = "rgba(255,255,255,.8)"; for (let i = 0; i < 70; i++) { const x = (i * 97.3 + t * 18 * (1 + i % 3)) % w, y = (i * 53.7 + t * 30 * (1 + i % 2)) % h; g.beginPath(); g.arc(x, y, 1 + (i % 3) * 0.6, 0, 7); g.fill(); } }
    if (se === "sonbahar" && Math.floor(s.time.min / 1440) % 3 === 0) { g.strokeStyle = "rgba(180,200,230,.35)"; g.lineWidth = 1; for (let i = 0; i < 80; i++) { const x = (i * 71.1 + t * 60) % w, y = (i * 37.3 + t * 260) % h; g.beginPath(); g.moveTo(x, y); g.lineTo(x - 3, y + 10); g.stroke(); } }
  };
  function drawRegion(s, t, dl) {
    const R = RG[M.region], reg = s.regions[M.region], { W, D } = bounds(M.region), se = season(s);
    const grass = M.region === "petrol" ? (se === "kis" ? "#d9d3c4" : "#c9a86a") : M.region === "maden" ? (se === "kis" ? "#d6dde3" : "#8a7d62") : GRASS[se];
    // zemin
    const m = MARGIN; fillPoly(g, [iso(-m, -m), iso(W + m, -m), iso(W + m, D + m), iso(-m, D + m)], grass);
    // su (liman ve ticaret merkezi)
    if (R.port) { fillPoly(g, [iso(W + 0.2, -m), iso(W + m + 2, -m), iso(W + m + 2, D + m), iso(W + 0.2, D + m)], "#2a6f96"); for (let i = 0; i < 18; i++) { const [x, y] = iso(W + 0.6 + (i % 3) * 0.7, -m + i * 0.45); g.strokeStyle = "rgba(190,225,255,.3)"; g.beginPath(); g.moveTo(x - 12 + Math.sin(t + i) * 3, y); g.lineTo(x + 12 + Math.sin(t + i) * 3, y); g.stroke(); }
      box(g, W + 0.05, -0.2, 0.18, D + 0.4, 6, "#7a6a5a"); ship(g, W + 0.55, D * 0.25 + Math.sin(t * 0.05) * 0.3, t); if (D > 2.5) ship(g, W + 0.5, D * 0.7, t + 2);
      for (let i = 0; i < 2; i++) { const [cx, cy] = iso(W + 0.12, 0.8 + i * 1.6, 0); g.strokeStyle = "#e0a83a"; g.lineWidth = 3; g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx, cy - 70); g.lineTo(cx + 60, cy - 50); g.stroke(); } }
    // dağlar / gökdelenler / tarlalar
    if (M.region === "maden") for (let i = 0; i < 6; i++) { const [x, y] = iso(-m + 0.2, -m + 0.6 + i * (D + 2) / 6); g.fillStyle = shade("#6d5a48", (i % 2) * 0.15); g.beginPath(); g.moveTo(x - 90, y + 20); g.lineTo(x, y - 110 - (i % 3) * 30); g.lineTo(x + 90, y + 20); g.fill(); if (se === "kis" || i % 2) { g.fillStyle = "#eef3f6"; g.beginPath(); g.moveTo(x - 22, y - 80 - (i % 3) * 22); g.lineTo(x, y - 110 - (i % 3) * 30); g.lineTo(x + 22, y - 80 - (i % 3) * 22); g.fill(); } }
    if (M.region === "teknoloji" || M.region === "uluslararasi") for (let i = 0; i < 7; i++) { const gx = -m + 0.3, gy = -m + 0.5 + i * (D + 2) / 7, hh = 80 + (i * 37) % 90; box(g, gx, gy, 0.45, 0.4, hh, M.region === "teknoloji" ? "#4a6f8f" : "#5a6777", { top: "#8fa3b5" }); for (let k = 0; k < hh / 14; k++) { const lit = dl < 0.6 && (i + k) % 3; fillPoly(g, [iso(gx + 0.45, gy + 0.05, k * 14 + 4), iso(gx + 0.45, gy + 0.35, k * 14 + 4), iso(gx + 0.45, gy + 0.35, k * 14 + 9), iso(gx + 0.45, gy + 0.05, k * 14 + 9)], lit ? "#ffd98a" : "#9fd0f0"); } }
    if (R.airport) { fillPoly(g, [iso(-m + 0.1, D + 0.4), iso(W + 0.1, D + 0.4), iso(W + 0.1, D + 0.95), iso(-m + 0.1, D + 0.95)], "#3b444c"); const ph = (t * 0.05) % 1, [px, py] = iso(-m + ph * (W + m + 2), D + 0.68); plane(g, px, py - ph * ph * 120, 1.2); }
    if (R.rail) { const y0 = -0.75; fillPoly(g, [iso(-m, y0), iso(W + m, y0), iso(W + m, y0 + 0.16), iso(-m, y0 + 0.16)], "#6a5a4a"); g.strokeStyle = "#9aa5ae"; g.lineWidth = 1.2; for (const yy of [y0 + 0.04, y0 + 0.12]) { g.beginPath(); g.moveTo(...iso(-m, yy)); g.lineTo(...iso(W + m, yy)); g.stroke(); } const tx = -m + ((t * 0.12) % 1) * (W + 2 * m + 2); train(g, tx, y0 + 0.03, 6, t); }
    if (M.region === "anadolu" || M.region === "liman") for (let i = 0; i < 8; i++) { const gx = -m + 0.4 + (i * 0.37) % 1.1, gy = -0.3 + i * (D + 1) / 8; tree(gx, gy, se); }
    for (let i = 0; i < 6; i++) tree(W + 0.2 + (R.port ? -0.15 : 0.3 + (i % 2) * 0.5), -0.8 + i * (D + 1.5) / 6, se, R.port);
    // yollar
    const road = "#3d454d"; for (let r = 0; r <= R.rows; r++) { const y = r * STEP; fillPoly(g, [iso(0, y), iso(W, y), iso(W, y + GAP), iso(0, y + GAP)], road); }
    for (let c = 0; c <= R.cols; c++) { const x = c * STEP; fillPoly(g, [iso(x, 0), iso(x + GAP, 0), iso(x + GAP, D), iso(x, D)], road); }
    g.strokeStyle = "rgba(240,220,140,.55)"; g.lineWidth = 1.4; g.setLineDash([8, 10]);
    for (let r = 0; r <= R.rows; r++) { const y = r * STEP + GAP / 2; g.beginPath(); g.moveTo(...iso(0, y)); g.lineTo(...iso(W, y)); g.stroke(); }
    for (let c = 0; c <= R.cols; c++) { const x = c * STEP + GAP / 2; g.beginPath(); g.moveTo(...iso(x, 0)); g.lineTo(...iso(x, D)); g.stroke(); } g.setLineDash([]);
    // arsalar ve binalar (arkadan öne)
    const order = []; for (let r = 0; r < R.rows; r++) for (let c = 0; c < R.cols; c++) order.push([c, r]); order.sort((a, b) => a[0] + a[1] - b[0] - b[1]);
    const trucks = Math.min(14, 3 + s.shipments.filter(x => x.to === M.region || x.from === M.region).length + Object.values(s.buildings).filter(b => b.region === M.region && b.status === "Çalışıyor").length);
    for (const [c, r] of order) {
      const i = r * R.cols + c, p = reg.plots[i], [ox, oy] = plotOrigin(c, r), b = p.b && s.buildings[p.b];
      if (!reg.owned) { fillPoly(g, [iso(ox, oy), iso(ox + 1, oy), iso(ox + 1, oy + 1), iso(ox, oy + 1)], shade(grass, -0.1)); continue; }
      if (p.owned) fillPoly(g, [iso(ox, oy), iso(ox + 1, oy), iso(ox + 1, oy + 1), iso(ox, oy + 1)], se === "kis" ? "#cfd8df" : "#a9b0b5", "rgba(0,0,0,.2)");
      else { fillPoly(g, [iso(ox + 0.04, oy + 0.04), iso(ox + 0.96, oy + 0.04), iso(ox + 0.96, oy + 0.96), iso(ox + 0.04, oy + 0.96)], shade(grass, -0.06)); sign(ox + 0.5, oy + 0.5, "SATILIK"); }
      if (b) drawBuilding(s, b, ox, oy, t, dl, se);
      else if (p.owned) { g.setLineDash([6, 6]); g.strokeStyle = "rgba(255,255,255,.55)"; g.lineWidth = 1.5; P4(ox + 0.12, oy + 0.12, 0.76); g.stroke(); g.setLineDash([]); const [x, y] = iso(ox + 0.5, oy + 0.5); g.fillStyle = "rgba(247,197,72,.9)"; g.beginPath(); g.arc(x, y, 13, 0, 7); g.fill(); g.fillStyle = "#2b1d00"; g.font = "900 18px Nunito, sans-serif"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText("+", x, y + 1); }
      if (M.sel && M.sel.plot === i) { const a = 0.6 + Math.sin(t * 5) * 0.3; g.strokeStyle = `rgba(247,197,72,${a})`; g.lineWidth = 3; P4(ox - 0.02, oy - 0.02, 1.04); g.stroke(); }
      // yol trafiği (bu arsanın önündeki yolda)
      if (i < trucks) { const k = (t * 0.06 + i * 0.37) % 1, horiz = i % 2 === 0, dir = i % 4 < 2 ? 1 : -1;
        if (horiz) { const y = (r + 1) * STEP + GAP / 2 + (dir > 0 ? -0.06 : 0.06), x = dir > 0 ? k * W : (1 - k) * W; truck(g, x, y, dir, ["#3f7fa8", "#c94a3a", "#4ca86a", "#e0a83a"][i % 4], ["#c9d3dc", "#8b5a3c", "#9fb3c6", "#a36f3d"][i % 4], i % 3 === 0); }
        else { const x = (c + 1) * STEP + GAP / 2 + (dir > 0 ? 0.06 : -0.06), y = dir > 0 ? k * D : (1 - k) * D; truck(g, x, y, 2 * dir, ["#e0a83a", "#3f7fa8", "#c94a3a"][i % 3], "#c9d3dc", i % 3 === 1); } }
    }
    // sokak lambaları geceleri
    if (dl < 0.5) for (let r = 0; r <= R.rows; r++) for (let c = 0; c <= R.cols; c++) { const [x, y] = iso(c * STEP + 0.05, r * STEP + 0.05); g.strokeStyle = "#5a6168"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(x, y); g.lineTo(x, y - 26); g.stroke(); glow(g, x, y - 26, 26, "rgba(255,215,140,.35)"); }
    if (!reg.owned) { const [x, y] = iso(W / 2, D / 2); g.fillStyle = "rgba(10,18,26,.55)"; g.fillRect(x - 900, y - 600, 1800, 1200); }
  }
  function P4(x, y, sz) { g.beginPath(); [iso(x, y), iso(x + sz, y), iso(x + sz, y + sz), iso(x, y + sz)].forEach((p, i) => (i ? g.lineTo(...p) : g.moveTo(...p))); g.closePath(); }
  function tree(gx, gy, se, palm) { const [x, y] = iso(gx, gy); g.fillStyle = "rgba(0,0,0,.2)"; g.beginPath(); g.ellipse(x, y, 10, 4, 0, 0, 7); g.fill(); g.fillStyle = "#5a4030"; g.fillRect(x - 1.5, y - 10, 3, 10);
    const col = se === "kis" ? "#e9eef2" : se === "sonbahar" ? "#c9782f" : se === "yaz" ? "#4f7a3a" : "#5f9a48"; g.fillStyle = col; g.beginPath(); g.moveTo(x - 10, y - 8); g.lineTo(x, y - 34); g.lineTo(x + 10, y - 8); g.fill(); g.fillStyle = shade(col, -0.2); g.beginPath(); g.moveTo(x, y - 34); g.lineTo(x + 10, y - 8); g.lineTo(x + 2, y - 8); g.fill(); }
  function sign(gx, gy, text) { const [x, y] = iso(gx, gy); g.strokeStyle = "#5a4030"; g.lineWidth = 2; g.beginPath(); g.moveTo(x, y); g.lineTo(x, y - 22); g.stroke(); g.fillStyle = "#f7c548"; g.fillRect(x - 28, y - 38, 56, 17); g.fillStyle = "#2b1d00"; g.font = "900 10px Nunito, sans-serif"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(text, x, y - 29); }
  function drawBuilding(s, b, ox, oy, t, dl, se) {
    const isW = b.kind === "warehouse", look = isW ? WT[b.type].look : FT[b.type].look, rec = !isW && RECIPE[b.recipe];
    const run = b.status === "Çalışıyor", seed = parseInt(String(b.id).replace(/\D/g, "")) || 1;
    const c = { g, x: ox, y: oy, t, run, night: dl < 0.6, lvl: b.level || 1, m: b.machines || 1, snow: se === "kis", seed };
    if (b.type === "maden" && rec) c.oreColor = RES[rec.res]?.color; if (b.type === "ciftlik" && rec) c.crop = RES[rec.res]?.color; if (rec && rec.id === "e_gunes") c.solar = true;
    (BUILD[look] || BUILD.shed)(c);
    if (run) for (let k = 0; k < Math.min(4, c.m); k++) { const ph = (t * 0.07 + k * 0.29 + seed * 0.1) % 1; person(g, ox + 0.1 + ph * 0.8, oy + 0.94, t, k + seed, k % 2 ? "#f2b631" : "#e9eef2"); }
    if (!isW) badge(s, b, ...iso(ox + 0.5, oy + 0.5, 92 + 6 * (c.lvl - 1)), t);
    const [lx, ly] = iso(ox + 1, oy + 1); g.font = "800 11px Nunito, sans-serif"; g.textAlign = "center"; const label = isW ? WT[b.type].name : FT[b.type].name.replace(" fabrikası", "");
    const tw = g.measureText(label).width + 12; g.fillStyle = "rgba(10,18,26,.78)"; rr(lx - tw / 2, ly - 8, tw, 16, 8); g.fill(); g.fillStyle = "#e9eef2"; g.textBaseline = "middle"; g.fillText(label + (b.level > 1 ? " " + b.level : ""), lx, ly);
  }
  function rr(x, y, w, h, r) { g.beginPath(); g.moveTo(x + r, y); g.arcTo(x + w, y, x + w, y + h, r); g.arcTo(x + w, y + h, x, y + h, r); g.arcTo(x, y + h, x, y, r); g.arcTo(x, y, x + w, y, r); g.closePath(); }
  const BADGE = { "Çalışıyor": ["#3ddc97", "▶"], "Hammadde bekliyor": ["#ffb02e", "!"], "Bakım gerekiyor": ["#ff6b5e", "🔧"], "Depo dolu": ["#b28cff", "▣"], "Enerji kesintisi": ["#ffd23c", "⚡"], "Üretim tamamlandı": ["#3ddc97", "✓"], "Durduruldu": ["#8ea4b6", "❚❚"], "İşçi yok": ["#7ec3ff", "👷"], "Hazır": ["#8ea4b6", "•"] };
  function badge(s, b, x, y, t) {
    const [col, sym] = BADGE[b.status] || BADGE["Hazır"], bob = Math.sin(t * 3 + x) * (b.status === "Çalışıyor" ? 0 : 2);
    g.fillStyle = "rgba(10,18,26,.85)"; g.beginPath(); g.arc(x, y + bob, 12, 0, 7); g.fill(); g.strokeStyle = col; g.lineWidth = 2.5; g.beginPath();
    if (b.cycle && b.status === "Çalışıyor") g.arc(x, y + bob, 12, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * (1 - b.cycle.left / b.cycle.total)); else g.arc(x, y + bob, 12, 0, 7); g.stroke();
    g.fillStyle = col; g.font = "900 11px Nunito, sans-serif"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(sym, x, y + bob + 1);
  }
  function drawWorld(s, t) {
    // stilize kara parçası, bölgeler ve bağlantılar
    g.fillStyle = "#1d4a66"; g.fillRect(-2000, -1200, 4000, 2400);
    g.fillStyle = "#4f6f4a"; g.beginPath(); g.ellipse(80, 0, 900, 420, -0.08, 0, 7); g.fill(); g.fillStyle = "#5d7f54"; g.beginPath(); g.ellipse(40, -30, 760, 330, -0.1, 0, 7); g.fill();
    g.fillStyle = "#3f5f3c"; g.beginPath(); g.ellipse(-880, -80, 180, 120, 0, 0, 7); g.fill();
    const owned = REGIONS.filter(r => s.regions[r.id].owned);
    g.strokeStyle = "rgba(247,197,72,.55)"; g.lineWidth = 3; g.setLineDash([10, 10]); g.lineDashOffset = -t * 20;
    for (const a of owned) for (const b of owned) if (a.id < b.id) { const [x1, y1] = worldPos(a), [x2, y2] = worldPos(b); g.beginPath(); g.moveTo(x1, y1); g.quadraticCurveTo((x1 + x2) / 2, (y1 + y2) / 2 - 60, x2, y2); g.stroke(); }
    g.setLineDash([]);
    for (const r of REGIONS) {
      const [x, y] = worldPos(r), own = s.regions[r.id].owned, sel = M.region === r.id;
      g.fillStyle = "rgba(0,0,0,.3)"; g.beginPath(); g.ellipse(x, y + 18, 110, 40, 0, 0, 7); g.fill();
      g.fillStyle = own ? r.color : "#4a5560"; g.beginPath(); g.moveTo(x, y - 46); g.lineTo(x + 100, y); g.lineTo(x, y + 46); g.lineTo(x - 100, y); g.closePath(); g.fill();
      g.strokeStyle = sel ? "#f7c548" : "rgba(255,255,255,.4)"; g.lineWidth = sel ? 5 : 2; g.stroke();
      const nb = Object.values(s.buildings).filter(b => b.region === r.id);
      for (let i = 0; i < Math.min(5, nb.length); i++) box(g, (x + (i - 2) * 30) / HW / 2 + (y) / HH / 2, (y) / HH / 2 - (x + (i - 2) * 30) / HW / 2, 0.16, 0.16, 14 + (i % 2) * 8, "#dfe6ec");
      const fz = 1 / M.cam.z; g.font = `900 ${Math.round(16 * fz)}px Nunito, sans-serif`; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = "#fff"; g.fillText(r.short, x, y - 46 - 14 * fz);
      g.font = `800 ${Math.round(12 * fz)}px Nunito, sans-serif`; g.fillStyle = own ? "#3ddc97" : "#f7c548"; g.fillText(own ? `${nb.length} tesis` : `🔒 Seviye ${r.level} · ${Math.round(r.cost / 1e6)} Mn ₺`, x, y + 46 + 12 * fz);
    }
  }
  return M;
}
