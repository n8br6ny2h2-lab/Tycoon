import test from "node:test"; import assert from "node:assert/strict";
import { fresh, rich } from "./helpers.mjs";
import * as M from "../src/core/market.js";
import { RESOURCES, RES } from "../src/data/resources.js";
import { advance } from "../src/core/game.js";
import { qty } from "../src/core/inventory.js";

test("alım fiyatı yükseltir, satım düşürür", () => {
  const s = rich(); const p0 = M.price(s, "celik");
  assert.ok(M.buy(s, "celik", 200, "anadolu").ok === false, "genel depoda metal yeri yok"); assert.equal(M.price(s, "celik"), p0);
  assert.ok(M.buy(s, "kereste", 300, "anadolu").ok); assert.ok(M.price(s, "kereste") > RES.kereste.base);
  advance(s, 60 * 25); const p1 = M.price(s, "kereste");
  assert.ok(M.sell(s, "kereste", 300, "anadolu").ok); assert.ok(M.price(s, "kereste") < p1);
});
test("hemen al-sat her zaman zarar (makas + komisyon + vergi + nakliye)", () => {
  for (const id of ["kereste", "bugday", "cam_kumu", "pamuk", "kimyasal"]) {
    const s = rich(); const b = M.quote(s, id, 100, "buy", "anadolu"), sl = M.quote(s, id, 100, "sell", "anadolu");
    assert.ok(sl.total < b.total, id);
  }
});
test("büyük emir kayma yaratır: birim fiyat miktarla artar", () => {
  const s = rich(), a = M.quote(s, "bugday", 10, "buy", "anadolu"), b = M.quote(s, "bugday", 5000, "buy", "anadolu");
  assert.ok(b.unit > a.unit * 1.005);
});
test("nakit ve depo kontrolü", () => {
  const s = fresh(); s.company.cash = 1000;
  assert.equal(M.buy(s, "kereste", 10, "anadolu").msg, "Nakit yetersiz");
  s.company.cash = 1e9; assert.equal(M.buy(s, "kereste", 5000, "anadolu").msg, "Depoda yer yok");
  assert.equal(M.buy(s, "petrol", 1, "anadolu").msg, "Depoda yer yok");
  assert.equal(M.buy(s, "elektrik", 1, "anadolu").ok, false);
  assert.equal(M.sell(s, "kereste", 1, "anadolu").msg, "Stok yetersiz");
});
test("teslimat tedarikçi süresinde gelir, ortalama maliyet tutulur", () => {
  const s = rich(); M.buy(s, "kereste", 100, "anadolu", "hizli");
  assert.equal(qty(s, "anadolu", "kereste"), 0); advance(s, 6 * 60 + 1); assert.equal(qty(s, "anadolu", "kereste"), 100);
  M.buy(s, "kereste", 100, "anadolu", "hizli"); advance(s, 7 * 60);
  const it = s.inventory.anadolu.kereste; assert.equal(it.q, 200); assert.ok(it.cost > RES.kereste.base);
});
test("satış kârı ortalama maliyete göre hesaplanır", () => {
  const s = rich(); M.buy(s, "kereste", 100, "anadolu", "hizli"); advance(s, 7 * 60);
  const cost = s.inventory.anadolu.kereste.cost, r = M.sell(s, "kereste", 50, "anadolu");
  assert.ok(Math.abs(r.profit - (r.quote.total - cost * 50)) < 1e-6);
});
test("limit emri fiyat sınırında gerçekleşir, süresi dolunca düşer", () => {
  const s = rich(); const q = M.quote(s, "kereste", 20, "buy", "anadolu");
  M.placeLimit(s, "kereste", "buy", 20, q.perUnit * 1.5, "anadolu"); advance(s, 61);
  assert.equal(s.orders.length, 0); assert.ok(s.shipments.some(x => x.kind === "purchase"));
  M.placeLimit(s, "kereste", "buy", 20, 1, "anadolu"); advance(s, 8 * 1440); assert.equal(s.orders.length, 0);
});
test("toplu sipariş her gün bir parti getirir", () => {
  const s = rich(); M.placeBulk(s, "kereste", 20, 3, "anadolu"); advance(s, 5 * 1440);
  assert.ok(Math.abs(qty(s, "anadolu", "kereste") - 60) < 1e-6);
});
test("oyuncu yokken 2 oyun yılında fiyatlar sınırlar içinde ve geçmiş penceresi doğru", () => {
  const s = fresh(3); s.company.cash = 1e12; advance(s, 730 * 1440);
  for (const r of RESOURCES) { const p = s.market[r.id].price; assert.ok(p >= r.min * 0.999 && p <= r.max * 1.001, r.id + " " + p); }
  assert.equal(M.history(s, "celik", "24s").length, 24); assert.equal(M.history(s, "celik", "7g").length, 42);
  assert.equal(M.history(s, "celik", "30g").length, 31); assert.equal(M.history(s, "celik", "1y").length, 366);
  // fiyatlar donmadı ama taban fiyattan da kopmadı
  const dev = RESOURCES.map(r => Math.abs(Math.log(s.market[r.id].price / r.base))); assert.ok(dev.reduce((a, b) => a + b) / dev.length < 0.4);
});
