import test from "node:test"; import assert from "node:assert/strict";
import { rich } from "./helpers.mjs";
import { makeOffer } from "../src/core/customers.js";
import * as K from "../src/core/contracts.js";
import { advance } from "../src/core/game.js";
import { day } from "../src/core/util.js";

const offer = (s, o) => { const x = makeOffer(s, { type: "kucuk", kind: "tek", res: "kereste", ...o }); x.q = 10; s.offers.push(x); return x; };
test("zamanında teslimat ödenir, itibar artar", () => {
  const s = rich(); const o = offer(s); const c = K.accept(s, o.id).contract;
  s.inventory.anadolu.kereste = { q: 20, cost: 8000, ql: 70 }; const cash = s.company.cash, rep = s.company.rep;
  assert.ok(K.deliver(s, c.id, "anadolu").ok); advance(s, 30 * 60);
  assert.equal(c.done, 1); assert.equal(c.status, "done"); assert.ok(s.company.cash > cash); assert.ok(s.company.rep > rep);
});
test("kalite yetersizse teslim edilmez", () => {
  const s = rich(); const o = offer(s, {}); o.minQ = 80; const c = K.accept(s, o.id).contract;
  s.inventory.anadolu.kereste = { q: 20, cost: 8000, ql: 60 }; assert.equal(K.deliver(s, c.id, "anadolu").ok, false);
});
test("teslim edilmezse ceza ve itibar kaybı; iki kaçırmada fesih", () => {
  const s = rich(); const o = makeOffer(s, { type: "orta", kind: "haftalik", res: "kereste" }); s.offers.push(o);
  const c = K.accept(s, o.id).contract, rep = s.company.rep; advance(s, (c.leadDays + 4) * 1440);
  assert.equal(c.missed, 1); assert.ok(s.company.rep < rep); advance(s, 12 * 1440); assert.equal(c.status, "cancelled");
});
test("gecikmeli teslimat %10 cezalı ödenir", () => {
  const s = rich(); const o = offer(s); const c = K.accept(s, o.id).contract; c.due = day(s) - 1;
  s.inventory.anadolu.kereste = { q: 20, cost: 8000, ql: 70 }; K.deliver(s, c.id, "anadolu"); advance(s, 30 * 60);
  assert.equal(c.log[0].late, true); assert.ok(Math.abs(c.log[0].pay - c.price * 10 * 0.9 * (1 + Math.min(0.05, (70 - c.minQ) * 0.002))) < 1);
});
test("ihale: en düşük teklif kazanır, teminat iade edilir", () => {
  const s = rich(); const o = makeOffer(s, { type: "devlet", kind: "ihale", res: "cimento" }); s.offers.push(o);
  const cash = s.company.cash; assert.ok(K.bid(s, o.id, s.market.cimento.price * 0.5).ok);
  advance(s, 4 * 1440); assert.ok(s.contracts.some(c => c.res === "cimento" && c.status === "active")); assert.equal(s.stats.tendersWon, 1);
  assert.ok(s.company.cash > cash - o.q * s.market.cimento.price * 0.02 * 0.5 - 2e6);
});
test("otomatik teslimat stok varsa kendiliğinden gönderir", () => {
  const s = rich(); const o = offer(s); const c = K.accept(s, o.id).contract; c.auto = true;
  s.inventory.anadolu.kereste = { q: 20, cost: 8000, ql: 70 }; advance(s, c.leadDays * 1440); assert.equal(c.done, 1);
});
