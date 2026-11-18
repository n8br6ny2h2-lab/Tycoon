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
// Büyük fiyat grafiği (eksenli).
export function chart(arr, w = 520, h = 170, labels = []) {
  const a = arr.filter(x => isFinite(x)); if (a.length < 2) return `<div class="muted">Grafik için veri birikiyor.</div>`;
  const mn = Math.min(...a), mx = Math.max(...a), pad = (mx - mn) * 0.12 || mx * 0.02, lo = mn - pad, hi = mx + pad, X = i => 44 + i / (a.length - 1) * (w - 52), Y = v => 8 + (1 - (v - lo) / (hi - lo)) * (h - 28);
  const pts = a.map((v, i) => `${X(i).toFixed(1)},${Y(v).toFixed(1)}`).join(" "), up = a[a.length - 1] >= a[0], col = up ? "#3ddc97" : "#ff6b5e";
  const grid = [0, 0.5, 1].map(f => { const v = lo + (hi - lo) * (1 - f), y = Y(v); return `<line x1="44" x2="${w - 6}" y1="${y}" y2="${y}" stroke="rgba(255,255,255,.08)"/><text x="40" y="${y + 4}" text-anchor="end" font-size="10" fill="#8ea4b6">${v >= 1e5 ? nf0.format(v / 1e3) + "b" : hi - lo < 30 ? nf1.format(v) : nf0.format(v)}</text>`; }).join("");
  const lab = labels.map(([f, t]) => `<text x="${X(f * (a.length - 1))}" y="${h - 4}" text-anchor="middle" font-size="10" fill="#8ea4b6">${t}</text>`).join("");
  return `<svg class="chart" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none" role="img" aria-label="Fiyat grafiği"><defs><linearGradient id="cg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${col}" stop-opacity=".3"/><stop offset="1" stop-color="${col}" stop-opacity="0"/></linearGradient></defs>${grid}
    <polygon points="44,${h - 20} ${pts} ${w - 8},${h - 20}" fill="url(#cg)"/><polyline points="${pts}" fill="none" stroke="${col}" stroke-width="2" stroke-linejoin="round"/>${lab}</svg>`;
}
