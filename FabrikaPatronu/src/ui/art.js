// İzometrik 2D çizim kütüphanesi: kutu, çatı, silindir, baca dumanı, bant, pencereler ve 25 bina/tesis çizimi.
// Koordinat: ızgara birimi (arsa = 1×1). iso(gx, gy, z) → ekran. Işık sol-üstten: üst açık, sol orta, sağ koyu.
import { shade } from "./icons.js";
export const HW = 80, HH = 40;
export const iso = (gx, gy, z = 0) => [(gx - gy) * HW, (gx + gy) * HH - z];
const P = (g, pts) => { g.beginPath(); pts.forEach(([x, y], i) => (i ? g.lineTo(x, y) : g.moveTo(x, y))); g.closePath(); };
export function fillPoly(g, pts, color, stroke) { P(g, pts); g.fillStyle = color; g.fill(); if (stroke) { g.strokeStyle = stroke; g.lineWidth = 0.8; g.stroke(); } }
// Gölge geçişi: withShadows(g, fn) önce bütün kutu/silindirlerin zemine düşen gölgesini çizer, sonra fn normal çizilir.
let SHADOW = null; const NULLG = new Proxy({}, { get: (t, k) => (k in t ? t[k] : () => NULLG), set: (t, k, v) => { t[k] = v; return true; } });
export function shadowPass(g, fn) { SHADOW = g; g.save(); g.fillStyle = "rgba(4,10,18,.36)"; try { fn(NULLG); } finally { g.restore(); SHADOW = null; } }
export let LIGHTS = null; export const recordLights = arr => { LIGHTS = arr; };
export function withShadows(g, fn) { SHADOW = g; g.save(); g.fillStyle = "rgba(6,14,22,.28)"; try { fn(NULLG); } finally { g.restore(); SHADOW = null; } fn(g); }
const SUN = [1.15, 0.5]; // gölge yönü: yükseklik başına ekran kayması
function castShadow(pts, h) { const g = SHADOW, [dx, dy] = [SUN[0] * h, SUN[1] * h];
  const all = [...pts, ...pts.map(([x, y]) => [x + dx, y + dy])]; // dışbükey zarf (basit): sırala
  const hull = convex(all); g.beginPath(); hull.forEach(([x, y], k) => (k ? g.lineTo(x, y) : g.moveTo(x, y))); g.closePath(); g.fill(); }
function convex(p) { p = p.slice().sort((a, b) => a[0] - b[0] || a[1] - b[1]); const cr = (o, a, b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]); const lo = [], up = [];
  for (const q of p) { while (lo.length >= 2 && cr(lo[lo.length - 2], lo[lo.length - 1], q) <= 0) lo.pop(); lo.push(q); }
  for (const q of p.reverse()) { while (up.length >= 2 && cr(up[up.length - 2], up[up.length - 1], q) <= 0) up.pop(); up.push(q); } up.pop(); lo.pop(); return lo.concat(up); }
function faceGrad(g, pts, c1, c2) { const ys = pts.map(p => p[1]), gr = g.createLinearGradient(0, Math.min(...ys), 0, Math.max(...ys)); gr.addColorStop(0, c1); gr.addColorStop(1, c2); return gr; }
export function box(g, x, y, w, d, h, col, opt = {}) {
  const z = opt.z || 0;
  if (SHADOW) { if (z < 2 && h > 2) castShadow([iso(x, y, z), iso(x + w, y, z), iso(x + w, y + d, z), iso(x, y + d, z)], h + z); return; }
  const top = opt.top || shade(col, 0.22), left = opt.left || col, right = opt.right || shade(col, -0.25), edge = opt.edge ?? "rgba(0,0,0,.28)";
  const R = [iso(x + w, y, z), iso(x + w, y + d, z), iso(x + w, y + d, z + h), iso(x + w, y, z + h)], Lf = [iso(x, y + d, z), iso(x + w, y + d, z), iso(x + w, y + d, z + h), iso(x, y + d, z + h)];
  fillPoly(g, R, h > 6 ? faceGrad(g, R, shade(right, 0.08), shade(right, -0.12)) : right, edge);
  fillPoly(g, Lf, h > 6 ? faceGrad(g, Lf, shade(left, 0.1), shade(left, -0.14)) : left, edge);
  if (h > 10 && !opt.flat) { // zemin teması: alt kenarda yumuşak kararma (ortam gölgesi)
    const ao = (P2, k) => { const a = P2[0], b = P2[1]; const gr = g.createLinearGradient(0, a[1], 0, a[1] - 8); gr.addColorStop(0, "rgba(0,0,0,.25)"); gr.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = gr; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineTo(b[0], b[1] - 8); g.lineTo(a[0], a[1] - 8); g.closePath(); g.fill(); };
    if (!z) { ao(Lf); ao(R); } }
  if (!opt.noTop) { const T = [iso(x, y, z + h), iso(x + w, y, z + h), iso(x + w, y + d, z + h), iso(x, y + d, z + h)]; fillPoly(g, T, top, edge);
    g.strokeStyle = "rgba(255,255,255,.18)"; g.lineWidth = 1; g.beginPath(); g.moveTo(...T[3]); g.lineTo(...T[2]); g.lineTo(...T[1]); g.stroke();
    if (opt.roof && w > 0.25 && d > 0.2) roofUnits(g, x, y, w, d, z + h, opt.roof); }
}
// Düz çatı ayrıntısı: klima üniteleri, havalandırma, çatı ışıkları.
export function roofUnits(g, x, y, w, d, h, seed = 1) {
  const r = n => ((Math.sin(seed * 12.9 + n * 78.2) * 43758.5) % 1 + 1) % 1;
  for (let i = 0; i < 2 + (seed % 2); i++) box(g, x + w * (0.12 + r(i) * 0.6), y + d * (0.15 + r(i + 5) * 0.5), 0.07, 0.06, 5, "#9aa5ae", { z: h, flat: 1 });
  for (let i = 0; i < 2; i++) { const [vx, vy] = iso(x + w * (0.2 + i * 0.45), y + d * 0.75, h); g.fillStyle = "#5d6973"; g.beginPath(); g.ellipse(vx, vy - 2, 3, 1.6, 0, 0, 7); g.fill(); g.fillRect(vx - 3, vy - 6, 6, 4); }
  fillPoly(g, [iso(x + w * 0.55, y + d * 0.2, h + 0.5), iso(x + w * 0.8, y + d * 0.2, h + 0.5), iso(x + w * 0.8, y + d * 0.35, h + 0.5), iso(x + w * 0.55, y + d * 0.35, h + 0.5)], "rgba(160,215,245,.55)", "rgba(0,0,0,.2)");
}
// Cephe tabelası (şirket ya da tesis adı).
export function facadeSign(g, x, y, w, d, h, text, col = "#f7c548") {
  const u0 = 0.12, u1 = 0.88, z0 = h * 0.72, z1 = h * 0.92, pts = [iso(x + w * u0, y + d, z0), iso(x + w * u1, y + d, z0), iso(x + w * u1, y + d, z1), iso(x + w * u0, y + d, z1)];
  fillPoly(g, pts, "#0f1c28", col); const [ax, ay] = pts[0], [bx, by] = pts[1], ang = Math.atan2(by - ay, bx - ax), len = Math.hypot(bx - ax, by - ay), hh = (z1 - z0);
  g.save(); g.translate((ax + pts[2][0]) / 2, (ay + pts[2][1]) / 2); g.transform(Math.cos(ang), Math.sin(ang), 0, 1, 0, 0); g.fillStyle = col; g.font = `900 ${Math.max(5, Math.min(hh * 0.75, len / Math.max(4, text.length) * 1.5))}px Nunito, sans-serif`; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(text, 0, 0.5); g.restore();
}
// Kırma çatı: mahya gx yönünde.
export function gable(g, x, y, w, d, h, rh, col) {
  fillPoly(g, [iso(x, y + d, h), iso(x + w, y + d, h), iso(x + w, y + d / 2, h + rh), iso(x, y + d / 2, h + rh)], shade(col, 0.1), "rgba(0,0,0,.25)");
  fillPoly(g, [iso(x + w, y, h), iso(x + w, y + d, h), iso(x + w, y + d / 2, h + rh)], shade(col, -0.3), "rgba(0,0,0,.25)");
  g.strokeStyle = shade(col, 0.35); g.lineWidth = 1; g.beginPath(); g.moveTo(...iso(x, y + d / 2, h + rh)); g.lineTo(...iso(x + w, y + d / 2, h + rh)); g.stroke();
}
export function sawtooth(g, x, y, w, d, h, n, col, glass = "#9fd8f0") {
  for (let i = 0; i < n; i++) { const yy = y + i * d / n, dd = d / n;
    fillPoly(g, [iso(x, yy + dd, h), iso(x + w, yy + dd, h), iso(x + w, yy + dd, h + 14), iso(x, yy + dd, h + 14)], glass, "rgba(0,0,0,.2)");
    fillPoly(g, [iso(x, yy, h), iso(x + w, yy, h), iso(x + w, yy + dd, h + 14), iso(x, yy + dd, h + 14)], shade(col, 0.15), "rgba(0,0,0,.2)"); }
}
// Yüzey üzerinde pencere sırası. face: "L" (ön-sol, +gy) | "R" (ön-sağ, +gx).
export function windows(g, x, y, w, d, h, face, rows, cols, lit, col = "#2b4256", z = 0) {
  if (SHADOW) return;
  for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) {
    const u0 = (c + 0.18) / cols, u1 = (c + 0.82) / cols, v0 = z + h * (r + 0.22) / rows, v1 = z + h * (r + 0.72) / rows;
    const pts = face === "L" ? [iso(x + w * u0, y + d, v0), iso(x + w * u1, y + d, v0), iso(x + w * u1, y + d, v1), iso(x + w * u0, y + d, v1)]
      : [iso(x + w, y + d * u0, v0), iso(x + w, y + d * u1, v0), iso(x + w, y + d * u1, v1), iso(x + w, y + d * u0, v1)];
    const on = lit && ((r * 7 + c * 3) % 5);
    fillPoly(g, pts, on ? "#ffd98a" : faceGrad(g, pts, shade(col, 0.35), shade(col, -0.2)), "rgba(220,230,240,.35)");
    if (!on) { g.strokeStyle = "rgba(255,255,255,.22)"; g.lineWidth = 1; g.beginPath(); g.moveTo(pts[0][0] + (pts[1][0] - pts[0][0]) * 0.2, pts[0][1] + (pts[1][1] - pts[0][1]) * 0.2 - 1); g.lineTo(pts[3][0] + (pts[2][0] - pts[3][0]) * 0.55, pts[3][1] + (pts[2][1] - pts[3][1]) * 0.55 + 1); g.stroke(); }
    else { const cx = (pts[0][0] + pts[2][0]) / 2, cy = (pts[0][1] + pts[2][1]) / 2; glow(g, cx, cy, 9, "rgba(255,200,110,.35)"); }
  }
}
export function door(g, x, y, w, d, face, u, wu, hh, col = "#39434c") {
  const pts = face === "L" ? [iso(x + w * u, y + d, 0), iso(x + w * (u + wu), y + d, 0), iso(x + w * (u + wu), y + d, hh), iso(x + w * u, y + d, hh)]
    : [iso(x + w, y + d * u, 0), iso(x + w, y + d * (u + wu), 0), iso(x + w, y + d * (u + wu), hh), iso(x + w, y + d * u, hh)];
  fillPoly(g, pts, col, "rgba(0,0,0,.3)");
  g.strokeStyle = "rgba(255,255,255,.12)"; g.lineWidth = 1; for (let i = 1; i < 5; i++) { const t = i / 5; const a = face === "L" ? iso(x + w * u, y + d, hh * t) : iso(x + w, y + d * u, hh * t), b = face === "L" ? iso(x + w * (u + wu), y + d, hh * t) : iso(x + w, y + d * (u + wu), hh * t); g.beginPath(); g.moveTo(...a); g.lineTo(...b); g.stroke(); }
}
export function cyl(g, cx, cy, r, h, col, opt = {}) {
  if (SHADOW) { if (!opt.z && h > 3) { const [x0, y0] = iso(cx, cy), rx = r * HW * 1.414, ry = r * HH * 1.414, g2 = SHADOW; g2.beginPath(); g2.ellipse(x0, y0, rx, ry, 0, 0, 7); g2.fill(); g2.beginPath(); g2.moveTo(x0 - rx * 0.7, y0 - ry * 0.7); g2.lineTo(x0 + SUN[0] * h - rx * 0.5, y0 + SUN[1] * h - ry); g2.lineTo(x0 + SUN[0] * h + rx * 0.5, y0 + SUN[1] * h + ry); g2.lineTo(x0 + rx * 0.7, y0 + ry * 0.7); g2.fill(); g2.beginPath(); g2.ellipse(x0 + SUN[0] * h, y0 + SUN[1] * h, rx, ry, 0, 0, 7); g2.fill(); } return; }
  const [x, y] = iso(cx, cy, opt.z || 0), rx = r * HW * 1.414, ry = r * HH * 1.414, gr = g.createLinearGradient(x - rx, 0, x + rx, 0);
  gr.addColorStop(0, shade(col, 0.15)); gr.addColorStop(0.45, shade(col, 0.3)); gr.addColorStop(1, shade(col, -0.35));
  g.fillStyle = gr; g.beginPath(); g.ellipse(x, y, rx, ry, 0, 0, Math.PI); g.lineTo(x - rx, y - h); g.ellipse(x, y - h, rx, ry, 0, Math.PI, 0, true); g.closePath(); g.fill();
  g.strokeStyle = "rgba(0,0,0,.25)"; g.lineWidth = 0.8; g.stroke();
  if (opt.bands) { g.strokeStyle = opt.bands; g.lineWidth = 1.4; for (const f of opt.bandAt || [0.33, 0.66]) { g.beginPath(); g.ellipse(x, y - h * f, rx, ry, 0, 0, Math.PI); g.stroke(); } }
  if (!opt.open) { g.fillStyle = opt.top || shade(col, 0.35); g.beginPath(); g.ellipse(x, y - h, rx, ry, 0, 0, Math.PI * 2); g.fill(); g.stroke(); }
  if (opt.dome) { g.fillStyle = shade(col, 0.25); g.beginPath(); g.ellipse(x, y - h, rx, ry + opt.dome, 0, Math.PI, 0); g.fill(); g.stroke(); }
}
export function sphereTank(g, cx, cy, r, col) {
  const [x, y] = iso(cx, cy, 0), R = r * HW * 1.3, gr = g.createRadialGradient(x - R * 0.35, y - R * 1.5, 2, x, y - R, R * 1.2);
  gr.addColorStop(0, shade(col, 0.5)); gr.addColorStop(1, shade(col, -0.35));
  g.strokeStyle = "#5d6973"; g.lineWidth = 2; for (const dx of [-0.7, 0, 0.7]) { g.beginPath(); g.moveTo(x + dx * R, y); g.lineTo(x + dx * R * 0.8, y - R * 0.6); g.stroke(); }
  g.fillStyle = gr; g.beginPath(); g.arc(x, y - R, R, 0, Math.PI * 2); g.fill(); g.strokeStyle = "rgba(0,0,0,.25)"; g.lineWidth = 0.8; g.stroke();
}
// Baca + duman. Duman, çalışırken sürekli yükselen ve dağılan puflardır.
export function chimney(g, cx, cy, h, col, running, t, seed = 0, smoke = "rgba(220,226,232,") {
  cyl(g, cx, cy, 0.04, h, col, { bands: "#c94a3a", bandAt: [0.85, 0.92] });
  if (!running) return;
  const [x, y] = iso(cx, cy, h);
  for (let i = 0; i < 7; i++) {
    const ph = (t * 0.35 + i / 7 + seed * 0.13) % 1, px = x + Math.sin(ph * 5 + seed) * 6 + ph * 26, py = y - ph * 60, r = 4 + ph * 13;
    g.fillStyle = smoke + (0.55 * (1 - ph)).toFixed(3) + ")"; g.beginPath(); g.arc(px, py, r, 0, Math.PI * 2); g.fill();
  }
}
export function glow(g, x, y, r, col) { if (SHADOW) return; if (LIGHTS) { const m = g.getTransform(); LIGHTS.push([m.a * x + m.c * y + m.e, m.b * x + m.d * y + m.f, r * Math.hypot(m.a, m.b), col]); } const gr = g.createRadialGradient(x, y, 0, x, y, r); gr.addColorStop(0, col); gr.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = gr; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill(); }
// Taşıma bandı: iki nokta arasında, çalışırken çizgiler akar.
export function belt(g, a, b, running, t, z = 8) {
  const p0 = iso(a[0], a[1], z), p1 = iso(b[0], b[1], z), p0b = iso(a[0], a[1], 0), p1b = iso(b[0], b[1], 0);
  g.strokeStyle = "#4a535b"; g.lineWidth = 2; g.beginPath(); g.moveTo(...p0b); g.lineTo(...p0); g.moveTo(...p1b); g.lineTo(...p1); g.stroke();
  g.strokeStyle = "#2b3137"; g.lineWidth = 6; g.beginPath(); g.moveTo(...p0); g.lineTo(...p1); g.stroke();
  g.strokeStyle = running ? "#e6b84a" : "#5a6168"; g.lineWidth = 2; g.setLineDash([5, 6]); g.lineDashOffset = running ? -t * 30 : 0;
  g.beginPath(); g.moveTo(...p0); g.lineTo(...p1); g.stroke(); g.setLineDash([]);
}
export function pile(g, cx, cy, r, h, col) {
  const [x, y] = iso(cx, cy), rx = r * HW * 1.2, ry = r * HH * 1.2;
  const gr = g.createLinearGradient(x - rx, y - h, x + rx, y); gr.addColorStop(0, shade(col, 0.25)); gr.addColorStop(1, shade(col, -0.35));
  g.fillStyle = gr; g.beginPath(); g.ellipse(x, y, rx, ry, 0, 0, Math.PI); g.quadraticCurveTo(x - rx * 0.2, y - h * 1.4, x - rx, y); g.fill();
  g.fillStyle = "rgba(0,0,0,.12)"; g.beginPath(); g.ellipse(x + rx * 0.2, y, rx * 0.8, ry * 0.7, 0, 0, Math.PI); g.fill();
}
export function person(g, gx, gy, t, seed, col = "#f2b631") {
  const [x, y] = iso(gx, gy), step = Math.sin(t * 9 + seed) * 1.2;
  g.fillStyle = "rgba(0,0,0,.25)"; g.beginPath(); g.ellipse(x, y, 3, 1.4, 0, 0, Math.PI * 2); g.fill();
  g.strokeStyle = "#2a3138"; g.lineWidth = 1.3; g.beginPath(); g.moveTo(x - 1, y - 4); g.lineTo(x - 1 - step, y); g.moveTo(x + 1, y - 4); g.lineTo(x + 1 + step, y); g.stroke();
  g.fillStyle = "#3d6fa0"; g.fillRect(x - 2, y - 9, 4, 5); g.fillStyle = "#e0ac85"; g.beginPath(); g.arc(x, y - 11, 1.8, 0, Math.PI * 2); g.fill();
  g.fillStyle = col; g.beginPath(); g.arc(x, y - 11.6, 2, Math.PI, 0); g.fill();
}
// Araçlar (yol üzerinde). dir: 1 = +gx, -1 = -gx, 2 = +gy, -2 = -gy. Gölge, tekerlek, kabin camı, far.
function vbox(g, ax, x0, y0, len, wid, h, col, z, opt) { if (ax) box(g, x0, y0, len, wid, h, col, { z, edge: "rgba(0,0,0,.35)", ...opt }); else box(g, x0, y0, wid, len, h, col, { z, edge: "rgba(0,0,0,.35)", ...opt }); }
function wheels(g, ax, x0, y0, len, wid, pos) { g.fillStyle = "#111417"; for (const p of pos) { const [wx, wy] = ax ? iso(x0 + len * p, y0 + wid, 2) : iso(x0 + wid, y0 + len * p, 2); g.beginPath(); g.ellipse(wx, wy, 2.6, 2.2, 0, 0, 7); g.fill(); } }
function vshadow(g, gx, gy, ax, len) { const [sx, sy] = iso(gx, gy); g.fillStyle = "rgba(0,0,0,.28)"; g.beginPath(); g.ellipse(sx + 3, sy + 2, ax ? len * HW * 0.75 : len * HW * 0.75, 5, ax ? 0.46 : -0.46, 0, 7); g.fill(); }
export function truck(g, gx, gy, dir, col = "#3f7fa8", load = "#c9d3dc", big = false, night = false) {
  const len = big ? 0.32 : 0.22, wid = 0.085, ax = Math.abs(dir) === 1, fwd = dir > 0, x0 = gx - (ax ? len / 2 : wid / 2), y0 = gy - (ax ? wid / 2 : len / 2);
  vshadow(g, gx, gy, ax, len);
  const cab = 0.07, bodyStart = fwd ? 0 : cab + 0.01, cabStart = fwd ? len - cab : 0;
  // kasa (nervürlü konteyner)
  const bx0 = ax ? x0 + bodyStart : x0, by0 = ax ? y0 : y0 + bodyStart; vbox(g, ax, bx0, by0, len - cab - 0.01, wid, big ? 11 : 9, load, 3);
  if (!SHADOW) { g.strokeStyle = "rgba(0,0,0,.18)"; g.lineWidth = 0.8; for (let k = 1; k < 6; k++) { const u = (len - cab - 0.01) * k / 6; const a = ax ? iso(bx0 + u, by0 + wid, 4) : iso(bx0 + wid, by0 + u, 4), b = ax ? iso(bx0 + u, by0 + wid, (big ? 11 : 9) + 3) : iso(bx0 + wid, by0 + u, (big ? 11 : 9) + 3); g.beginPath(); g.moveTo(...a); g.lineTo(...b); g.stroke(); } }
  // kabin + ön cam
  const cx0 = ax ? x0 + cabStart : x0, cy0 = ax ? y0 : y0 + cabStart; vbox(g, ax, cx0, cy0, cab, wid, 8, col, 3);
  if (!SHADOW) { const fx = fwd ? cab : 0; const win = ax ? [iso(cx0 + fx, cy0 + wid * 0.1, 7), iso(cx0 + fx, cy0 + wid * 0.9, 7), iso(cx0 + fx, cy0 + wid * 0.9, 10), iso(cx0 + fx, cy0 + wid * 0.1, 10)] : [iso(cx0 + wid * 0.1, cy0 + fx, 7), iso(cx0 + wid * 0.9, cy0 + fx, 7), iso(cx0 + wid * 0.9, cy0 + fx, 10), iso(cx0 + wid * 0.1, cy0 + fx, 10)];
    fillPoly(g, win, "#bfe2ff", "rgba(0,0,0,.3)"); wheels(g, ax, x0, y0, len, wid, big ? [0.12, 0.3, 0.85] : [0.18, 0.82]);
    if (night && fwd !== undefined) { const [hx, hy] = ax ? iso(fwd ? x0 + len + 0.02 : x0 - 0.02, gy, 4) : iso(gx, fwd ? y0 + len + 0.02 : y0 - 0.02, 4); glow(g, hx, hy, 16, "rgba(255,240,190,.45)"); } }
}
const CAR_COLORS = ["#e0473c", "#f2f4f6", "#2a2d31", "#3f7fa8", "#9aa5ae", "#e0a83a", "#4ca86a", "#6a4ca8"];
export function car(g, gx, gy, dir, seed = 0, night = false) {
  const col = CAR_COLORS[seed % CAR_COLORS.length], len = 0.12, wid = 0.07, ax = Math.abs(dir) === 1, x0 = gx - (ax ? len / 2 : wid / 2), y0 = gy - (ax ? wid / 2 : len / 2);
  if (!SHADOW) vshadow(g, gx, gy, ax, len * 0.8);
  vbox(g, ax, x0, y0, len, wid, 4, col, 2);
  const cx0 = ax ? x0 + len * 0.25 : x0 + wid * 0.08, cy0 = ax ? y0 + wid * 0.08 : y0 + len * 0.25;
  vbox(g, ax, cx0, cy0, len * 0.5, wid * 0.84, 3.5, "#9fcbe8", 6, { top: shade(col, 0.1), left: "#7fb3d6", right: "#5d8fb3" });
  if (!SHADOW) { wheels(g, ax, x0, y0, len, wid, [0.2, 0.8]); if (night) { const fwd = dir > 0; const [hx, hy] = ax ? iso(fwd ? x0 + len + 0.03 : x0 - 0.03, gy, 3) : iso(gx, fwd ? y0 + len + 0.03 : y0 - 0.03, 3); glow(g, hx, hy, 12, "rgba(255,240,190,.4)"); } }
}
export function tree(g, gx, gy, season, kind = 0) {
  const [x, y] = iso(gx, gy);
  if (SHADOW) { SHADOW.beginPath(); SHADOW.ellipse(x + 10, y + 4, 12, 5, 0.3, 0, 7); SHADOW.fill(); return; }
  g.fillStyle = "#5a4030"; g.fillRect(x - 1.5, y - 10, 3, 10);
  const base = season === "kis" ? "#dfe8ee" : season === "sonbahar" ? ["#c9782f", "#d99a3a", "#b5562c"][kind % 3] : season === "yaz" ? "#4f7a3a" : "#5f9a48";
  if (kind % 2 === 0) { for (const [dx, dy, r] of [[0, -18, 9], [-6, -13, 7], [6, -13, 7], [0, -24, 6]]) { const gr = g.createRadialGradient(x + dx - 3, y + dy - 3, 1, x + dx, y + dy, r); gr.addColorStop(0, shade(base, 0.25)); gr.addColorStop(1, shade(base, -0.2)); g.fillStyle = gr; g.beginPath(); g.arc(x + dx, y + dy, r, 0, 7); g.fill(); } }
  else { g.fillStyle = shade(base, -0.1); g.beginPath(); g.moveTo(x - 9, y - 6); g.lineTo(x, y - 34); g.lineTo(x + 9, y - 6); g.fill(); g.fillStyle = shade(base, -0.3); g.beginPath(); g.moveTo(x, y - 34); g.lineTo(x + 9, y - 6); g.lineTo(x + 2, y - 6); g.fill(); }
}
export function lamp(g, gx, gy, night) {
  const [x, y] = iso(gx, gy); if (SHADOW) return;
  g.strokeStyle = "#4a535b"; g.lineWidth = 1.6; g.beginPath(); g.moveTo(x, y); g.lineTo(x, y - 24); g.lineTo(x + 5, y - 26); g.stroke();
  g.fillStyle = night ? "#ffe2a0" : "#8b97a3"; g.fillRect(x + 3, y - 27, 5, 2); if (night) { glow(g, x + 5, y - 25, 10, "rgba(255,226,160,.8)"); const gr = g.createRadialGradient(x + 5, y, 2, x + 5, y, 26); gr.addColorStop(0, "rgba(255,215,140,.3)"); gr.addColorStop(1, "rgba(255,215,140,0)"); g.fillStyle = gr; g.beginPath(); g.ellipse(x + 5, y, 26, 13, 0, 0, 7); g.fill(); }
}
export function train(g, gx, gy, n, t) {
  for (let i = n - 1; i >= 0; i--) box(g, gx - i * 0.34, gy, 0.3, 0.1, i ? 10 : 13, i ? ["#8a5a3a", "#5c6b78", "#3f7fa8"][i % 3] : "#c94a3a", { z: 2 });
}
export function ship(g, gx, gy, t) {
  const bob = Math.sin(t * 1.3) * 1.5;
  g.save(); g.translate(0, bob);
  box(g, gx, gy, 1.2, 0.28, 10, "#24323d", { top: "#4b5a66" });
  const cols = ["#c94a3a", "#3f7fa8", "#e0a83a", "#4ca86a", "#9aa5ae"];
  for (let i = 0; i < 6; i++) box(g, gx + 0.08 + i * 0.15, gy + 0.04, 0.13, 0.2, 7, cols[i % 5], { z: 10 });
  box(g, gx + 1.0, gy + 0.05, 0.14, 0.18, 14, "#e9eef2", { z: 10 });
  g.restore();
}
export function plane(g, x, y, s = 1) {
  g.save(); g.translate(x, y); g.scale(s, s); g.fillStyle = "#e9eef2";
  g.beginPath(); g.ellipse(0, 0, 22, 4, -0.46, 0, Math.PI * 2); g.fill(); g.beginPath(); g.moveTo(-2, 0); g.lineTo(-14, 14); g.lineTo(-6, 14); g.lineTo(8, 1); g.fill();
  g.beginPath(); g.moveTo(12, -6); g.lineTo(16, -14); g.lineTo(19, -12); g.lineTo(17, -5); g.fill(); g.fillStyle = "#c94a3a"; g.fillRect(-20, 8, 6, 2); g.restore();
}
export function pumpjack(g, cx, cy, t, running) {
  const [x, y] = iso(cx, cy), a = running ? Math.sin(t * 2) * 0.35 : 0;
  g.strokeStyle = "#39434c"; g.lineWidth = 3; g.beginPath(); g.moveTo(x - 10, y); g.lineTo(x, y - 26); g.lineTo(x + 10, y); g.stroke();
  g.save(); g.translate(x, y - 26); g.rotate(a); g.fillStyle = "#e0a83a"; g.fillRect(-26, -3, 44, 6); g.fillStyle = "#c94a3a"; g.beginPath(); g.moveTo(18, -6); g.lineTo(26, 0); g.lineTo(18, 6); g.fill(); g.restore();
  g.strokeStyle = "#2b3137"; g.lineWidth = 1.2; g.beginPath(); g.moveTo(x + 20, y - 26 + Math.sin(a) * 20); g.lineTo(x + 20, y + 2); g.stroke();
  g.fillStyle = "#59616a"; g.fillRect(x - 30, y - 8, 10, 8);
}
// ---------- Binalar (arsa yerel koordinatı: 0..1) ----------
// ctx: { g, x, y (arsa köşesi), t, run (çalışıyor mu), night, lvl, m (makine sayısı), snow, seed }
const lit = c => c.night;
const SG = (c, x, y, w, d, h, col) => { if (c.sign && !SHADOW) facadeSign(c.g, x, y, w, d, h, c.sign, col); };
function snowCap(g, x, y, w, d, h, c) { if (c.snow) fillPoly(g, [iso(x, y, h + 0.5), iso(x + w, y, h + 0.5), iso(x + w, y + d, h + 0.5), iso(x, y + d, h + 0.5)], "rgba(245,248,252,.85)"); }
const H = (c, h) => h * (1 + 0.12 * (c.lvl - 1));
export const BUILD = {
  steel(c) { const { g, x, y } = c, h = H(c, 46);
    box(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, "#5f6d7a"); SG(c, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55); gable(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, 14, "#46525d"); windows(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, "L", 1, 6, lit(c));
    cyl(g, x + 0.72, y + 0.3, 0.12, h * 1.25, "#7c4a3a", { bands: "#4a2c22" });
    box(g, x + 0.62, y + 0.12, 0.2, 0.12, h * 0.9, "#6d5048");
    if (c.run) glow(g, ...iso(x + 0.66, y + 0.88, 6), 22, "rgba(255,140,40,.7)");
    chimney(g, x + 0.2, y + 0.18, h * 1.6, "#9aa5ae", c.run, c.t, 1); chimney(g, x + 0.36, y + 0.12, h * 1.4, "#9aa5ae", c.run, c.t, 2, "rgba(120,110,105,");
    belt(g, [x + 0.92, y + 0.75], [x + 0.72, y + 0.4], c.run, c.t, 16); pile(g, x + 0.92, y + 0.86, 0.07, 10, "#8b5a3c"); },
  plastic(c) { const { g, x, y } = c, h = H(c, 34);
    box(g, x + 0.1, y + 0.45, 0.5, 0.42, h, "#dfe6ec", { roof: 3 }); windows(g, x + 0.1, y + 0.45, 0.5, 0.42, h * 0.65, "L", 2, 5, lit(c), "#5b7fa0"); SG(c, x + 0.1, y + 0.45, 0.5, 0.42, h, "#47bdf5");
    for (const [i, dx] of [[0, 0.68], [1, 0.84]]) cyl(g, x + dx, y + 0.25 + i * 0.12, 0.07, h * 1.6, "#47bdf5", { dome: 4 });
    g.strokeStyle = "#8b97a3"; g.lineWidth = 2.5; g.beginPath(); g.moveTo(...iso(x + 0.6, y + 0.6, h * 0.8)); g.lineTo(...iso(x + 0.68, y + 0.3, h * 0.9)); g.lineTo(...iso(x + 0.84, y + 0.4, h * 0.9)); g.stroke();
    chimney(g, x + 0.22, y + 0.3, h * 1.5, "#c9d3dc", c.run, c.t, 3); },
  smelter(c) { const { g, x, y } = c, h = H(c, 30);
    box(g, x + 0.06, y + 0.38, 0.8, 0.48, h, "#a7b4bd"); SG(c, x + 0.06, y + 0.38, 0.8, 0.48, h); sawtooth(g, x + 0.06, y + 0.38, 0.8, 0.48, h, 3, "#8696a3");
    windows(g, x + 0.06, y + 0.38, 0.8, 0.48, h, "L", 1, 8, c.run, "#3a4a58");
    if (c.run) for (let i = 0; i < 4; i++) glow(g, ...iso(x + 0.15 + i * 0.18, y + 0.86, 8), 12, "rgba(255,170,60,.55)");
    chimney(g, x + 0.8, y + 0.15, h * 2, "#9aa5ae", c.run, c.t, 4); box(g, x + 0.1, y + 0.08, 0.3, 0.2, h * 0.5, "#7d8b97"); },
  glass(c) { const { g, x, y } = c, h = H(c, 28);
    box(g, x + 0.1, y + 0.45, 0.7, 0.42, h, "#7fa9bd"); windows(g, x + 0.1, y + 0.45, 0.7, 0.42, h, "L", 2, 7, true, "#bfe8f8");
    cyl(g, x + 0.3, y + 0.2, 0.13, h * 0.8, "#c97a4a", { dome: 10 }); if (c.run) glow(g, ...iso(x + 0.3, y + 0.33, h * 0.4), 18, "rgba(255,180,90,.6)");
    chimney(g, x + 0.62, y + 0.18, h * 1.9, "#9aa5ae", c.run, c.t, 5); },
  furniture(c) { const { g, x, y } = c, h = H(c, 26);
    box(g, x + 0.12, y + 0.42, 0.6, 0.45, h, "#a87a52"); SG(c, x + 0.12, y + 0.42, 0.6, 0.45, h); gable(g, x + 0.12, y + 0.42, 0.6, 0.45, h, 14, "#7a4a2c"); door(g, x + 0.12, y + 0.42, 0.6, 0.45, "L", 0.4, 0.22, h * 0.7);
    windows(g, x + 0.12, y + 0.42, 0.6, 0.45, h, "R", 1, 3, lit(c), "#3a3226");
    for (let i = 0; i < 3; i++) for (let j = 0; j < 2 - (i > 1); j++) box(g, x + 0.12 + i * 0.12, y + 0.12 + j * 0.06, 0.1, 0.05, 5, "#b9824a", { z: j * 5, top: "#d9a066" });
    chimney(g, x + 0.62, y + 0.46, h * 1.6, "#8a8f94", c.run, c.t, 6); },
  textile(c) { const { g, x, y } = c, h = H(c, 24);
    box(g, x + 0.06, y + 0.32, 0.82, 0.56, h, "#c96f8c"); sawtooth(g, x + 0.06, y + 0.32, 0.82, 0.56, h, 4, "#a5576f", "#f0d6e0");
    windows(g, x + 0.06, y + 0.32, 0.82, 0.56, h, "L", 1, 9, lit(c), "#5a2f3d"); door(g, x + 0.06, y + 0.32, 0.82, 0.56, "R", 0.3, 0.35, h * 0.75);
    for (let i = 0; i < 3; i++) cyl(g, x + 0.2 + i * 0.1, y + 0.16, 0.04, 6, "#f3efe4"); },
  chem(c) { const { g, x, y } = c, h = H(c, 22);
    sphereTank(g, x + 0.25, y + 0.25, 0.12, "#dfe6ec"); sphereTank(g, x + 0.55, y + 0.2, 0.1, "#dfe6ec");
    box(g, x + 0.1, y + 0.55, 0.45, 0.32, h, "#8ae66e", { left: "#6fc455" }); windows(g, x + 0.1, y + 0.55, 0.45, 0.32, h, "L", 1, 4, lit(c), "#2d5a24");
    cyl(g, x + 0.75, y + 0.55, 0.06, h * 2.2, "#c9d3dc", { bands: "#8b97a3" }); cyl(g, x + 0.85, y + 0.75, 0.05, h * 1.6, "#c9d3dc");
    const [fx, fy] = iso(x + 0.75, y + 0.55, h * 2.2 + 4); if (c.run) { glow(g, fx, fy - 4, 12 + Math.sin(c.t * 9) * 2, "rgba(255,150,50,.9)"); }
    g.strokeStyle = "#8b97a3"; g.lineWidth = 2; g.beginPath(); g.moveTo(...iso(x + 0.25, y + 0.25, 6)); g.lineTo(...iso(x + 0.55, y + 0.62, 6)); g.lineTo(...iso(x + 0.75, y + 0.55, 6)); g.stroke(); },
  electronics(c) { const { g, x, y } = c, h = H(c, 36);
    box(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "#eef2f6", { right: "#c3ccd4", roof: 5 }); windows(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "L", 3, 6, true, "#5fb0e8"); windows(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "R", 3, 5, true, "#4a92c4");
    box(g, x + 0.3, y + 0.4, 0.3, 0.3, 6, "#9aa5ae", { z: h }); fillPoly(g, [iso(x + 0.14, y + 0.86, h * 0.92), iso(x + 0.76, y + 0.86, h * 0.92), iso(x + 0.76, y + 0.86, h), iso(x + 0.14, y + 0.86, h)], "#2fa36b"); },
  battery(c) { const { g, x, y } = c, h = H(c, 30);
    box(g, x + 0.1, y + 0.36, 0.72, 0.5, h, "#e6ecef", { right: "#c0cad1", roof: 2 }); SG(c, x + 0.1, y + 0.36, 0.72, 0.5, h, "#4cd893"); fillPoly(g, [iso(x + 0.1, y + 0.86, 4), iso(x + 0.82, y + 0.86, 4), iso(x + 0.82, y + 0.86, 9), iso(x + 0.1, y + 0.86, 9)], "#4cd893");
    windows(g, x + 0.1, y + 0.36, 0.72, 0.5, h, "L", 2, 7, lit(c), "#3a5a4a");
    for (let i = 0; i < 3; i++) box(g, x + 0.15 + i * 0.2, y + 0.08, 0.14, 0.18, 12, "#59616a", { top: "#4cd893" }); },
  auto(c) { const { g, x, y } = c, h = H(c, 32);
    box(g, x + 0.04, y + 0.36, 0.86, 0.52, h, "#dfe6ec", { right: "#b7c2cb" }); SG(c, x + 0.04, y + 0.36, 0.86, 0.52, h, "#e0473c"); sawtooth(g, x + 0.04, y + 0.36, 0.86, 0.52, h, 4, "#c3ccd4");
    windows(g, x + 0.04, y + 0.36, 0.86, 0.52, h, "L", 1, 9, lit(c), "#5b7fa0"); door(g, x + 0.04, y + 0.36, 0.86, 0.52, "R", 0.2, 0.25, h * 0.7);
    const cols = ["#e0473c", "#3f7fa8", "#e9eef2", "#2a2d31", "#ffb02e"];
    for (let i = 0; i < 8; i++) box(g, x + 0.08 + (i % 4) * 0.11, y + 0.08 + Math.floor(i / 4) * 0.12, 0.08, 0.05, 4, cols[(i + c.seed) % 5], { z: 1 }); },
  machine(c) { const { g, x, y } = c, h = H(c, 34);
    box(g, x + 0.08, y + 0.32, 0.78, 0.55, h, "#ffb02e", { left: "#e39a22", right: "#b77a1a" }); SG(c, x + 0.08, y + 0.32, 0.78, 0.55, h * 0.6, "#1d1f22"); gable(g, x + 0.08, y + 0.32, 0.78, 0.55, h, 12, "#6d7b88");
    windows(g, x + 0.08, y + 0.32, 0.78, 0.55, h, "L", 1, 7, lit(c), "#4a3616"); door(g, x + 0.08, y + 0.32, 0.78, 0.55, "L", 0.62, 0.25, h * 0.75);
    g.strokeStyle = "#39434c"; g.lineWidth = 2; const a = iso(x + 0.1, y + 0.15, 30), b = iso(x + 0.6, y + 0.15, 30); g.beginPath(); g.moveTo(...iso(x + 0.1, y + 0.15)); g.lineTo(...a); g.lineTo(...b); g.lineTo(...iso(x + 0.6, y + 0.15)); g.stroke();
    const k = 0.15 + (Math.sin(c.t * 0.6) * 0.5 + 0.5) * 0.4 * (c.run ? 1 : 0); box(g, x + k, y + 0.12, 0.08, 0.06, 6, "#6d7b88", { z: 12 }); },
  food(c) { const { g, x, y } = c, h = H(c, 24);
    box(g, x + 0.1, y + 0.45, 0.6, 0.42, h, "#f2e7cc", { right: "#cdbf9e", roof: 4 }); windows(g, x + 0.1, y + 0.45, 0.6, 0.42, h, "L", 1, 6, lit(c), "#6b5a3a");
    fillPoly(g, [iso(x + 0.1, y + 0.87, h * 0.75), iso(x + 0.7, y + 0.87, h * 0.75), iso(x + 0.7, y + 0.87, h * 0.92), iso(x + 0.1, y + 0.87, h * 0.92)], "#e8553f");
    for (let i = 0; i < 3; i++) cyl(g, x + 0.22 + i * 0.16, y + 0.2, 0.065, h * 1.7, "#dfe6ec", { dome: 5, bands: "#b9c3cb" });
    chimney(g, x + 0.78, y + 0.55, h * 1.4, "#c9d3dc", c.run, c.t, 7, "rgba(240,240,240,"); },
  refinery(c) { const { g, x, y } = c, h = H(c, 30);
    for (const [i, [dx, dy, r, hh]] of [[0.25, 0.25, 0.07, 2.4], [0.42, 0.18, 0.05, 2.0], [0.58, 0.3, 0.06, 1.8]].entries()) cyl(g, x + dx, y + dy, r, h * hh, "#c9d3dc", { bands: "#8b97a3", bandAt: [0.25, 0.5, 0.75] });
    for (let i = 0; i < 3; i++) cyl(g, x + 0.18 + i * 0.2, y + 0.72, 0.09, h * 0.6, "#e9eef2", { top: "#cfd6dd" });
    cyl(g, x + 0.86, y + 0.2, 0.025, h * 2.6, "#8b97a3"); const [fx, fy] = iso(x + 0.86, y + 0.2, h * 2.6 + 3);
    if (c.run) { glow(g, fx, fy - 6, 16 + Math.sin(c.t * 11) * 3, "rgba(255,140,40,.95)"); g.fillStyle = "#ffd25a"; g.beginPath(); g.ellipse(fx, fy - 6, 3, 7 + Math.sin(c.t * 13) * 2, 0, 0, Math.PI * 2); g.fill(); }
    g.strokeStyle = "#e0a83a"; g.lineWidth = 2; g.beginPath(); g.moveTo(...iso(x + 0.25, y + 0.25, 10)); g.lineTo(...iso(x + 0.58, y + 0.72, 10)); g.stroke(); },
  paper(c) { const { g, x, y } = c, h = H(c, 28);
    box(g, x + 0.1, y + 0.4, 0.62, 0.46, h, "#dfe1dc"); SG(c, x + 0.1, y + 0.4, 0.62, 0.46, h); gable(g, x + 0.1, y + 0.4, 0.62, 0.46, h, 12, "#7d8b97"); windows(g, x + 0.1, y + 0.4, 0.62, 0.46, h, "L", 1, 6, lit(c), "#4a5560");
    for (let i = 0; i < 4; i++) for (let j = 0; j < 2; j++) cyl(g, x + 0.12 + i * 0.09, y + 0.14 + j * 0.1, 0.035, 6, "#a36f3d", { z: 0 });
    chimney(g, x + 0.78, y + 0.3, h * 1.8, "#9aa5ae", c.run, c.t, 8, "rgba(235,235,230,"); },
  cement(c) { const { g, x, y } = c, h = H(c, 30);
    for (let i = 0; i < 2; i++) cyl(g, x + 0.62 + i * 0.16, y + 0.22 + i * 0.1, 0.08, h * 1.6, "#b9b5ab", { dome: 4 });
    const a = iso(x + 0.1, y + 0.7, 14), b = iso(x + 0.55, y + 0.45, 24); g.strokeStyle = "#8a8378"; g.lineWidth = 12; g.lineCap = "round"; g.beginPath(); g.moveTo(...a); g.lineTo(...b); g.stroke(); g.lineCap = "butt";
    g.strokeStyle = "#6d675e"; g.lineWidth = 2; for (let i = 0; i < 4; i++) { const p = [a[0] + (b[0] - a[0]) * i / 4, a[1] + (b[1] - a[1]) * i / 4]; g.beginPath(); g.moveTo(p[0] - 3, p[1] - 6); g.lineTo(p[0] + 3, p[1] + 6); g.stroke(); }
    box(g, x + 0.45, y + 0.6, 0.35, 0.28, h * 0.7, "#9b968a"); pile(g, x + 0.18, y + 0.3, 0.1, 14, "#cfc9b6");
    chimney(g, x + 0.3, y + 0.55, h * 1.7, "#9aa5ae", c.run, c.t, 9, "rgba(200,196,186,"); },
  mine(c) { const { g, x, y } = c, h = H(c, 50);
    pile(g, x + 0.7, y + 0.72, 0.16, 22, c.oreColor || "#8b5a3c"); pile(g, x + 0.85, y + 0.45, 0.1, 14, c.oreColor || "#8b5a3c");
    box(g, x + 0.1, y + 0.55, 0.35, 0.3, 20, "#6d5a48"); gable(g, x + 0.1, y + 0.55, 0.35, 0.3, 20, 10, "#4a3a2c");
    g.strokeStyle = "#39434c"; g.lineWidth = 2.5; const base = [[0.2, 0.2], [0.4, 0.2], [0.4, 0.4], [0.2, 0.4]].map(([a, b]) => iso(x + a, y + b)), top = iso(x + 0.3, y + 0.3, h * 1.3);
    for (const p of base) { g.beginPath(); g.moveTo(...p); g.lineTo(...top); g.stroke(); }
    g.save(); g.translate(...top); g.rotate(c.run ? c.t * 2 : 0); g.strokeStyle = "#c94a3a"; g.lineWidth = 2; g.beginPath(); g.arc(0, 0, 9, 0, Math.PI * 2); g.moveTo(-9, 0); g.lineTo(9, 0); g.moveTo(0, -9); g.lineTo(0, 9); g.stroke(); g.restore();
    belt(g, [x + 0.42, y + 0.5], [x + 0.7, y + 0.7], c.run, c.t, 18); },
  well(c) { const { g, x, y } = c;
    pumpjack(g, x + 0.3, y + 0.35, c.t, c.run); pumpjack(g, x + 0.65, y + 0.2, c.t + 1.3, c.run);
    for (let i = 0; i < 2; i++) cyl(g, x + 0.3 + i * 0.25, y + 0.75, 0.09, 26, "#e9eef2", { bands: "#c94a3a", bandAt: [0.8] });
    g.strokeStyle = "#e0a83a"; g.lineWidth = 2; g.beginPath(); g.moveTo(...iso(x + 0.3, y + 0.35, 2)); g.lineTo(...iso(x + 0.3, y + 0.75, 2)); g.lineTo(...iso(x + 0.55, y + 0.75, 2)); g.stroke(); },
  power(c) { const { g, x, y } = c, h = H(c, 40);
    if (c.solar) { for (let i = 0; i < 4; i++) for (let j = 0; j < 3; j++) fillPoly(g, [iso(x + 0.08 + i * 0.21, y + 0.1 + j * 0.28, 6), iso(x + 0.26 + i * 0.21, y + 0.1 + j * 0.28, 6), iso(x + 0.26 + i * 0.21, y + 0.32 + j * 0.28, 14), iso(x + 0.08 + i * 0.21, y + 0.32 + j * 0.28, 14)], "#2b4a7a", "#8fb2ff"); return; }
    for (const [dx, dy] of [[0.28, 0.28], [0.58, 0.2]]) {
      const [bx, by] = iso(x + dx, y + dy), rx = 22, top = by - h * 1.4, gr = g.createLinearGradient(bx - rx, 0, bx + rx, 0); gr.addColorStop(0, "#e9eef2"); gr.addColorStop(1, "#9aa5ae");
      g.fillStyle = gr; g.beginPath(); g.moveTo(bx - rx, by); g.quadraticCurveTo(bx - rx * 0.55, by - h * 0.8, bx - rx * 0.72, top); g.lineTo(bx + rx * 0.72, top); g.quadraticCurveTo(bx + rx * 0.55, by - h * 0.8, bx + rx, by); g.closePath(); g.fill();
      g.fillStyle = "#cfd6dd"; g.beginPath(); g.ellipse(bx, top, rx * 0.72, 5, 0, 0, Math.PI * 2); g.fill();
      if (c.run) for (let i = 0; i < 6; i++) { const ph = (c.t * 0.25 + i / 6 + dx) % 1; g.fillStyle = `rgba(255,255,255,${0.5 * (1 - ph)})`; g.beginPath(); g.arc(bx + ph * 18, top - ph * 50, 10 + ph * 16, 0, Math.PI * 2); g.fill(); }
    }
    box(g, x + 0.45, y + 0.55, 0.42, 0.32, h * 0.55, "#5f6d7a"); windows(g, x + 0.45, y + 0.55, 0.42, 0.32, h * 0.55, "L", 1, 4, lit(c));
    for (let i = 0; i < 3; i++) { const [px, py] = iso(x + 0.1 + i * 0.1, y + 0.8 + i * 0.03); g.strokeStyle = "#8b97a3"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(px - 5, py); g.lineTo(px, py - 30); g.lineTo(px + 5, py); g.moveTo(px - 8, py - 22); g.lineTo(px + 8, py - 22); g.stroke(); } },
  farm(c) { const { g, x, y } = c;
    const crop = c.crop || "#d9b44a"; for (let i = 0; i < 5; i++) fillPoly(g, [iso(x + 0.05, y + 0.08 + i * 0.12), iso(x + 0.6, y + 0.08 + i * 0.12), iso(x + 0.6, y + 0.14 + i * 0.12), iso(x + 0.05, y + 0.14 + i * 0.12)], c.snow ? "#e9eef2" : i % 2 ? crop : shade(crop, -0.15));
    box(g, x + 0.65, y + 0.5, 0.25, 0.3, 18, "#b5452f"); gable(g, x + 0.65, y + 0.5, 0.25, 0.3, 18, 10, "#7a2f22"); cyl(g, x + 0.8, y + 0.25, 0.07, 36, "#c9d3dc", { dome: 6 });
    const tx = x + 0.08 + ((c.t * 0.04 + c.seed * 0.1) % 1) * 0.5; if (c.run) { box(g, tx, y + 0.66, 0.08, 0.05, 6, "#4ca86a", { z: 1 }); } },
  // Depolar
  shed(c) { const { g, x, y } = c, h = H(c, 24);
    box(g, x + 0.1, y + 0.3, 0.75, 0.55, h, "#8fa3b5", { left: "#7d93a6", right: "#5e7385" }); SG(c, x + 0.1, y + 0.3, 0.75, 0.55, h * 1.05); gable(g, x + 0.1, y + 0.3, 0.75, 0.55, h, 14, "#536677");
    door(g, x + 0.1, y + 0.3, 0.75, 0.55, "L", 0.15, 0.25, h * 0.75); door(g, x + 0.1, y + 0.3, 0.75, 0.55, "L", 0.55, 0.25, h * 0.75); snowCap(g, x + 0.1, y + 0.3, 0.75, 0.55, h, c);
    for (let i = 0; i < 3; i++) box(g, x + 0.12 + i * 0.1, y + 0.1, 0.08, 0.08, 6, "#c98a2b", { top: "#e0a83a" }); },
  metal(c) { const { g, x, y } = c, h = H(c, 28);
    box(g, x + 0.08, y + 0.3, 0.8, 0.56, h, "#6d7b88"); gable(g, x + 0.08, y + 0.3, 0.8, 0.56, h, 10, "#4a5763"); door(g, x + 0.08, y + 0.3, 0.8, 0.56, "R", 0.25, 0.5, h * 0.8, "#2b3137");
    for (let i = 0; i < 4; i++) cyl(g, x + 0.15 + i * 0.12, y + 0.16, 0.05, 8, "#9fb3c6", { z: 0 }); },
  chemstore(c) { const { g, x, y } = c, h = H(c, 22);
    box(g, x + 0.06, y + 0.06, 0.88, 0.88, 4, "#9b968a"); box(g, x + 0.14, y + 0.3, 0.72, 0.5, h, "#dfe6ec", { z: 4 });
    for (let i = 0; i < 6; i++) fillPoly(g, [iso(x + 0.14 + i * 0.12, y + 0.8, 4), iso(x + 0.2 + i * 0.12, y + 0.8, 4), iso(x + 0.2 + i * 0.12, y + 0.8, 10), iso(x + 0.14 + i * 0.12, y + 0.8, 10)], i % 2 ? "#ffd23c" : "#1d1f22");
    box(g, x + 0.3, y + 0.4, 0.2, 0.2, 5, "#9aa5ae", { z: h + 4 }); },
  cold(c) { const { g, x, y } = c, h = H(c, 26);
    box(g, x + 0.1, y + 0.3, 0.75, 0.55, h, "#f4f7fa", { right: "#c9d3dc", roof: 6 }); for (let i = 0; i < 3; i++) { const [fx, fy] = iso(x + 0.25 + i * 0.2, y + 0.55, h + 3); g.fillStyle = "#8b97a3"; g.beginPath(); g.ellipse(fx, fy, 10, 5, 0, 0, Math.PI * 2); g.fill(); g.save(); g.translate(fx, fy); g.scale(1, 0.5); g.rotate(c.t * 6); g.strokeStyle = "#e9eef2"; g.lineWidth = 2; g.beginPath(); g.moveTo(-7, 0); g.lineTo(7, 0); g.moveTo(0, -7); g.lineTo(0, 7); g.stroke(); g.restore(); }
    door(g, x + 0.1, y + 0.3, 0.75, 0.55, "L", 0.35, 0.3, h * 0.7, "#5fb0e8"); },
  tank(c) { const { g, x, y } = c, h = H(c, 30);
    for (const [dx, dy, r] of [[0.28, 0.28, 0.16], [0.68, 0.3, 0.13], [0.45, 0.68, 0.14]]) cyl(g, x + dx, y + dy, r, h, "#e9eef2", { bands: "#c94a3a", bandAt: [0.8], top: "#cfd6dd" });
    g.strokeStyle = "#e0a83a"; g.lineWidth = 2; g.beginPath(); g.moveTo(...iso(x + 0.28, y + 0.28, 3)); g.lineTo(...iso(x + 0.45, y + 0.68, 3)); g.lineTo(...iso(x + 0.68, y + 0.3, 3)); g.stroke(); },
  logistics(c) { const { g, x, y } = c, h = H(c, 32);
    box(g, x + 0.04, y + 0.2, 0.9, 0.68, h, "#9fb3c6", { left: "#8fa3b5", right: "#6c8195", roof: 7 }); for (let i = 0; i < 6; i++) door(g, x + 0.04, y + 0.2, 0.9, 0.68, "L", 0.04 + i * 0.16, 0.12, h * 0.55, "#39434c");
    fillPoly(g, [iso(x + 0.04, y + 0.88, h * 0.82), iso(x + 0.94, y + 0.88, h * 0.82), iso(x + 0.94, y + 0.88, h * 0.95), iso(x + 0.04, y + 0.88, h * 0.95)], "#f2b631"); snowCap(g, x + 0.04, y + 0.2, 0.9, 0.68, h, c);
    for (let i = 0; i < 3; i++) truck(g, x + 0.2 + i * 0.25, y + 0.96, 2, "#3f7fa8", "#c9d3dc"); },
};
export const LOOK_OF_RECIPE = { e_gunes: "solar" };
