import test from "node:test"; import assert from "node:assert/strict";
import { fresh, memStorage } from "./helpers.mjs";
import * as S from "../src/core/save.js";
import { advance, applyOffline } from "../src/core/game.js";
import * as M from "../src/core/market.js";

test("kayıt/yükleme aynı durumu verir", async () => {
  const s = fresh(); advance(s, 3 * 1440); const st = memStorage(); await S.saveGame(s, st);
  const r = await S.loadGame(st); assert.equal(r.from, "main"); assert.deepEqual(r.state, JSON.parse(JSON.stringify(s)));
});
test("bozuk kayıt yedekten kurtarılır", async () => {
  const s = fresh(), st = memStorage(); await S.saveGame(s, st); advance(s, 1440); await S.saveGame(s, st);
  const t = st.get(S.KEY); st.set(S.KEY, t.slice(0, 200) + "9" + t.slice(201)); const r = await S.loadGame(st);
  assert.equal(r.from, "backup"); assert.ok(r.state);
});
test("eski kayıtta eksik alanlar göçle doldurulur", () => {
  const s = fresh(); delete s.routes; delete s.market.otomobil; delete s.staff.mudur; const m = S.migrate(JSON.parse(JSON.stringify(s)), 1);
  assert.ok(Array.isArray(m.routes) && m.market.otomobil && m.staff.mudur);
});
test("çevrimdışı: 20 dk gerçek süre = 40 oyun saati, en fazla 3 oyun günü; saat geri alınırsa ilerleme yok", () => {
  const s = fresh(); s.time.lastReal = 1e12; const m0 = s.time.min;
  applyOffline(s, 1e12 + 20 * 60e3); assert.equal(s.time.min - m0, 40 * 60);
  const m1 = s.time.min; applyOffline(s, 1e12); assert.equal(s.time.min, m1); assert.ok(s.offline.skipped);
  const s2 = fresh(); s2.time.lastReal = 1e12; const m2 = s2.time.min; applyOffline(s2, 1e12 + 100 * 3600e3); assert.equal(s2.time.min - m2, 72 * 60); assert.ok(s2.offline.capped);
});
