import test from "node:test"; import assert from "node:assert/strict";
import { rich } from "./helpers.mjs";
import * as L from "../src/core/logistics.js";
import * as W from "../src/core/warehouse.js";
import * as F from "../src/core/factory.js";
import { advance } from "../src/core/game.js";
import { qty } from "../src/core/inventory.js";

const two = () => { const s = rich(); F.unlockRegion(s, "maden"); W.buildWarehouse(s, "genel", "maden", 0); for (const b of Object.values(s.buildings)) b.security = 3; s.staff.depo.n = 8; s.inventory.maden.kereste = { q: 100, cost: 8000, ql: 60 }; return s; };
test("bölgeler arası transfer: mesafe kadar sürer, maliyet ve stok taşınır", () => {
  const s = two(); const r = L.dispatch(s, { from: "maden", to: "anadolu", items: [{ res: "kereste", q: 40 }] });
  assert.ok(r.ok); assert.equal(qty(s, "maden", "kereste"), 60); assert.equal(qty(s, "anadolu", "kereste"), 0);
  advance(s, Math.ceil(r.hours * 60) + 61); assert.equal(qty(s, "anadolu", "kereste"), 40); assert.ok(s.inventory.anadolu.kereste.cost > 8000);
});
test("kendi filosu: araç meşgul olur, dönünce boşalır; tren demiryolu ister", () => {
  const s = two(); assert.ok(L.buyVehicle(s, "agir", "maden").ok);
  const r = L.dispatch(s, { from: "maden", to: "anadolu", items: [{ res: "kereste", q: 20 }], mode: "agir" }); assert.ok(r.ok);
  assert.equal(L.dispatch(s, { from: "maden", to: "anadolu", items: [{ res: "kereste", q: 20 }], mode: "agir" }).ok, false);
  advance(s, Math.ceil(r.hours * 2 * 60) + 61); assert.ok(L.dispatch(s, { from: "maden", to: "anadolu", items: [{ res: "kereste", q: 20 }], mode: "agir" }).ok);
  assert.equal(L.buyVehicle(s, "gemi", "maden").ok, false);
});
test("hedefte yer yoksa transfer reddedilir", () => {
  const s = two(); s.staff.depo.n = 0; s.inventory.anadolu.agrega = { q: 595, cost: 1, ql: 60 };
  assert.equal(L.dispatch(s, { from: "maden", to: "anadolu", items: [{ res: "kereste", q: 40 }] }).msg, "Hedef depoda yer yok");
});
test("otomatik rota her gün taşır, kaynakta bırakılacak miktara uyar", () => {
  const s = two(); L.addRoute(s, { from: "maden", to: "anadolu", res: "kereste", q: 30, every: 1, keep: 20 }); advance(s, 4 * 1440);
  assert.ok(Math.abs(qty(s, "maden", "kereste") - 20) < 1e-6); assert.ok(qty(s, "anadolu", "kereste") > 70);
});
