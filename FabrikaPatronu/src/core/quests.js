// QuestManager — günlük/haftalık görevler, başarımlar, üretim yarışması.
import { DAILY, WEEKLY, ACHIEVEMENTS } from "../data/quests.js";
import { RESOURCES, RES } from "../data/resources.js";
import { RECIPE } from "../data/factories.js";
import { pick, rnd } from "./rng.js";
import { day, level, note } from "./util.js";
import { earn } from "./finance.js";
import { addXp } from "./progress.js";

function makeQuest(s, tpl, scope) {
  const l = level(s), q = { id: tpl.id + ":" + scope + ":" + day(s), tpl: tpl.id, scope, kind: tpl.kind, n: tpl.n(l), reward: tpl.reward(l), prog: 0, done: false, claimed: false };
  if (tpl.pick) {
    const produced = Object.values(s.buildings).filter(b => b.recipe).flatMap(b => Object.keys(RECIPE[b.recipe].out));
    const pool = tpl.pick === "raw" ? RESOURCES.filter(r => ["metal", "enerji", "endustriyel", "orman"].includes(r.cat) && r.storage !== "enerji" && r.base < 120000)
      : tpl.pick === "produced" ? produced.filter(id => id !== "elektrik").map(id => RES[id]) : RESOURCES.filter(r => r.storage !== "enerji" && r.base < 120000);
    const r = pool.length ? pick(s, pool) : RES.kereste; q.res = r.id;
    if (r.vol < 0.1) q.n = Math.round(q.n * 20);
  }
  q.text = tpl.text.replace("{n}", q.kind === "profit" ? Math.round(q.n).toLocaleString("tr-TR") : q.n).replace("{unit}", q.res ? RES[q.res].unit : "").replace("{res}", q.res ? RES[q.res].name : "");
  if (tpl.kind === "value") q.base = s.stats.lastValue || s.company.cash;
  return q;
}
export function initQuests(s) { s.quests = { daily: [], weekly: [], dailyDay: -1, weeklyWeek: -1 }; questsDay(s); }
export function questsDay(s) {
  const d = day(s), Q = s.quests;
  if (Q.dailyDay !== d) {
    Q.dailyDay = d; const pool = DAILY.filter(t => t.kind !== "produce" || Object.values(s.buildings).some(b => b.recipe));
    const pickN = []; while (pickN.length < 3 && pickN.length < pool.length) { const t = pick(s, pool); if (!pickN.includes(t)) pickN.push(t); }
    Q.daily = pickN.map(t => makeQuest(s, t, "g")); s.stats.profitToday = 0;
  }
  const w = Math.floor(d / 7);
  if (Q.weeklyWeek !== w) {
    Q.weeklyWeek = w; const pool = WEEKLY.filter(t => !t.level || level(s) >= t.level), pickN = [];
    while (pickN.length < 3 && pickN.length < pool.length) { const t = pick(s, pool); if (!pickN.includes(t)) pickN.push(t); }
    Q.weekly = pickN.map(t => makeQuest(s, t, "h"));
  }
}
// Sistemler olay bildirir: buy, sell, produce, deliveries, upgrade, profit, build, export, newProduct, research.
export function questEvent(s, kind, d = {}) {
  if (!s.quests) return;
  for (const q of [...s.quests.daily, ...s.quests.weekly]) {
    if (q.done || q.kind !== kind) continue;
    if (q.res && d.res !== q.res) continue;
    q.prog += kind === "profit" ? (d.value || 0) : (d.q || 1);
    if (q.prog >= q.n) { q.done = true; note(s, `Görev tamamlandı: ${q.text}. Ödülü almayı unutma.`, "good"); }
  }
  if (kind === "produce" && s.special && s.special.contest) { const c = s.special.contest; c.made = c.made || {}; c.made[d.res] = (c.made[d.res] || 0) + (d.q || 0); }
}
export function checkValueQuests(s) {
  for (const q of s.quests.weekly) if (q.kind === "value" && !q.done && s.stats.lastValue >= q.base * (1 + q.n / 100)) { q.done = true; q.prog = q.n; note(s, `Görev tamamlandı: ${q.text}.`, "good"); }
}
export function claim(s, qid) {
  const q = [...s.quests.daily, ...s.quests.weekly].find(x => x.id === qid);
  if (!q || !q.done || q.claimed) return { ok: false };
  q.claimed = true; earn(s, q.reward, "Ödül"); addXp(s, q.reward / 300); return { ok: true, reward: q.reward };
}
export function achievementsCheck(s) {
  for (const a of ACHIEVEMENTS) if (!s.achievements[a.id] && a.test(s)) { s.achievements[a.id] = s.time.min; note(s, `Başarım: ${a.name}`, "level"); addXp(s, 500 * level(s)); }
}
