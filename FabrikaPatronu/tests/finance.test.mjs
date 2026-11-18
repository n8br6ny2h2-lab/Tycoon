import test from "node:test"; import assert from "node:assert/strict";
import { fresh, rich } from "./helpers.mjs";
import * as Fi from "../src/core/finance.js";
import * as E from "../src/core/employees.js";
import { advance } from "../src/core/game.js";

test("kredi: anüite taksiti, aylık ödeme, borç azalır", () => {
  const s = fresh(); const o = Fi.loanOffers(s)[0]; const r = Fi.takeLoan(s, o.id, 2000000, 12); assert.ok(r.ok);
  const L = s.loans[0]; const expect = 2000000 * L.rate / (1 - Math.pow(1 + L.rate, -12)); assert.ok(Math.abs(L.pay - expect) < 1e-6);
  advance(s, 31 * 1440); assert.ok(s.loans[0].left < 2000000 && s.loans[0].paid === 1);
  assert.equal(Fi.takeLoan(s, o.id, 1e12, 12).ok, false);
});
test("aylık kurumlar vergisi yalnızca kâr üzerinden", () => {
  const s = fresh(); s.ledger.monthProfit = 1000000; const c = s.company.cash; Fi.financeMonth(s); assert.ok(Math.abs(c - s.company.cash - 200000) < 1);
  s.ledger.monthProfit = -500000; const c2 = s.company.cash; Fi.financeMonth(s); assert.equal(s.company.cash, c2);
});
test("maaşlar günlük kesilir, eğitim seviyeyi artırır", () => {
  const s = rich(); E.hire(s, "muhendis", 2); const c = s.company.cash; advance(s, 1440); assert.ok(c - s.company.cash >= 2 * 75000 / 30 - 1);
  assert.ok(E.train(s, "muhendis").ok); advance(s, 4 * 1440); assert.equal(s.staff.muhendis.lvl, 2);
});
test("eksi bakiye → kurtarma seçenekleri → kullanılmazsa iflas", () => {
  const s = fresh(); s.company.cash = -5e7; advance(s, 6 * 1440); assert.ok(s.company.rescue);
  assert.ok(Fi.rescueOptions(s).some(o => o.id === "tesvik"));
  const s2 = fresh(); s2.company.cash = -5e7; advance(s2, 6 * 1440); assert.ok(Fi.takeRescue(s2, "tesvik").ok);
  assert.equal(Fi.rescueOptions(s2).some(o => o.id === "tesvik"), false);
  s.company.cash = -5e8; advance(s, 8 * 1440); assert.equal(s.company.bankrupt, true);
});
test("şirket değeri: nakit + varlık − borç", () => {
  const s = fresh(); const v0 = Fi.companyValue(s); Fi.takeLoan(s, "ticari", 1000000, 6); assert.ok(Math.abs(Fi.companyValue(s) - v0) < 1);
});
