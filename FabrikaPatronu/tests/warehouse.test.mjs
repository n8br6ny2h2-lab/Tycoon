import test from "node:test"; import assert from "node:assert/strict";
import { rich, noNegative } from "./helpers.mjs";
import * as W from "../src/core/warehouse.js";
import * as M from "../src/core/market.js";
import { advance } from "../src/core/game.js";

test("depo sınıfları: petrol tank ister, metal metal deposu", () => {
  const s = rich(); assert.equal(W.freeFor(s, "anadolu", "petrol"), 0);
  assert.ok(W.buildWarehouse(s, "tank", "anadolu", 1).ok); assert.ok(W.freeFor(s, "anadolu", "petrol") >= 1000);
  assert.ok(M.buy(s, "petrol", 900, "anadolu").ok); assert.equal(M.buy(s, "petrol", 200, "anadolu").msg, "Depoda yer yok");
});
test("gelmekte olan mal yer ayırır, kapasite dolunca kabul yok", () => {
  const s = rich(); assert.ok(M.buy(s, "kereste", 590, "anadolu").ok); assert.equal(M.buy(s, "kereste", 20, "anadolu").msg, "Depoda yer yok");
});
test("seviye ve raf sistemi kapasiteyi artırır", () => {
  const s = rich(), b = Object.values(s.buildings)[0], c0 = W.whCap(s, b);
  W.upgradeWarehouse(s, b.id, "level"); W.upgradeWarehouse(s, b.id, "racks"); assert.ok(W.whCap(s, b) > c0 * 1.8);
});
test("büyük lojistik deposu genel+dökme+metal alır", () => {
  const s = rich(); assert.ok(W.buildWarehouse(s, "lojistik", "anadolu", 1).ok); assert.ok(W.freeFor(s, "anadolu", "celik") >= 4000);
});
test("otomatik stok alt sınırda alır, üst sınırda satar", () => {
  const s = rich(); W.setAutoStock(s, "anadolu", "kereste", 50, 120); advance(s, 3 * 1440);
  const q = s.inventory.anadolu.kereste.q; assert.ok(q >= 50 && q <= 120, q);
});
test("uzun sürede depo gideri ve hırsızlık stoğu eksiye düşürmez", () => {
  const s = rich(); M.buy(s, "kereste", 500, "anadolu"); advance(s, 200 * 1440); assert.equal(noNegative(s), null);
});
