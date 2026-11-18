// EmployeeManager — işe alım, çıkarma, eğitim, maaş, verim.
import { POS, POSITIONS, TRAINING } from "../data/employees.js";
import { FT } from "../data/factories.js";
import { spend } from "./finance.js";
import { note } from "./util.js";

export function staffEff(s, pos) {
  const p = POS[pos], st = s.staff[pos]; if (!st || !p.per) return 0;
  return Math.min(p.cap, st.n * p.per * (1 + TRAINING.bonusPerLevel * (st.lvl - 1)));
}
// Fabrikaların toplam işçi ihtiyacı (otomasyon azaltır).
export function workersNeeded(s) {
  let n = 0;
  for (const b of Object.values(s.buildings)) if (b.kind !== "warehouse" && b.on) n += FT[b.type].workers * b.machines * (1 - 0.2 * b.automation);
  return Math.ceil(n);
}
// İşçi verimi: deneyim seviyesiyle artar, eksik işçi oranında düşer.
export function workerEfficiency(s) {
  const need = workersNeeded(s); if (!need) return 1;
  const st = s.staff.isci, eff = Math.min(1, st.n / need);
  return eff * (1 + 0.05 * (st.lvl - 1));
}
export function hire(s, pos, n = 1) {
  const p = POS[pos]; if (!p || !(n > 0)) return { ok: false };
  const fee = p.salary * 0.25 * n; if (s.company.cash < fee) return { ok: false, msg: "Nakit yetersiz (işe alım gideri)" };
  spend(s, fee, "İşe alım"); s.staff[pos].n += n; return { ok: true };
}
export function fire(s, pos, n = 1) {
  const st = s.staff[pos]; n = Math.min(n, st.n); if (!n) return { ok: false };
  spend(s, POS[pos].salary * 0.5 * n, "Tazminat"); st.n -= n; return { ok: true };
}
export const trainingCost = (s, pos) => POS[pos].salary * TRAINING.costMonths * s.staff[pos].lvl * Math.max(1, s.staff[pos].n);
export function train(s, pos) {
  const st = s.staff[pos]; if (st.lvl >= TRAINING.maxLevel) return { ok: false, msg: "En üst seviye" };
  if (st.training) return { ok: false, msg: "Eğitim sürüyor" };
  if (!st.n) return { ok: false, msg: "Bu pozisyonda çalışan yok" };
  const c = trainingCost(s, pos); if (s.company.cash < c) return { ok: false, msg: "Nakit yetersiz" };
  spend(s, c, "Eğitim"); st.training = TRAINING.days; return { ok: true };
}
export function employeesDay(s) {
  let pay = 0;
  for (const p of POSITIONS) {
    const st = s.staff[p.id]; pay += st.n * p.salary / 30 * (1 + 0.05 * (st.lvl - 1));
    if (st.training && --st.training <= 0) { st.training = 0; st.lvl++; note(s, `${p.name} eğitimi tamamlandı: seviye ${st.lvl}.`, "good"); }
  }
  if (pay) spend(s, pay, "Maaşlar");
}
export const monthlyPayroll = s => POSITIONS.reduce((a, p) => a + s.staff[p.id].n * p.salary * (1 + 0.05 * (s.staff[p.id].lvl - 1)), 0);
