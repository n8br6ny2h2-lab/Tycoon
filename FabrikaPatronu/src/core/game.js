// GameManager — oyun saati, tik döngüsü (saatlik/günlük/aylık adımlar), hız, çevrimdışı ilerleme ve saat hilesi koruması.
import { BAL } from "../data/balance.js";
import { MIN_DAY, day, note, level } from "./util.js";
import { marketHour, marketDay, ordersHour, bulkDay } from "./market.js";
import * as market from "./market.js";
import { productionHour, factoriesDay, automationTick } from "./production.js";
import { energyReset, energySettle, economyDay } from "./economy.js";
import { shipmentsHour, routesTick, fleetDay } from "./logistics.js";
import { warehouseDay, autoStockDay } from "./warehouse.js";
import { customersDay, bigOffer } from "./customers.js";
import { contractsDay, tendersDay, onContractArrival } from "./contracts.js";
import { employeesDay } from "./employees.js";
import { researchHour } from "./research.js";
import { competitorsDay, rivalBids } from "./competitors.js";
import { financeDay, financeMonth, companyValue, spend } from "./finance.js";
import { eventsDay, specialWeek } from "./events.js";
import { questsDay, achievementsCheck, checkValueQuests } from "./quests.js";
import { has } from "./util.js";

export function hourStep(s) {
  energyReset(s);
  productionHour(s);
  energySettle(s);
  shipmentsHour(s, onContractArrival);
  researchHour(s);
  marketHour(s);
  ordersHour(s);
  if (Math.floor(s.time.min / 60) % 6 === 0) automationTick(s, market);
  if (has(s, "lojistik_opt") && Math.floor(s.time.min / 60) % 24 === 12) routesTick(s);
}
export function dayStep(s) {
  const d = day(s);
  marketDay(s);
  economyDay(s);
  employeesDay(s); factoriesDay(s); fleetDay(s); warehouseDay(s);
  bulkDay(s); autoStockDay(s, market); routesTick(s);
  competitorsDay(s);
  eventsDay(s, { onBigOffer: () => bigOffer(s) });
  contractsDay(s); tendersDay(s, rivalBids); customersDay(s);
  if (d % 7 === 0) specialWeek(s);
  if (s.company.partner && s.company.partner.until >= d) { const net = Object.entries(s.ledger.today).reduce((a, [, v]) => a + v, 0); if (net > 0) spend(s, net * s.company.partner.share, "Ortak payı"); }
  financeDay(s);
  checkValueQuests(s); questsDay(s); achievementsCheck(s);
  if (d % 30 === 0 && d > 0) financeMonth(s);
}
// Oyun dakikası kadar ilerlet; saat ve gün sınırlarında adımları çalıştır. Döndürür: çalışan saat sayısı.
export function advance(s, minutes) {
  if (s.company.bankrupt) return 0;
  let hours = 0; const end = s.time.min + minutes;
  while (true) {
    const nextHour = (Math.floor(s.time.min / 60) + 1) * 60;
    if (nextHour > end) { s.time.min = end; break; }
    s.time.min = nextHour; hourStep(s); hours++;
    if (s.time.min % MIN_DAY === 0) dayStep(s);
    if (s.company.bankrupt) break;
  }
  return hours;
}
// Gerçek zamandan ilerleme: tarayıcı/uygulama her kare çağırır.
export function tickReal(s, realSeconds) {
  const sp = BAL.speeds[s.time.speed] ?? 1;
  return advance(s, realSeconds * BAL.minutesPerRealSecond * sp);
}
// Çevrimdışı ilerleme: son kayıttan bu yana geçen gerçek süre (en fazla 12 saat), ×1 hızla. Saat geri alınmışsa ilerleme yok.
export function applyOffline(s, nowMs) {
  const last = s.time.lastReal || nowMs;
  if (nowMs < last) { s.time.lastReal = nowMs; s.offline = { skipped: true, reason: "Cihaz saati geri alınmış; çevrimdışı ilerleme verilmedi." }; return s.offline; }
  const realSec = Math.min((nowMs - last) / 1000, BAL.offlineMaxRealHours * 3600);
  s.time.lastReal = nowMs;
  if (realSec < 60) return null;
  const before = snapshot(s);
  const hours = advance(s, Math.min(realSec * BAL.minutesPerRealSecond, BAL.offlineMaxGameHours * 60));
  const after = snapshot(s);
  s.offline = { realHours: realSec / 3600, gameHours: hours, capped: realSec * BAL.minutesPerRealSecond > BAL.offlineMaxGameHours * 60, before, after };
  return s.offline;
}
export function snapshot(s) {
  const cats = {}; for (const d of s.ledger.days) for (const [k, v] of Object.entries(d.cats)) cats[k] = (cats[k] || 0) + v;
  for (const [k, v] of Object.entries(s.ledger.today)) cats[k] = (cats[k] || 0) + v;
  return { cash: s.company.cash, value: companyValue(s), produced: { ...s.stats.producedBy }, deliveries: s.stats.deliveries, day: day(s), cats, notes: s.notes.length, rep: s.company.rep };
}
export function touchReal(s, nowMs) { s.time.lastReal = Math.max(s.time.lastReal || 0, nowMs); }
