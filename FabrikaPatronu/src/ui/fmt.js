// Biçimlendirme yardımcıları (Türkçe sayı biçimi).
import { RES } from "../data/resources.js";
import { monthOf } from "../core/util.js";
const nf0 = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 }), nf1 = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 1 }), nf2 = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 });
export const n0 = x => nf0.format(Math.round(x || 0)), n1 = x => nf1.format(x || 0), n2 = x => nf2.format(x || 0);
export function tl(x) { const a = Math.abs(x); const s = a >= 1e9 ? nf2.format(x / 1e9) + " Mlr" : a >= 1e6 ? nf2.format(x / 1e6) + " Mn" : a >= 1e4 ? nf0.format(x / 1e3) + " bin" : nf0.format(x); return s + " ₺"; }
export const tlFull = x => nf0.format(Math.round(x)) + " ₺";
export const pct = x => (x >= 0 ? "+" : "−") + nf1.format(Math.abs(x * 100)) + "%";
export function qtyTxt(id, q) { const r = RES[id]; return (r.vol < 0.05 || r.unit === "adet" ? nf0.format(q) : q < 10 ? nf2.format(q) : nf1.format(q)) + " " + r.unit; }
export const esc = s => String(s).replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
export function dateTxt(min) { const d = Math.floor(min / 1440), h = Math.floor((min % 1440) / 60), m = Math.floor(min % 60); const mon = ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"][monthOf(d)]; return { day: d + 1, date: `${(d % 30) + 1} ${mon}`, year: Math.floor((d + 60) / 360) + 1, clock: `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}` }; }
export function spark(arr, w = 70, h = 22, cls = "") {
  const a = arr.filter(x => isFinite(x)); if (a.length < 2) return `<svg class="spark ${cls}" width="${w}" height="${h}"></svg>`;
  const mn = Math.min(...a), mx = Math.max(...a), sp = mx - mn || 1, pts = a.map((v, i) => `${(i / (a.length - 1) * (w - 2) + 1).toFixed(1)},${(h - 2 - (v - mn) / sp * (h - 4)).toFixed(1)}`);
  const up = a[a.length - 1] >= a[0], col = up ? "var(--good)" : "var(--bad)";
  return `<svg class="spark ${cls}" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" aria-hidden="true"><polyline points="${pts.join(" ")}" fill="none" stroke="${col}" stroke-width="1.6" stroke-linejoin="round"/></svg>`;
}
// Büyük fiyat grafiği: yumuşak eğri, dolgu, ızgara, en yüksek/en düşük işaretleri ve son fiyat etiketi.
export function chart(arr, w = 520, h = 170, labels = []) {
  const a = arr.filter(x => isFinite(x)); if (a.length < 2) return `<div class="muted">Grafik için veri birikiyor.</div>`;
  const mn = Math.min(...a), mx = Math.max(...a), pad = (mx - mn) * 0.15 || mx * 0.01, lo = mn - pad, hi = mx + pad, L = 50, Rr = w - 64;
  const X = i => L + i / (a.length - 1) * (Rr - L), Y = v => 10 + (1 - (v - lo) / (hi - lo)) * (h - 32);
  const fmt = v => (v >= 1e5 ? nf0.format(v / 1e3) + " b" : hi - lo < 30 ? nf1.format(v) : nf0.format(v));
  let d = `M${X(0).toFixed(1)},${Y(a[0]).toFixed(1)}`;
  for (let i = 1; i < a.length; i++) { const x0 = X(i - 1), x1 = X(i), y0 = Y(a[i - 1]), y1 = Y(a[i]), cx = (x0 + x1) / 2; d += ` C${cx.toFixed(1)},${y0.toFixed(1)} ${cx.toFixed(1)},${y1.toFixed(1)} ${x1.toFixed(1)},${y1.toFixed(1)}`; }
  const up = a[a.length - 1] >= a[0], col = up ? "#3ddc97" : "#ff6b5e", last = a[a.length - 1], iMax = a.indexOf(mx), iMin = a.indexOf(mn), id = "cg" + Math.random().toString(36).slice(2, 7);
  const grid = [0, 0.25, 0.5, 0.75, 1].map(f => { const v = lo + (hi - lo) * (1 - f), y = Y(v); return `<line x1="${L}" x2="${Rr}" y1="${y}" y2="${y}" stroke="rgba(255,255,255,${f === 1 ? 0.14 : 0.06})"/><text x="${L - 6}" y="${y + 3.5}" text-anchor="end" font-size="10" fill="#8ea4b6">${fmt(v)}</text>`; }).join("");
  const vgrid = [0.25, 0.5, 0.75].map(f => `<line x1="${L + f * (Rr - L)}" x2="${L + f * (Rr - L)}" y1="10" y2="${h - 22}" stroke="rgba(255,255,255,.04)"/>`).join("");
  const lab = labels.map(([f, t]) => `<text x="${L + f * (Rr - L)}" y="${h - 6}" text-anchor="${f === 0 ? "start" : f === 1 ? "end" : "middle"}" font-size="10" fill="#8ea4b6">${t}</text>`).join("");
  const mark = (i, v, txt, dy) => `<circle cx="${X(i)}" cy="${Y(v)}" r="3" fill="#0c1620" stroke="#e9eef2" stroke-width="1.2"/><text x="${Math.min(Rr - 20, Math.max(L + 20, X(i)))}" y="${Y(v) + dy}" text-anchor="middle" font-size="9.5" fill="#c9d6e2">${txt} ${fmt(v)}</text>`;
  return `<svg class="chart" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none" role="img" aria-label="Fiyat grafiği"><defs><linearGradient id="${id}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${col}" stop-opacity=".35"/><stop offset="1" stop-color="${col}" stop-opacity="0"/></linearGradient></defs>${grid}${vgrid}
    <path d="${d} L${X(a.length - 1)},${h - 22} L${X(0)},${h - 22} Z" fill="url(#${id})"/><path d="${d}" fill="none" stroke="${col}" stroke-width="2.2" stroke-linejoin="round" stroke-linecap="round"/>
    ${mx > mn ? mark(iMax, mx, "en yüksek", -7) + mark(iMin, mn, "en düşük", 13) : ""}
    <line x1="${L}" x2="${Rr}" y1="${Y(last)}" y2="${Y(last)}" stroke="${col}" stroke-dasharray="3 4" stroke-opacity=".6"/><circle cx="${X(a.length - 1)}" cy="${Y(last)}" r="4" fill="${col}"/>
    <rect x="${Rr + 4}" y="${Y(last) - 9}" width="58" height="18" rx="5" fill="${col}"/><text x="${Rr + 33}" y="${Y(last) + 3.5}" text-anchor="middle" font-size="10.5" font-weight="800" fill="#0c1620">${fmt(last)}</text>${lab}</svg>`;
}
