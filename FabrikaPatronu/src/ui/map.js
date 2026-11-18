// İzometrik bölge haritası ve dünya haritası. Kamera (kaydır/yakınlaştır), dokunma testi, gece-gündüz, mevsim, araç trafiği.
import { RG, REGIONS } from "../data/regions.js";
import { FT, RECIPE } from "../data/factories.js";
import { WT } from "../data/warehouses.js";
import { RES } from "../data/resources.js";
import { HW, HH, iso, fillPoly, box, cyl, truck, car, tree as treeArt, lamp, train, ship, plane, person, glow, BUILD, shadowPass, recordLights } from "./art.js";
import { shade } from "./icons.js";
import { monthOf } from "../core/util.js";

export const STEP = 1.35, GAP = 0.35, MARGIN = 1.6, OUTER = 5;
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
    const [w, h] = size(); if (!w || !h) { M.needFit = true; return; } M.needFit = false; if (M.mode === "world") { M.cam = { x: 0, y: 0, z: Math.min(w / 1700, h / 900) }; clampCam(); return; }
    const { W, D } = bounds(M.region), R = RG[M.region], st = getState(); let x0 = 0, y0 = 0, x1 = W, y1 = D;
    if (st && st.regions[M.region].owned) { const own = st.regions[M.region].plots.map((p, i) => [p, i]).filter(([p]) => p.owned || p.b);
      if (own.length && own.length < R.cols * R.rows) { const cs = own.map(([, i]) => i % R.cols), rs = own.map(([, i]) => Math.floor(i / R.cols));
        x0 = Math.max(0, Math.min(...cs) * STEP - 0.6); y0 = Math.max(0, Math.min(...rs) * STEP - 0.6); x1 = Math.min(W, (Math.max(...cs) + 1) * STEP + GAP + 0.8); y1 = Math.min(D, (Math.max(...rs) + 1) * STEP + GAP + 0.8); } }
    const pts = [iso(x0 - 0.2, y0 - 0.2), iso(x1 + 0.2, y0 - 0.2), iso(x1 + 0.2, y1 + 0.2), iso(x0 - 0.2, y1 + 0.2)];
    const minX = Math.min(...pts.map(p => p[0])), maxX = Math.max(...pts.map(p => p[0])), minY = Math.min(...pts.map(p => p[1])) - 70, maxY = Math.max(...pts.map(p => p[1]));
    const z = Math.min(w * 0.72 / (maxX - minX), h / (maxY - minY)) * 0.96; M.cam = { x: -(minX + maxX) / 2 + (w * 0.12) / z, y: -(minY + maxY) / 2, z }; M.fitted = { z, x: M.cam.x, y: M.cam.y }; clampCam();
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
    if (pts.size === 2 && M.pinch) { const [a, b] = [...pts.values()]; M.cam.z = clampZ(M.pinch.z * Math.hypot(a[0] - b[0], a[1] - b[1]) / M.pinch.d); clampCam(); if (M.drag) M.drag.moved = 99; return; }
    if (M.drag) { const dx = e.offsetX - M.drag.x, dy = e.offsetY - M.drag.y; M.drag.moved = Math.max(M.drag.moved, Math.hypot(dx, dy)); M.cam.x = M.drag.cx + dx / M.cam.z; M.cam.y = M.drag.cy + dy / M.cam.z; clampCam(); }
  });
  const up = e => { const d = M.drag; pts.delete(e.pointerId); if (pts.size < 2) M.pinch = null; if (!pts.size) { M.drag = null; if (d && d.moved < 8) { const hit = M.pick(e.offsetX, e.offsetY); onTap(hit); } } };
  canvas.addEventListener("pointerup", up); canvas.addEventListener("pointercancel", e => { pts.delete(e.pointerId); M.drag = null; M.pinch = null; });
  canvas.addEventListener("wheel", e => { e.preventDefault(); M.cam.z = clampZ(M.cam.z * (e.deltaY < 0 ? 1.1 : 0.9)); clampCam(); }, { passive: false });
  const clampZ = z => Math.max(M.mode === "world" ? 0.2 : 0.25, Math.min(2.6, z));
  // Kamera sınırı: ekranın dört köşesi her zaman zeminin (bölge + çevre halkası) üstünde kalır; boşluğa kayılmaz.
  function clampCam() {
    const [w, h] = size(); if (!w || !h) return; if (M.needFit || !(M.cam.z > 0) || !isFinite(M.cam.x) || !isFinite(M.cam.y)) { M.fit(); return; }
    if (M.mode === "world") { M.cam.z = Math.max(M.cam.z, Math.max(w / 3600, h / 2200)); const hx = Math.max(0, 1800 - w / 2 / M.cam.z), hy = Math.max(0, 1100 - h / 2 / M.cam.z); M.cam.x = Math.max(-hx, Math.min(hx, M.cam.x)); M.cam.y = Math.max(-hy, Math.min(hy, M.cam.y)); return; }
    const { W, D } = bounds(M.region), ext = RG[M.region].port ? 2.4 : 0, lim = OUTER - 0.35;
    const inside = (cx, cy, z) => [[-1, -1], [1, -1], [1, 1], [-1, 1]].every(([sx, sy]) => { const X = cx + sx * w / 2 / z, Y = cy + sy * h / 2 / z, gx = (X / HW + Y / HH) / 2, gy = (Y / HH - X / HW) / 2; return gx >= -lim && gx <= W + lim + ext && gy >= -lim && gy <= D + lim; });
    const core = iso(W / 2, D / 2); core[1] -= 30;
    let cx = -M.cam.x, cy = -M.cam.y, z = M.cam.z;
    let guard = 0; while (!inside(core[0], core[1], z) && guard++ < 60) z *= 1.04;
    if (!inside(cx, cy, z)) { let lo = 0, hi = 1; for (let k = 0; k < 18; k++) { const t = (lo + hi) / 2; if (inside(core[0] + (cx - core[0]) * t, core[1] + (cy - core[1]) * t, z)) lo = t; else hi = t; } cx = core[0] + (cx - core[0]) * lo; cy = core[1] + (cy - core[1]) * lo; }
    M.cam.z = z; M.cam.x = -cx; M.cam.y = -cy;
  }
  M.clamp = clampCam;
  M.zoom = f => { M.cam.z = clampZ(M.cam.z * f); clampCam(); };

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
    if (dl < 1) { g.fillStyle = `rgba(8,16,40,${(1 - dl) * 0.5})`; g.fillRect(0, 0, w, h);
      if (M.lights && M.mode === "region") { g.save(); g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = "lighter"; for (const [x, y, r, col] of M.lights) { const gr = g.createRadialGradient(x, y, 0, x, y, r * 1.6); gr.addColorStop(0, col); gr.addColorStop(1, "rgba(0,0,0,0)"); g.globalAlpha = (1 - dl) * 0.7; g.fillStyle = gr; g.beginPath(); g.arc(x, y, r * 1.6, 0, 7); g.fill(); } g.restore(); } }
    { const vg = g.createRadialGradient(w / 2, h / 2, Math.min(w, h) * 0.45, w / 2, h / 2, Math.max(w, h) * 0.75); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,.35)"); g.fillStyle = vg; g.fillRect(0, 0, w, h); }
    const se = season(s);
    if (se === "kis") { g.fillStyle = "rgba(255,255,255,.8)"; for (let i = 0; i < 70; i++) { const x = (i * 97.3 + t * 18 * (1 + i % 3)) % w, y = (i * 53.7 + t * 30 * (1 + i % 2)) % h; g.beginPath(); g.arc(x, y, 1 + (i % 3) * 0.6, 0, 7); g.fill(); } }
    if (se === "sonbahar" && Math.floor(s.time.min / 1440) % 3 === 0) { g.strokeStyle = "rgba(180,200,230,.35)"; g.lineWidth = 1; for (let i = 0; i < 80; i++) { const x = (i * 71.1 + t * 60) % w, y = (i * 37.3 + t * 260) % h; g.beginPath(); g.moveTo(x, y); g.lineTo(x - 3, y + 10); g.stroke(); } }
  };
  // ---- statik katman (zemin, yollar, kaldırımlar, arsalar): değişince yeniden çizilir, her karede tek drawImage ----
  let STATIC = null;
  const hashR = n => { const x = Math.sin(n * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
  function staticKey(s) { const reg = s.regions[M.region]; return [M.region, season(s), reg.owned, reg.plots.map(p => (p.owned ? 1 : 0) + (p.b ? 2 : 0)).join("")].join("|"); }
  // Dış çevre: bölgeye göre tarlalar, ormanlar, köy evleri, tepeler, göl, dağlar, kumullar, şehir blokları, ana yol bağlantıları.
  function surroundings(c, P, quad, R, W, D, se, mo, grass) {
    const id = M.region, H = (k) => hashR(k * 1.37 + id.length * 11.3), snow = se === "kis";
    const inCore = (gx, gy) => gx > -1.7 && gx < W + 1.7 && gy > -1.7 && gy < D + 1.7;
    const cropCols = snow ? ["#e8eef2", "#dfe7ec", "#f1f5f8"] : se === "sonbahar" ? ["#b8873f", "#9a6e34", "#c9a24a", "#8d7a3e"] : se === "yaz" ? ["#c9a64a", "#a8b04a", "#d7b75a", "#7f9a4a"] : ["#6f9e4f", "#8fae4f", "#5f8a44", "#a7b85a"];
    const box2 = (gx, gy, w, d, h, col, roof) => { const pts = (z, a, b, cc, dd) => [iso(a, b, z), iso(cc, b, z), iso(cc, dd, z), iso(a, dd, z)];
      P([iso(gx + w, gy), iso(gx + w, gy + d), iso(gx + w, gy + d, h), iso(gx + w, gy, h)], shade(col, -0.25)); P([iso(gx, gy + d), iso(gx + w, gy + d), iso(gx + w, gy + d, h), iso(gx, gy + d, h)], col);
      if (roof) { P([iso(gx, gy + d, h), iso(gx + w, gy + d, h), iso(gx + w, gy + d / 2, h + 7), iso(gx, gy + d / 2, h + 7)], roof); P([iso(gx + w, gy, h), iso(gx + w, gy + d, h), iso(gx + w, gy + d / 2, h + 7)], shade(roof, -0.25)); }
      else P(pts(h, gx, gy, gx + w, gy + d), shade(col, 0.2)); };
    const treeS = (gx, gy, k) => { const [x, y] = iso(gx, gy), base = snow ? "#dde6ec" : se === "sonbahar" ? ["#c9782f", "#d99a3a", "#b5562c", "#7a8a3a"][k % 4] : se === "yaz" ? "#4f7a3a" : "#5c9446";
      c.fillStyle = "rgba(4,10,18,.22)"; c.beginPath(); c.ellipse(x + 9, y + 3, 11, 4, 0.3, 0, 7); c.fill(); c.fillStyle = "#5a4030"; c.fillRect(x - 1.2, y - 8, 2.4, 8);
      if (k % 3) { c.fillStyle = shade(base, 0.08); c.beginPath(); c.arc(x, y - 15, 8, 0, 7); c.fill(); c.fillStyle = shade(base, -0.18); c.beginPath(); c.arc(x + 3, y - 12, 6, 0, 7); c.fill(); }
      else { c.fillStyle = shade(base, -0.12); c.beginPath(); c.moveTo(x - 8, y - 5); c.lineTo(x, y - 30); c.lineTo(x + 8, y - 5); c.fill(); c.fillStyle = shade(base, -0.32); c.beginPath(); c.moveTo(x, y - 30); c.lineTo(x + 8, y - 5); c.lineTo(x + 2, y - 5); c.fill(); } };
    // ana yolların dışarı uzantısı (çevreyle bağlantı) + toprak yollar
    const road = "#353c43";
    for (const r of [0, R.rows]) { const y = r * STEP; quad(-mo, y + 0.05, 0, y + GAP - 0.05, road); if (!R.port) quad(W, y + 0.05, W + mo, y + GAP - 0.05, road); }
    for (const cc of [0, R.cols]) { const x = cc * STEP; quad(x + 0.05, -mo, x + GAP - 0.05, 0, road); quad(x + 0.05, D, x + GAP - 0.05, D + mo, road); }
    c.strokeStyle = "rgba(245,240,225,.6)"; c.lineWidth = 1.1; c.setLineDash([7, 9]);
    for (const r of [0, R.rows]) { const y = r * STEP + GAP / 2; c.beginPath(); c.moveTo(...iso(-mo, y)); c.lineTo(...iso(0, y)); c.stroke(); if (!R.port) { c.beginPath(); c.moveTo(...iso(W, y)); c.lineTo(...iso(W + mo, y)); c.stroke(); } }
    for (const cc of [0, R.cols]) { const x = cc * STEP + GAP / 2; c.beginPath(); c.moveTo(...iso(x, -mo)); c.lineTo(...iso(x, 0)); c.stroke(); c.beginPath(); c.moveTo(...iso(x, D)); c.lineTo(...iso(x, D + mo)); c.stroke(); }
    c.setLineDash([]);
    // arazi: bölgeye göre
    const cells = []; for (let gx = -mo; gx < W + mo; gx += 0.9) for (let gy = -mo; gy < D + mo; gy += 0.9) { if (inCore(gx + 0.45, gy + 0.45)) continue; if (R.port && gx > W + 0.1) continue; cells.push([gx, gy]); }
    cells.forEach(([gx, gy], k) => {
      const r = H(k + 17);
      if (id === "anadolu" || id === "liman") {
        if (r < 0.55) { const col = cropCols[Math.floor(H(k + 3) * cropCols.length)]; quad(gx + 0.04, gy + 0.04, gx + 0.86, gy + 0.86, col, "rgba(60,50,30,.25)"); if (!snow) { c.strokeStyle = "rgba(0,0,0,.08)"; c.lineWidth = 1; for (let q = 1; q < 6; q++) { c.beginPath(); c.moveTo(...iso(gx + 0.04, gy + 0.04 + q * 0.14)); c.lineTo(...iso(gx + 0.86, gy + 0.04 + q * 0.14)); c.stroke(); } } }
        else if (r < 0.8) { for (let q = 0; q < 6; q++) treeS(gx + 0.15 + H(k * 7 + q) * 0.6, gy + 0.15 + H(k * 9 + q) * 0.6, k + q); }
        else if (r < 0.9) { box2(gx + 0.3, gy + 0.3, 0.22, 0.18, 12, ["#e9e2d0", "#d9c9a8", "#f0ece4"][k % 3], ["#b5452f", "#8a3a2c", "#5d6b78"][k % 3]); treeS(gx + 0.7, gy + 0.65, k); }
        else { const [x, y] = iso(gx + 0.45, gy + 0.45), gr = c.createRadialGradient(x - 10, y - 4, 4, x, y, 48); gr.addColorStop(0, snow ? "#e9f1f6" : "#4f97c4"); gr.addColorStop(1, snow ? "#c9d6df" : "#2f6f99"); c.fillStyle = gr; c.beginPath(); c.ellipse(x, y, 46, 21, 0, 0, 7); c.fill(); c.strokeStyle = shade(grass, -0.25); c.lineWidth = 2; c.stroke(); }
      } else if (id === "maden") {
        if (r < 0.35) { for (let q = 0; q < 5; q++) treeS(gx + 0.15 + H(k * 7 + q) * 0.6, gy + 0.15 + H(k * 9 + q) * 0.6, 3 * q); }
        else if (r < 0.55) { const [x, y] = iso(gx + 0.45, gy + 0.45); for (let q = 3; q > 0; q--) { c.fillStyle = shade("#7a6450", -0.08 * q); c.beginPath(); c.ellipse(x, y + (3 - q) * 3, 14 * q, 6.5 * q, 0, 0, 7); c.fill(); } }
        else { const [x, y] = iso(gx + 0.45, gy + 0.45); for (let q = 0; q < 4; q++) { c.fillStyle = shade("#8a7d62", (H(k * 3 + q) - 0.5) * 0.3); c.beginPath(); c.ellipse(x + (H(k + q) - 0.5) * 40, y + (H(k + q + 9) - 0.5) * 18, 6 + H(k + q) * 8, 3 + H(k + q) * 3, 0, 0, 7); c.fill(); } }
      } else if (id === "petrol") {
        const [x, y] = iso(gx + 0.45, gy + 0.45);
        if (r < 0.5) { const gr = c.createLinearGradient(x - 40, y - 12, x + 40, y + 10); gr.addColorStop(0, shade(grass, 0.12)); gr.addColorStop(1, shade(grass, -0.15)); c.fillStyle = gr; c.beginPath(); c.moveTo(x - 50, y + 10); c.quadraticCurveTo(x - 6, y - 26, x + 50, y + 10); c.fill(); }
        else if (r < 0.62) { c.strokeStyle = "#39434c"; c.lineWidth = 2.2; c.beginPath(); c.moveTo(x - 7, y); c.lineTo(x, y - 18); c.lineTo(x + 7, y); c.moveTo(x - 16, y - 18); c.lineTo(x + 12, y - 20); c.stroke(); }
        else if (r < 0.8) for (let q = 0; q < 3; q++) { c.fillStyle = snow ? "#e9eef2" : "#7d8a4a"; c.beginPath(); c.arc(x + (H(k + q) - 0.5) * 40, y + (H(k + q + 3) - 0.5) * 16, 3 + H(k + q) * 3, 0, 7); c.fill(); }
      } else { // teknoloji, uluslararası: şehir blokları
        if (r < 0.7) { const n = 1 + Math.floor(H(k + 5) * 3); for (let q = 0; q < n; q++) { const hh = 14 + H(k * 5 + q) * (id === "uluslararasi" ? 70 : 45); box2(gx + 0.1 + q * 0.26, gy + 0.2 + (q % 2) * 0.25, 0.22, 0.22, hh, ["#8fa3b5", "#6d7b88", "#b7c2cb", "#5a6777"][(k + q) % 4]); } }
        else for (let q = 0; q < 4; q++) treeS(gx + 0.2 + H(k * 7 + q) * 0.5, gy + 0.2 + H(k * 9 + q) * 0.5, k + q);
      }
    });
    // arka ufukta dağ silsilesi (maden ve petrol) ya da tepe hattı
    if (id === "maden" || id === "petrol" || id === "anadolu") for (let k = 0; k < 12; k++) { const [x, y] = iso(-mo + 0.2, -mo + 0.3 + k * (D + 2 * mo) / 12), hh = (id === "maden" ? 120 : 60) + H(k + 70) * (id === "maden" ? 60 : 30), base = id === "petrol" ? "#b08a55" : id === "maden" ? "#6d5a48" : "#5f7a4a";
      const gr = c.createLinearGradient(x - 90, 0, x + 90, 0); gr.addColorStop(0, shade(base, 0.12)); gr.addColorStop(0.5, base); gr.addColorStop(1, shade(base, -0.35)); c.fillStyle = gr; c.beginPath(); c.moveTo(x - 110, y + 30); c.quadraticCurveTo(x - 30, y - hh * 1.1, x, y - hh); c.quadraticCurveTo(x + 30, y - hh * 1.05, x + 110, y + 30); c.fill();
      if (id === "maden" || snow) { c.fillStyle = "#eef3f6"; c.beginPath(); c.moveTo(x - 22, y - hh + 22); c.quadraticCurveTo(x, y - hh - 4, x + 22, y - hh + 20); c.lineTo(x + 6, y - hh + 16); c.lineTo(x - 6, y - hh + 24); c.fill(); } }
  }
  function buildStatic(s) {
    const R = RG[M.region], reg = s.regions[M.region], { W, D } = bounds(M.region), se = season(s), m = MARGIN, mo = OUTER, ext = R.port ? 2.4 : 0;
    const corners = [iso(-mo, -mo), iso(W + mo + ext, -mo), iso(W + mo + ext, D + mo), iso(-mo, D + mo)];
    const minX = Math.min(...corners.map(p => p[0])) - 40, maxX = Math.max(...corners.map(p => p[0])) + 40, minY = Math.min(...corners.map(p => p[1])) - 220, maxY = Math.max(...corners.map(p => p[1])) + 40;
    const SC = 1.25, cv = document.createElement("canvas"); cv.width = Math.ceil((maxX - minX) * SC); cv.height = Math.ceil((maxY - minY) * SC);
    const c = cv.getContext("2d"); c.setTransform(SC, 0, 0, SC, -minX * SC, -minY * SC);
    const grass = M.region === "petrol" ? (se === "kis" ? "#d9d3c4" : "#c9a86a") : M.region === "maden" ? (se === "kis" ? "#d6dde3" : "#8a7d62") : GRASS[se];
    const P = (pts, col, st) => { c.beginPath(); pts.forEach((p, k) => (k ? c.lineTo(...p) : c.moveTo(...p))); c.closePath(); c.fillStyle = col; c.fill(); if (st) { c.strokeStyle = st; c.lineWidth = 0.8; c.stroke(); } };
    const quad = (x0, y0, x1, y1, col, st) => P([iso(x0, y0), iso(x1, y0), iso(x1, y1), iso(x0, y1)], col, st);
    // zemin + doku lekeleri (bölge ve dış çevre halkası)
    quad(-mo, -mo, W + mo + (R.port ? 0 : ext), D + mo, grass);
    surroundings(c, P, quad, R, W, D, se, mo, grass);
    for (let k = 0; k < 260; k++) { const gx = -m + hashR(k) * (W + 2 * m), gy = -m + hashR(k + 999) * (D + 2 * m), [x, y] = iso(gx, gy), r = 6 + hashR(k + 55) * 22;
      c.fillStyle = hashR(k + 7) > 0.5 ? shade(grass, 0.07) : shade(grass, -0.08); c.globalAlpha = 0.5; c.beginPath(); c.ellipse(x, y, r, r * 0.5, 0, 0, 7); c.fill(); }
    c.globalAlpha = 1;
    if (se !== "kis" && M.region !== "petrol") for (let k = 0; k < 120; k++) { const gx = -m + hashR(k + 3000) * (W + 2 * m), gy = -m + hashR(k + 4000) * (D + 2 * m); if (gx > 0 && gx < W && gy > 0 && gy < D) continue; const [x, y] = iso(gx, gy); c.fillStyle = ["#f2e27a", "#f4f1e8", "#e88aa0"][k % 3]; c.fillRect(x, y, 1.6, 1.6); }
    // tarlalar (Anadolu), kum tepeleri (Petrol)
    if (M.region === "anadolu") for (let k = 0; k < 3; k++) { const x0 = -m + 0.15, y0 = -m + 0.2 + k * (D + 2 * m - 0.4) / 3; for (let r2 = 0; r2 < 6; r2++) quad(x0, y0 + r2 * 0.12, x0 + 1.1, y0 + r2 * 0.12 + 0.1, se === "kis" ? "#e6edf2" : r2 % 2 ? shade(grass, 0.15) : ["#c9a64a", "#7fa04a", "#a8873f"][k]); }
    // su ve rıhtım
    if (R.port) { const wx = W + 0.25; const wg = c.createLinearGradient(...iso(wx, 0), ...iso(W + m + ext, 0)); wg.addColorStop(0, "#2f7aa3"); wg.addColorStop(1, "#1b4f73");
      P([iso(wx, -mo), iso(W + mo + ext, -mo), iso(W + mo + ext, D + mo), iso(wx, D + mo)], wg); quad(W + 0.02, -mo, wx, D + mo, "#8a7d6b", "rgba(0,0,0,.3)");
      for (let k = 0; k < 90; k++) { const [x, y] = iso(wx + 0.3 + hashR(k + 1200) * (mo + ext - 0.5), -mo + hashR(k + 1300) * (D + 2 * mo)); c.strokeStyle = "rgba(210,235,255,.16)"; c.lineWidth = 1; c.beginPath(); c.moveTo(x - 9, y); c.quadraticCurveTo(x, y - 2.5, x + 9, y); c.stroke(); }
      for (let k = 0; k < 14; k++) { const [x, y] = iso(W + 0.06, -m + 0.4 + k * (D + 2 * m) / 14); c.fillStyle = "#3d3a36"; c.beginPath(); c.ellipse(x, y, 3, 1.5, 0, 0, 7); c.fill(); } }
    // demiryolu
    if (R.rail) { const y0 = -0.78, x1 = W + (R.port ? 0.2 : mo); quad(-mo, y0 - 0.04, x1, y0 + 0.2, "#7a6b5a"); for (let k = 0; k < 160; k++) { const gx = -mo + k * (x1 + mo) / 160; quad(gx, y0 + 0.01, gx + 0.03, y0 + 0.15, "#5a4637"); }
      c.strokeStyle = "#b6c0c8"; c.lineWidth = 1.3; for (const yy of [y0 + 0.04, y0 + 0.12]) { c.beginPath(); c.moveTo(...iso(-mo, yy)); c.lineTo(...iso(x1, yy)); c.stroke(); } }
    // havalimanı pisti
    if (R.airport) { quad(-m + 0.1, D + 0.45, W + 0.1, D + 0.95, "#3b444c"); c.strokeStyle = "#e9eef2"; c.lineWidth = 2; c.setLineDash([14, 12]); c.beginPath(); c.moveTo(...iso(-m + 0.2, D + 0.7)); c.lineTo(...iso(W, D + 0.7)); c.stroke(); c.setLineDash([]); }
    // dağlar ve silüet (arka kenar)
    if (M.region === "maden") for (let k = 0; k < 7; k++) { const [x, y] = iso(-m + 0.15, -m + 0.4 + k * (D + 2) / 7), hh = 110 + (k % 3) * 35, gr = c.createLinearGradient(x - 90, 0, x + 90, 0); gr.addColorStop(0, shade("#7a6450", 0.12)); gr.addColorStop(0.5, "#6d5a48"); gr.addColorStop(1, shade("#6d5a48", -0.35));
      c.fillStyle = gr; c.beginPath(); c.moveTo(x - 100, y + 24); c.lineTo(x - 10, y - hh); c.lineTo(x + 100, y + 24); c.fill(); c.fillStyle = "#eef3f6"; c.beginPath(); c.moveTo(x - 30, y - hh + 34); c.lineTo(x - 10, y - hh); c.lineTo(x + 14, y - hh + 30); c.lineTo(x - 2, y - hh + 26); c.fill(); }
    // yollar: asfalt, kaldırım, şerit, yaya geçidi, rögar
    const road = "#353c43", walk = se === "kis" ? "#d7dee4" : "#9aa1a6";
    for (let r = 0; r <= R.rows; r++) { const y = r * STEP; quad(0, y, W, y + GAP, road); quad(0, y, W, y + 0.035, walk); quad(0, y + GAP - 0.035, W, y + GAP, walk); }
    for (let cc = 0; cc <= R.cols; cc++) { const x = cc * STEP; quad(x, 0, x + GAP, D, road); quad(x, 0, x + 0.035, D, walk); quad(x + GAP - 0.035, 0, x + GAP, D, walk); }
    for (let r = 0; r <= R.rows; r++) for (let cc = 0; cc <= R.cols; cc++) quad(cc * STEP + 0.035, r * STEP + 0.035, cc * STEP + GAP - 0.035, r * STEP + GAP - 0.035, road);
    for (let k = 0; k < 500; k++) { const gx = hashR(k + 7000) * W, gy = hashR(k + 8000) * D; const onRoad = (gx % STEP) < GAP || (gy % STEP) < GAP; if (!onRoad) continue; const [x, y] = iso(gx, gy); c.fillStyle = hashR(k) > 0.5 ? "rgba(255,255,255,.05)" : "rgba(0,0,0,.12)"; c.fillRect(x, y, 2, 1); }
    c.strokeStyle = "rgba(245,240,225,.75)"; c.lineWidth = 1.2; c.setLineDash([7, 9]);
    for (let r = 0; r <= R.rows; r++) for (let cc = 0; cc < R.cols; cc++) { const y = r * STEP + GAP / 2, x0 = cc * STEP + GAP + 0.06, x1 = (cc + 1) * STEP - 0.06; c.beginPath(); c.moveTo(...iso(x0, y)); c.lineTo(...iso(x1, y)); c.stroke(); }
    for (let cc = 0; cc <= R.cols; cc++) for (let r = 0; r < R.rows; r++) { const x = cc * STEP + GAP / 2, y0 = r * STEP + GAP + 0.06, y1 = (r + 1) * STEP - 0.06; c.beginPath(); c.moveTo(...iso(x, y0)); c.lineTo(...iso(x, y1)); c.stroke(); }
    c.setLineDash([]);
    for (let r = 0; r <= R.rows; r++) for (let cc = 0; cc <= R.cols; cc++) { const x = cc * STEP, y = r * STEP;
      for (let k = 0; k < 5; k++) { if (cc < R.cols) quad(x + GAP + 0.01, y + 0.06 + k * 0.055, x + GAP + 0.07, y + 0.09 + k * 0.055, "rgba(240,240,240,.7)"); if (r < R.rows) quad(x + 0.06 + k * 0.055, y + GAP + 0.01, x + 0.09 + k * 0.055, y + GAP + 0.07, "rgba(240,240,240,.7)"); }
      const [mx, my] = iso(x + GAP * 0.7, y + GAP * 0.3); c.fillStyle = "#2a2f34"; c.beginPath(); c.ellipse(mx, my, 3, 1.5, 0, 0, 7); c.fill(); }
    // arsalar
    for (let r = 0; r < R.rows; r++) for (let cc = 0; cc < R.cols; cc++) {
      const i = r * R.cols + cc, p = reg.plots[i], [ox, oy] = plotOrigin(cc, r);
      if (!reg.owned) { quad(ox, oy, ox + 1, oy + 1, shade(grass, -0.08)); continue; }
      if (p.owned) { quad(ox, oy, ox + 1, oy + 1, se === "kis" ? "#cfd8df" : "#aeb5b9", "rgba(0,0,0,.25)");
        c.strokeStyle = "rgba(0,0,0,.09)"; c.lineWidth = 0.8; for (let k = 1; k < 5; k++) { c.beginPath(); c.moveTo(...iso(ox + k / 5, oy)); c.lineTo(...iso(ox + k / 5, oy + 1)); c.moveTo(...iso(ox, oy + k / 5)); c.lineTo(...iso(ox + 1, oy + k / 5)); c.stroke(); }
        if (p.b) { // otopark çizgileri ön kenarda
          quad(ox + 0.04, oy + 0.88, ox + 0.96, oy + 0.98, "#4a5158"); c.strokeStyle = "rgba(255,255,255,.7)"; c.lineWidth = 1; for (let k = 0; k < 9; k++) { c.beginPath(); c.moveTo(...iso(ox + 0.06 + k * 0.1, oy + 0.885)); c.lineTo(...iso(ox + 0.06 + k * 0.1, oy + 0.975)); c.stroke(); } } }
      else { quad(ox + 0.03, oy + 0.03, ox + 0.97, oy + 0.97, shade(grass, -0.05)); for (let k = 0; k < 30; k++) { const [x, y] = iso(ox + hashR(i * 50 + k) , oy + hashR(i * 50 + k + 25)); c.fillStyle = shade(grass, -0.2); c.fillRect(x, y, 2, 1); }
        c.strokeStyle = "rgba(90,70,50,.7)"; c.lineWidth = 1; c.beginPath(); [iso(ox + 0.05, oy + 0.05), iso(ox + 0.95, oy + 0.05), iso(ox + 0.95, oy + 0.95), iso(ox + 0.05, oy + 0.95)].forEach((q, k) => (k ? c.lineTo(...q) : c.moveTo(...q))); c.closePath(); c.stroke(); }
    }
    STATIC = { key: staticKey(s), cv, minX, minY, SC };
  }
  function drawRegion(s, t, dl) {
    const R = RG[M.region], reg = s.regions[M.region], { W, D } = bounds(M.region), se = season(s), night = dl < 0.6, m = MARGIN;
    if (!STATIC || STATIC.key !== staticKey(s)) buildStatic(s);
    g.drawImage(STATIC.cv, STATIC.minX, STATIC.minY, STATIC.cv.width / STATIC.SC, STATIC.cv.height / STATIC.SC);
    // su dalgaları
    if (R.port) { g.strokeStyle = "rgba(200,232,255,.35)"; g.lineWidth = 1.2; for (let k = 0; k < 26; k++) { const [x, y] = iso(W + 0.5 + (k % 4) * 0.55 + Math.sin(t * 0.6 + k) * 0.05, -m + k * 0.3); g.beginPath(); g.moveTo(x - 10, y); g.quadraticCurveTo(x, y - 3, x + 10, y); g.stroke(); } }
    // --- sahne nesneleri (derinliğe göre sıralanır) ---
    const items = [], push = (depth, fn) => items.push([depth, fn]);
    const order = []; for (let r = 0; r < R.rows; r++) for (let c = 0; c < R.cols; c++) order.push([c, r]);
    for (const [c, r] of order) {
      const i = r * R.cols + c, p = reg.plots[i], [ox, oy] = plotOrigin(c, r), b = p.b && s.buildings[p.b];
      if (!reg.owned) continue;
      if (b) { push(ox + oy + 1, g2 => drawBuilding(s, b, ox, oy, t, dl, se, g2));
        for (let k = 0; k < 3; k++) if (hashR(i * 9 + k) > 0.35) { const px = ox + 0.12 + k * 0.28 + hashR(i + k) * 0.05; push(px + oy + 0.93, g2 => car(g2, px, oy + 0.93, 2, (i * 3 + k) % 8, false)); } }
      else if (!p.owned) push(ox + oy + 1, g2 => sign(ox + 0.5, oy + 0.5, "SATILIK", g2));
      else push(ox + oy + 1, g2 => plus(ox, oy, t, g2));
    }
    // ağaçlar, lambalar
    for (let k = 0; k < 16; k++) { const back = k % 2 === 0, gx = back ? -0.45 - hashR(k) * 1.0 : W + 0.25 + (R.port ? -0.1 : hashR(k) * 0.9), gy = -0.6 + hashR(k + 40) * (D + 1.4); if (R.port && !back) continue; push(gx + gy, g2 => treeArt(g2, gx, gy, se, k)); }
    for (let k = 0; k < 10; k++) { const gx = hashR(k + 90) * (W + 1) - 0.5, gy = D + 0.25 + hashR(k + 91) * 1.1; if (R.airport) continue; push(gx + gy, g2 => treeArt(g2, gx, gy, se, k + 1)); }
    for (let r = 0; r <= R.rows; r++) for (let c = 0; c <= R.cols; c++) { const gx = c * STEP + 0.02, gy = r * STEP + 0.02; push(gx + gy, g2 => lamp(g2, gx, gy, night)); }
    // trafik: şeritlerde iki yönlü araçlar
    const active = s.shipments.filter(x => x.to === M.region || x.from === M.region).length + Object.values(s.buildings).filter(b => b.region === M.region && b.status === "Çalışıyor").length;
    const nV = Math.min(22, 8 + active * 2);
    for (let k = 0; k < nV; k++) {
      const horiz = k % 2 === 0, lane = (k >> 1) % 2, sp = 0.028 + hashR(k + 300) * 0.02, ph = (t * sp + hashR(k + 400)) % 1, isTruck = k % 3 === 0;
      if (horiz) { const r = Math.floor(hashR(k + 500) * (R.rows + 1)), y = r * STEP + (lane ? 0.245 : 0.105), dir = lane ? -1 : 1, x = dir > 0 ? ph * W : (1 - ph) * W;
        push(x + y, g2 => isTruck ? truck(g2, x, y, dir, ["#3f7fa8", "#c94a3a", "#4ca86a", "#e0a83a"][k % 4], ["#e9eef2", "#c9d3dc", "#f2b631", "#9fb3c6"][k % 4], k % 2 === 0, night) : car(g2, x, y, dir, k, night)); }
      else { const c = Math.floor(hashR(k + 600) * (R.cols + 1)), x = c * STEP + (lane ? 0.105 : 0.245), dir = lane ? -2 : 2, y = dir > 0 ? ph * D : (1 - ph) * D;
        push(x + y, g2 => isTruck ? truck(g2, x, y, dir, ["#e0a83a", "#3f7fa8", "#c94a3a"][k % 3], "#e9eef2", k % 2 === 1, night) : car(g2, x, y, dir, k + 3, night)); }
    }
    if (R.rail) { const tx = -m + ((t * 0.08) % 1) * (W + 2 * m + 2); push(tx - 0.78 - 2, g2 => train(g2, tx, -0.75, 7, t)); }
    if (R.port) { push(W + 0.55 + D * 0.25 + 5, g2 => ship(g2, W + 0.6, D * 0.2 + Math.sin(t * 0.05) * 0.2, t)); if (D > 2.5) push(W + 0.5 + D * 0.7 + 5, g2 => ship(g2, W + 0.55, D * 0.66, t + 2));
      for (let k = 0; k < 2; k++) push(W + 0.12 + 0.8 + k * 1.6, g2 => { const [cx, cy] = iso(W + 0.12, 0.8 + k * 1.6); g2.strokeStyle = "#e0a83a"; g2.lineWidth = 3; g2.beginPath(); g2.moveTo(cx - 6, cy); g2.lineTo(cx, cy - 74); g2.lineTo(cx + 6, cy); g2.moveTo(cx, cy - 70); g2.lineTo(cx + 64, cy - 52); g2.stroke(); const hk = 30 + Math.sin(t * 0.7 + k) * 16; g2.lineWidth = 1; g2.beginPath(); g2.moveTo(cx + 44, cy - 57); g2.lineTo(cx + 44, cy - 57 + hk); g2.stroke(); g2.fillStyle = ["#c94a3a", "#3f7fa8"][k]; g2.fillRect(cx + 38, cy - 57 + hk, 12, 7); }); }
    if (M.region === "teknoloji" || M.region === "uluslararasi") for (let k = 0; k < 7; k++) { const gx = -m + 0.3, gy = -m + 0.5 + k * (D + 2) / 7, hh = 90 + (k * 37) % 110; push(gx + gy - 3, g2 => { box(g2, gx, gy, 0.45, 0.4, hh, M.region === "teknoloji" ? "#4a6f8f" : "#5a6777", { top: "#8fa3b5" }); for (let q = 0; q < hh / 12; q++) { const lit = night && (k + q) % 3; fillPoly(g2, [iso(gx + 0.45, gy + 0.05, q * 12 + 4), iso(gx + 0.45, gy + 0.35, q * 12 + 4), iso(gx + 0.45, gy + 0.35, q * 12 + 9), iso(gx + 0.45, gy + 0.05, q * 12 + 9)], lit ? "#ffd98a" : "rgba(170,215,245,.85)"); fillPoly(g2, [iso(gx + 0.05, gy + 0.4, q * 12 + 4), iso(gx + 0.4, gy + 0.4, q * 12 + 4), iso(gx + 0.4, gy + 0.4, q * 12 + 9), iso(gx + 0.05, gy + 0.4, q * 12 + 9)], lit ? "#ffcf70" : "rgba(140,190,225,.8)"); } }); }
    items.sort((a, b) => a[0] - b[0]);
    // önce bütün gölgeler (yalnızca zemine), sonra nesneler
    shadowPass(g, ng => { for (const [, fn] of items) fn(ng); });
    const lights = []; recordLights(lights); M.labels = [];
    for (const [, fn] of items) fn(g);
    for (const fn of M.labels) fn();
    if (R.airport) { const ph = (t * 0.05) % 1, [px, py] = iso(-m + ph * (W + m + 2), D + 0.7); plane(g, px, py - ph * ph * 120, 1.2); }
    recordLights(null);
    M.lights = night ? lights : null;
    if (!reg.owned) { const [x, y] = iso(W / 2, D / 2); g.fillStyle = "rgba(10,18,26,.55)"; g.fillRect(x - 1200, y - 800, 2400, 1600); }
    // bulut gölgeleri
    if (dl > 0.3) for (let k = 0; k < 3; k++) { const gx = ((t * 0.01 + k * 0.37) % 1) * (W + 4) - 2, gy = D * (0.2 + k * 0.3), [x, y] = iso(gx, gy), gr = g.createRadialGradient(x, y, 10, x, y, 160); gr.addColorStop(0, "rgba(10,20,30,.13)"); gr.addColorStop(1, "rgba(10,20,30,0)"); g.fillStyle = gr; g.beginPath(); g.ellipse(x, y, 200, 90, 0, 0, 7); g.fill(); }
  }
  function plus(ox, oy, t, g2 = g) { g2.setLineDash([6, 6]); g2.strokeStyle = "rgba(255,255,255,.6)"; g2.lineWidth = 1.5; g2.beginPath(); [iso(ox + 0.12, oy + 0.12), iso(ox + 0.88, oy + 0.12), iso(ox + 0.88, oy + 0.88), iso(ox + 0.12, oy + 0.88)].forEach((p, i) => (i ? g2.lineTo(...p) : g2.moveTo(...p))); g2.closePath(); g2.stroke(); g2.setLineDash([]);
    const [x, y] = iso(ox + 0.5, oy + 0.5), pulse = 1 + Math.sin(t * 3) * 0.06; g2.fillStyle = "rgba(247,197,72,.95)"; g2.beginPath(); g2.arc(x, y, 14 * pulse, 0, 7); g2.fill(); g2.fillStyle = "#2b1d00"; g2.font = "900 19px Nunito, sans-serif"; g2.textAlign = "center"; g2.textBaseline = "middle"; g2.fillText("+", x, y + 1); }
  function P4(x, y, sz) { g.beginPath(); [iso(x, y), iso(x + sz, y), iso(x + sz, y + sz), iso(x, y + sz)].forEach((p, i) => (i ? g.lineTo(...p) : g.moveTo(...p))); g.closePath(); }
  function sign(gx, gy, text, g2 = g) { const [x, y] = iso(gx, gy); g2.strokeStyle = "#5a4030"; g2.lineWidth = 2; g2.beginPath(); g2.moveTo(x - 14, y); g2.lineTo(x - 14, y - 24); g2.moveTo(x + 14, y); g2.lineTo(x + 14, y - 24); g2.stroke(); g2.fillStyle = "#f7c548"; g2.fillRect(x - 30, y - 40, 60, 18); g2.strokeStyle = "#b07d16"; g2.lineWidth = 1; g2.strokeRect(x - 30, y - 40, 60, 18); g2.fillStyle = "#2b1d00"; g2.font = "900 10px Nunito, sans-serif"; g2.textAlign = "center"; g2.textBaseline = "middle"; g2.fillText(text, x, y - 31); }
  function drawBuilding(s, b, ox, oy, t, dl, se, g2 = g) {
    const isW = b.kind === "warehouse", look = isW ? WT[b.type].look : FT[b.type].look, rec = !isW && RECIPE[b.recipe];
    const run = b.status === "Çalışıyor", seed = parseInt(String(b.id).replace(/\D/g, "")) || 1;
    const c = { g: g2, x: ox, y: oy, t, run, night: dl < 0.6, lvl: b.level || 1, m: b.machines || 1, snow: se === "kis", seed, sign: (s.company.name.split(" ")[0] || "").toLocaleUpperCase("tr").slice(0, 10) };
    if (b.type === "maden" && rec) c.oreColor = RES[rec.res]?.color; if (b.type === "ciftlik" && rec) c.crop = RES[rec.res]?.color; if (rec && rec.id === "e_gunes") c.solar = true;
    (BUILD[look] || BUILD.shed)(c);
    if (g2 !== g) return; // gölge geçişi
    if (run) for (let k = 0; k < Math.min(4, c.m); k++) { const ph = (t * 0.07 + k * 0.29 + seed * 0.1) % 1; person(g, ox + 0.1 + ph * 0.8, oy + 0.86, t, k + seed, k % 2 ? "#f2b631" : "#e9eef2"); }
    (M.labels = M.labels || []).push(() => {
      if (!isW) badge(s, b, ...iso(ox + 0.5, oy + 0.5, 92 + 6 * (c.lvl - 1)), t);
      const [lx, ly] = iso(ox + 1, oy + 1); g.font = "800 11px Nunito, sans-serif"; g.textAlign = "center"; const label = isW ? WT[b.type].name : FT[b.type].name.replace(" fabrikası", "");
      const tw = g.measureText(label).width + 12; g.fillStyle = "rgba(10,18,26,.82)"; rr(lx - tw / 2, ly - 8, tw, 16, 8); g.fill(); g.fillStyle = "#e9eef2"; g.textBaseline = "middle"; g.fillText(label + (b.level > 1 ? " " + b.level : ""), lx, ly); });
    if (M.sel && M.sel.plot === b.plot && M.region === b.region) { const a = 0.6 + Math.sin(t * 5) * 0.3; g.strokeStyle = `rgba(247,197,72,${a})`; g.lineWidth = 3; P4(ox - 0.02, oy - 0.02, 1.04); g.stroke(); }
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
