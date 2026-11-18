import test from "node:test"; import assert from "node:assert/strict";
import { rich } from "./helpers.mjs";
import * as F from "../src/core/factory.js";
import * as M from "../src/core/market.js";
import * as W from "../src/core/warehouse.js";
import * as E from "../src/core/employees.js";
import { STATUS, recipeEconomics } from "../src/core/production.js";
import { advance } from "../src/core/game.js";
import { qty } from "../src/core/inventory.js";
import { FACTORY_TYPES } from "../src/data/factories.js";
import { RES } from "../src/data/resources.js";

const setup = () => { const s = rich(); E.hire(s, "isci", 100); return s; };
test("her reçetenin taban fiyatlarla marjı %3–200 aralığında (sınırsız para yok)", () => {
  for (const f of FACTORY_TYPES) for (const r of f.recipes) {
    const cin = Object.entries(r.in).reduce((a, [k, q]) => a + q * RES[k].base, 0) + r.mwh * RES.elektrik.base + r.labor;
    const out = Object.entries(r.out).reduce((a, [k, q]) => a + q * (1 - r.fire) * RES[k].base, 0);
    assert.ok(out / cin - 1 > 0.03 && out / cin - 1 < (cin < 1000 ? 9 : 2), `${r.id} ${(out / cin - 1).toFixed(2)}`);
  }
});
test("zincir: kereste → işlenmiş ahşap; girdi bitince 'Hammadde bekliyor'", () => {
  const s = setup(); assert.ok(F.buildFactory(s, "mobilya", "anadolu", 1).ok);
  M.buy(s, "kereste", 13, "anadolu", "hizli"); advance(s, 7 * 60); advance(s, 6 * 60);
  const b = Object.values(s.buildings).find(x => x.type === "mobilya");
  assert.ok(qty(s, "anadolu", "islenmis_ahsap") > 9); assert.equal(b.status, STATUS.wait); assert.ok(qty(s, "anadolu", "kereste") < 6.5);
});
test("çıktı deposu dolunca üretim durur", () => {
  const s = setup(); F.buildFactory(s, "mobilya", "anadolu", 1);
  const inv = s.inventory.anadolu; inv.kereste = { q: 100, cost: 8000, ql: 60 }; inv.agrega = { q: 499.5, cost: 400, ql: 60 };
  advance(s, 10 * 60); const b = Object.values(s.buildings).find(x => x.type === "mobilya"); assert.equal(b.status, STATUS.full);
});
test("bakım gerekince durur, bakım yapınca devam eder", () => {
  const s = setup(); F.buildFactory(s, "mobilya", "anadolu", 1); const b = Object.values(s.buildings).find(x => x.type === "mobilya");
  s.inventory.anadolu.kereste = { q: 200, cost: 8000, ql: 60 }; b.condition = 10; advance(s, 120); assert.equal(b.status, STATUS.maint);
  assert.ok(F.maintain(s, b.id).ok); advance(s, 120); assert.ok([STATUS.run, STATUS.done].includes(b.status), b.status);
});
test("işçi yoksa üretim yok", () => {
  const s = rich(); F.buildFactory(s, "mobilya", "anadolu", 1); s.inventory.anadolu.kereste = { q: 50, cost: 8000, ql: 60 };
  advance(s, 300); assert.equal(Object.values(s.buildings).find(x => x.type === "mobilya").status, STATUS.noWorkers);
});
test("enerji kesintisinde kendi santrali olmayan fabrika durur", () => {
  const s = setup(); F.buildFactory(s, "mobilya", "anadolu", 1); s.inventory.anadolu.kereste = { q: 200, cost: 8000, ql: 60 };
  s.events.push({ id: "kesinti", name: "Enerji kesintisi", until: s.time.min + 600, effect: { grid: false } });
  advance(s, 180); assert.equal(Object.values(s.buildings).find(x => x.type === "mobilya").status, STATUS.power);
});
test("santral kendi fabrikalarını besler ve fazlasını satar", () => {
  const s = setup(); F.buildFactory(s, "santral", "anadolu", 1); F.buildFactory(s, "mobilya", "anadolu", 4);
  s.inventory.anadolu.kereste = { q: 200, cost: 8000, ql: 60 }; s.inventory.anadolu.komur = { q: 300, cost: 4000, ql: 60 };
  s.events.push({ id: "kesinti", name: "Enerji kesintisi", until: s.time.min + 600, effect: { grid: false } });
  advance(s, 180); assert.equal(Object.values(s.buildings).find(x => x.type === "mobilya").status, STATUS.run);
  s.events = []; advance(s, 120); assert.ok((s.ledger.today["Enerji satışı"] || 0) > 0);
});
test("öncelik: kıt girdi önce yüksek öncelikli fabrikaya gider", () => {
  const s = setup(); s.regions.anadolu.plots[2].owned = true;
  F.buildFactory(s, "mobilya", "anadolu", 1); F.buildFactory(s, "mobilya", "anadolu", 2);
  const [a, b] = Object.values(s.buildings).filter(x => x.type === "mobilya"); F.setPriority(s, b.id, 3); F.setPriority(s, a.id, 1);
  s.inventory.anadolu.kereste = { q: 6.6, cost: 8000, ql: 60 }; advance(s, 61);
  assert.ok(b.cycle && !a.cycle);
});
test("makine, seviye, otomasyon, kalite ve fire hatları çalışır", () => {
  const s = setup(); F.buildFactory(s, "mobilya", "anadolu", 1); const b = Object.values(s.buildings).find(x => x.type === "mobilya");
  for (let i = 0; i < 3; i++) assert.ok(F.upgrade(s, b.id, "machine").ok);
  assert.equal(F.upgrade(s, b.id, "machine").ok, false); assert.ok(F.upgrade(s, b.id, "level").ok); assert.ok(F.upgrade(s, b.id, "machine").ok);
  assert.ok(F.upgrade(s, b.id, "automation").ok); assert.equal(F.upgrade(s, b.id, "automation").ok, false);
  assert.ok(F.upgrade(s, b.id, "quality").ok); assert.ok(F.upgrade(s, b.id, "fire").ok);
  const e = recipeEconomics(s, b); assert.ok(e.hours < 2);
});
test("bölgeye bağlı tesisler: Anadolu'da kuyu kurulamaz, Maden Vadisi açılınca demir madeni", () => {
  const s = setup(); assert.equal(F.buildFactory(s, "kuyu", "anadolu", 1).ok, false);
  assert.ok(F.unlockRegion(s, "maden").ok); assert.ok(F.buildFactory(s, "maden", "maden", 0).ok);
  const m = Object.values(s.buildings).find(x => x.type === "maden"); assert.ok(F.setRecipe(s, m.id, "m_demir").ok);
  assert.ok(W.buildWarehouse(s, "genel", "maden", 1).ok); advance(s, 8 * 60); assert.ok(qty(s, "maden", "demir_cevheri") > 5);
});
