// İzometrik 2D çizim kütüphanesi: kutu, çatı, silindir, baca dumanı, bant, pencereler ve 25 bina/tesis çizimi.
// Koordinat: ızgara birimi (arsa = 1×1). iso(gx, gy, z) → ekran. Işık sol-üstten: üst açık, sol orta, sağ koyu.
import { shade } from "./icons.js";
export const HW = 80, HH = 40;
export const iso = (gx, gy, z = 0) => [(gx - gy) * HW, (gx + gy) * HH - z];
const P = (g, pts) => { g.beginPath(); pts.forEach(([x, y], i) => (i ? g.lineTo(x, y) : g.moveTo(x, y))); g.closePath(); };
export function fillPoly(g, pts, color, stroke) { P(g, pts); g.fillStyle = color; g.fill(); if (stroke) { g.strokeStyle = stroke; g.lineWidth = 0.8; g.stroke(); } }
export function box(g, x, y, w, d, h, col, opt = {}) {
  const top = opt.top || shade(col, 0.22), left = opt.left || col, right = opt.right || shade(col, -0.25), z = opt.z || 0, edge = opt.edge ?? "rgba(0,0,0,.25)";
  fillPoly(g, [iso(x + w, y, z), iso(x + w, y + d, z), iso(x + w, y + d, z + h), iso(x + w, y, z + h)], right, edge);
  fillPoly(g, [iso(x, y + d, z), iso(x + w, y + d, z), iso(x + w, y + d, z + h), iso(x, y + d, z + h)], left, edge);
  if (!opt.noTop) fillPoly(g, [iso(x, y, z + h), iso(x + w, y, z + h), iso(x + w, y + d, z + h), iso(x, y + d, z + h)], top, edge);
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
  for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) {
    const u0 = (c + 0.2) / cols, u1 = (c + 0.8) / cols, v0 = z + h * (r + 0.25) / rows, v1 = z + h * (r + 0.7) / rows;
    const pts = face === "L" ? [iso(x + w * u0, y + d, v0), iso(x + w * u1, y + d, v0), iso(x + w * u1, y + d, v1), iso(x + w * u0, y + d, v1)]
      : [iso(x + w, y + d * u0, v0), iso(x + w, y + d * u1, v0), iso(x + w, y + d * u1, v1), iso(x + w, y + d * u0, v1)];
    fillPoly(g, pts, lit && ((r * 7 + c * 3) % 5) ? "#ffd98a" : col);
  }
}
export function door(g, x, y, w, d, face, u, wu, hh, col = "#39434c") {
  const pts = face === "L" ? [iso(x + w * u, y + d, 0), iso(x + w * (u + wu), y + d, 0), iso(x + w * (u + wu), y + d, hh), iso(x + w * u, y + d, hh)]
    : [iso(x + w, y + d * u, 0), iso(x + w, y + d * (u + wu), 0), iso(x + w, y + d * (u + wu), hh), iso(x + w, y + d * u, hh)];
  fillPoly(g, pts, col, "rgba(0,0,0,.3)");
  g.strokeStyle = "rgba(255,255,255,.12)"; g.lineWidth = 1; for (let i = 1; i < 5; i++) { const t = i / 5; const a = face === "L" ? iso(x + w * u, y + d, hh * t) : iso(x + w, y + d * u, hh * t), b = face === "L" ? iso(x + w * (u + wu), y + d, hh * t) : iso(x + w, y + d * (u + wu), hh * t); g.beginPath(); g.moveTo(...a); g.lineTo(...b); g.stroke(); }
}
export function cyl(g, cx, cy, r, h, col, opt = {}) {
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
export function glow(g, x, y, r, col) { const gr = g.createRadialGradient(x, y, 0, x, y, r); gr.addColorStop(0, col); gr.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = gr; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill(); }
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
// Araçlar (yol üzerinde). dir: 1 = +gx, -1 = -gx, 2 = +gy, -2 = -gy.
export function truck(g, gx, gy, dir, col = "#3f7fa8", load = "#c9d3dc", big = false) {
  const w = big ? 0.3 : 0.2, d = 0.09, ax = Math.abs(dir) === 1;
  const x0 = gx - (ax ? w / 2 : d / 2), y0 = gy - (ax ? d / 2 : w / 2);
  g.fillStyle = "rgba(0,0,0,.25)"; const [sx, sy] = iso(gx, gy); g.beginPath(); g.ellipse(sx, sy + 2, 14, 5, 0, 0, Math.PI * 2); g.fill();
  if (ax) { const cab = dir > 0 ? x0 + w * 0.72 : x0; box(g, dir > 0 ? x0 : x0 + w * 0.28, y0, w * 0.72, d, 9, load, { z: 2 }); box(g, cab, y0, w * 0.28, d, 8, col, { z: 2 }); }
  else { const cab = dir > 0 ? y0 + w * 0.72 : y0; box(g, x0, dir > 0 ? y0 : y0 + w * 0.28, d, w * 0.72, 9, load, { z: 2 }); box(g, x0, cab, d, w * 0.28, 8, col, { z: 2 }); }
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
function snowCap(g, x, y, w, d, h, c) { if (c.snow) fillPoly(g, [iso(x, y, h + 0.5), iso(x + w, y, h + 0.5), iso(x + w, y + d, h + 0.5), iso(x, y + d, h + 0.5)], "rgba(245,248,252,.85)"); }
const H = (c, h) => h * (1 + 0.12 * (c.lvl - 1));
export const BUILD = {
  steel(c) { const { g, x, y } = c, h = H(c, 46);
    box(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, "#5f6d7a"); gable(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, 14, "#46525d"); windows(g, x + 0.08, y + 0.5, 0.55, 0.38, h * 0.55, "L", 1, 6, lit(c));
    cyl(g, x + 0.72, y + 0.3, 0.12, h * 1.25, "#7c4a3a", { bands: "#4a2c22" });
    box(g, x + 0.62, y + 0.12, 0.2, 0.12, h * 0.9, "#6d5048");
    if (c.run) glow(g, ...iso(x + 0.66, y + 0.88, 6), 22, "rgba(255,140,40,.7)");
    chimney(g, x + 0.2, y + 0.18, h * 1.6, "#9aa5ae", c.run, c.t, 1); chimney(g, x + 0.36, y + 0.12, h * 1.4, "#9aa5ae", c.run, c.t, 2, "rgba(120,110,105,");
    belt(g, [x + 0.92, y + 0.75], [x + 0.72, y + 0.4], c.run, c.t, 16); pile(g, x + 0.92, y + 0.86, 0.07, 10, "#8b5a3c"); },
  plastic(c) { const { g, x, y } = c, h = H(c, 34);
    box(g, x + 0.1, y + 0.45, 0.5, 0.42, h, "#dfe6ec"); windows(g, x + 0.1, y + 0.45, 0.5, 0.42, h, "L", 2, 5, lit(c), "#5b7fa0");
    for (const [i, dx] of [[0, 0.68], [1, 0.84]]) cyl(g, x + dx, y + 0.25 + i * 0.12, 0.07, h * 1.6, "#47bdf5", { dome: 4 });
    g.strokeStyle = "#8b97a3"; g.lineWidth = 2.5; g.beginPath(); g.moveTo(...iso(x + 0.6, y + 0.6, h * 0.8)); g.lineTo(...iso(x + 0.68, y + 0.3, h * 0.9)); g.lineTo(...iso(x + 0.84, y + 0.4, h * 0.9)); g.stroke();
    chimney(g, x + 0.22, y + 0.3, h * 1.5, "#c9d3dc", c.run, c.t, 3); },
  smelter(c) { const { g, x, y } = c, h = H(c, 30);
    box(g, x + 0.06, y + 0.38, 0.8, 0.48, h, "#a7b4bd"); sawtooth(g, x + 0.06, y + 0.38, 0.8, 0.48, h, 3, "#8696a3");
    windows(g, x + 0.06, y + 0.38, 0.8, 0.48, h, "L", 1, 8, c.run, "#3a4a58");
    if (c.run) for (let i = 0; i < 4; i++) glow(g, ...iso(x + 0.15 + i * 0.18, y + 0.86, 8), 12, "rgba(255,170,60,.55)");
    chimney(g, x + 0.8, y + 0.15, h * 2, "#9aa5ae", c.run, c.t, 4); box(g, x + 0.1, y + 0.08, 0.3, 0.2, h * 0.5, "#7d8b97"); },
  glass(c) { const { g, x, y } = c, h = H(c, 28);
    box(g, x + 0.1, y + 0.45, 0.7, 0.42, h, "#7fa9bd"); windows(g, x + 0.1, y + 0.45, 0.7, 0.42, h, "L", 2, 7, true, "#bfe8f8");
    cyl(g, x + 0.3, y + 0.2, 0.13, h * 0.8, "#c97a4a", { dome: 10 }); if (c.run) glow(g, ...iso(x + 0.3, y + 0.33, h * 0.4), 18, "rgba(255,180,90,.6)");
    chimney(g, x + 0.62, y + 0.18, h * 1.9, "#9aa5ae", c.run, c.t, 5); },
  furniture(c) { const { g, x, y } = c, h = H(c, 26);
    box(g, x + 0.12, y + 0.42, 0.6, 0.45, h, "#a87a52"); gable(g, x + 0.12, y + 0.42, 0.6, 0.45, h, 14, "#7a4a2c"); door(g, x + 0.12, y + 0.42, 0.6, 0.45, "L", 0.4, 0.22, h * 0.7);
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
    box(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "#eef2f6", { right: "#c3ccd4" }); windows(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "L", 3, 6, true, "#5fb0e8"); windows(g, x + 0.14, y + 0.3, 0.62, 0.56, h, "R", 3, 5, true, "#4a92c4");
    box(g, x + 0.3, y + 0.4, 0.3, 0.3, 6, "#9aa5ae", { z: h }); fillPoly(g, [iso(x + 0.14, y + 0.86, h * 0.92), iso(x + 0.76, y + 0.86, h * 0.92), iso(x + 0.76, y + 0.86, h), iso(x + 0.14, y + 0.86, h)], "#2fa36b"); },
  battery(c) { const { g, x, y } = c, h = H(c, 30);
    box(g, x + 0.1, y + 0.36, 0.72, 0.5, h, "#e6ecef", { right: "#c0cad1" }); fillPoly(g, [iso(x + 0.1, y + 0.86, 4), iso(x + 0.82, y + 0.86, 4), iso(x + 0.82, y + 0.86, 9), iso(x + 0.1, y + 0.86, 9)], "#4cd893");
    windows(g, x + 0.1, y + 0.36, 0.72, 0.5, h, "L", 2, 7, lit(c), "#3a5a4a");
    for (let i = 0; i < 3; i++) box(g, x + 0.15 + i * 0.2, y + 0.08, 0.14, 0.18, 12, "#59616a", { top: "#4cd893" }); },
  auto(c) { const { g, x, y } = c, h = H(c, 32);
    box(g, x + 0.04, y + 0.36, 0.86, 0.52, h, "#dfe6ec", { right: "#b7c2cb" }); sawtooth(g, x + 0.04, y + 0.36, 0.86, 0.52, h, 4, "#c3ccd4");
    windows(g, x + 0.04, y + 0.36, 0.86, 0.52, h, "L", 1, 9, lit(c), "#5b7fa0"); door(g, x + 0.04, y + 0.36, 0.86, 0.52, "R", 0.2, 0.25, h * 0.7);
    const cols = ["#e0473c", "#3f7fa8", "#e9eef2", "#2a2d31", "#ffb02e"];
    for (let i = 0; i < 8; i++) box(g, x + 0.08 + (i % 4) * 0.11, y + 0.08 + Math.floor(i / 4) * 0.12, 0.08, 0.05, 4, cols[(i + c.seed) % 5], { z: 1 }); },
  machine(c) { const { g, x, y } = c, h = H(c, 34);
    box(g, x + 0.08, y + 0.32, 0.78, 0.55, h, "#ffb02e", { left: "#e39a22", right: "#b77a1a" }); gable(g, x + 0.08, y + 0.32, 0.78, 0.55, h, 12, "#6d7b88");
    windows(g, x + 0.08, y + 0.32, 0.78, 0.55, h, "L", 1, 7, lit(c), "#4a3616"); door(g, x + 0.08, y + 0.32, 0.78, 0.55, "L", 0.62, 0.25, h * 0.75);
    g.strokeStyle = "#39434c"; g.lineWidth = 2; const a = iso(x + 0.1, y + 0.15, 30), b = iso(x + 0.6, y + 0.15, 30); g.beginPath(); g.moveTo(...iso(x + 0.1, y + 0.15)); g.lineTo(...a); g.lineTo(...b); g.lineTo(...iso(x + 0.6, y + 0.15)); g.stroke();
    const k = 0.15 + (Math.sin(c.t * 0.6) * 0.5 + 0.5) * 0.4 * (c.run ? 1 : 0); box(g, x + k, y + 0.12, 0.08, 0.06, 6, "#6d7b88", { z: 12 }); },
  food(c) { const { g, x, y } = c, h = H(c, 24);
    box(g, x + 0.1, y + 0.45, 0.6, 0.42, h, "#f2e7cc", { right: "#cdbf9e" }); windows(g, x + 0.1, y + 0.45, 0.6, 0.42, h, "L", 1, 6, lit(c), "#6b5a3a");
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
    box(g, x + 0.1, y + 0.4, 0.62, 0.46, h, "#dfe1dc"); gable(g, x + 0.1, y + 0.4, 0.62, 0.46, h, 12, "#7d8b97"); windows(g, x + 0.1, y + 0.4, 0.62, 0.46, h, "L", 1, 6, lit(c), "#4a5560");
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
    box(g, x + 0.1, y + 0.3, 0.75, 0.55, h, "#8fa3b5", { left: "#7d93a6", right: "#5e7385" }); gable(g, x + 0.1, y + 0.3, 0.75, 0.55, h, 14, "#536677");
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
    box(g, x + 0.1, y + 0.3, 0.75, 0.55, h, "#f4f7fa", { right: "#c9d3dc" }); for (let i = 0; i < 3; i++) { const [fx, fy] = iso(x + 0.25 + i * 0.2, y + 0.55, h + 3); g.fillStyle = "#8b97a3"; g.beginPath(); g.ellipse(fx, fy, 10, 5, 0, 0, Math.PI * 2); g.fill(); g.save(); g.translate(fx, fy); g.scale(1, 0.5); g.rotate(c.t * 6); g.strokeStyle = "#e9eef2"; g.lineWidth = 2; g.beginPath(); g.moveTo(-7, 0); g.lineTo(7, 0); g.moveTo(0, -7); g.lineTo(0, 7); g.stroke(); g.restore(); }
    door(g, x + 0.1, y + 0.3, 0.75, 0.55, "L", 0.35, 0.3, h * 0.7, "#5fb0e8"); },
  tank(c) { const { g, x, y } = c, h = H(c, 30);
    for (const [dx, dy, r] of [[0.28, 0.28, 0.16], [0.68, 0.3, 0.13], [0.45, 0.68, 0.14]]) cyl(g, x + dx, y + dy, r, h, "#e9eef2", { bands: "#c94a3a", bandAt: [0.8], top: "#cfd6dd" });
    g.strokeStyle = "#e0a83a"; g.lineWidth = 2; g.beginPath(); g.moveTo(...iso(x + 0.28, y + 0.28, 3)); g.lineTo(...iso(x + 0.45, y + 0.68, 3)); g.lineTo(...iso(x + 0.68, y + 0.3, 3)); g.stroke(); },
  logistics(c) { const { g, x, y } = c, h = H(c, 32);
    box(g, x + 0.04, y + 0.2, 0.9, 0.68, h, "#9fb3c6", { left: "#8fa3b5", right: "#6c8195" }); for (let i = 0; i < 6; i++) door(g, x + 0.04, y + 0.2, 0.9, 0.68, "L", 0.04 + i * 0.16, 0.12, h * 0.55, "#39434c");
    fillPoly(g, [iso(x + 0.04, y + 0.88, h * 0.82), iso(x + 0.94, y + 0.88, h * 0.82), iso(x + 0.94, y + 0.88, h * 0.95), iso(x + 0.04, y + 0.88, h * 0.95)], "#f2b631"); snowCap(g, x + 0.04, y + 0.2, 0.9, 0.68, h, c);
    for (let i = 0; i < 3; i++) truck(g, x + 0.2 + i * 0.25, y + 0.96, 2, "#3f7fa8", "#c9d3dc"); },
};
export const LOOK_OF_RECIPE = { e_gunes: "solar" };
