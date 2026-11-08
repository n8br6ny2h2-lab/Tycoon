import { test } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { createApp, currentContract } from '../src/app.js';
import { Store } from '../src/store.js';
import { sanitizeName } from '../src/rules.js';

async function start(nowRef) {
  const store = new Store(null);
  const app = createApp({ store, events: { events: [{ id: 'e1' }] }, now: () => nowRef.t, rateLimit: 1000 });
  const server = http.createServer(app);
  await new Promise(r => server.listen(0, r));
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = async (method, path, body, token) => {
    const res = await fetch(base + path, {
      method, headers: { 'content-type': 'application/json', ...(token ? { authorization: `Bearer ${token}` } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    return { status: res.status, body: await res.json() };
  };
  return { server, call, store };
}

test('names are sanitised and offensive names rejected', () => {
  assert.equal(sanitizeName('  Çelik   Kulübü  '), 'Çelik Kulübü');
  assert.equal(sanitizeName('ab'), null);
  assert.equal(sanitizeName('fuck club'), null);
  assert.equal(sanitizeName('<b>Demir</b>'), 'bDemirb');
});

test('time and events are public, other routes need a token', async () => {
  const now = { t: Date.UTC(2027, 2, 3, 12) };
  const { server, call } = await start(now);
  try {
    assert.equal((await call('GET', '/v1/time')).body.ms, now.t);
    assert.equal((await call('GET', '/v1/events')).body.events[0].id, 'e1');
    assert.equal((await call('GET', '/v1/clubs/mine')).status, 401);
    assert.equal((await call('GET', '/v1/nope')).status, 404);
  } finally { server.close(); }
});

test('co-op: create, join, contribute cumulatively, complete, contract rotates weekly', async () => {
  const now = { t: Date.UTC(2027, 2, 3, 12) };
  const { server, call } = await start(now);
  try {
    const a = (await call('POST', '/v1/register', { name: 'Kral Sanayi' })).body;
    const b = (await call('POST', '/v1/register', { name: 'Demir A.Ş.' })).body;
    const created = (await call('POST', '/v1/coop/create', {}, a.token)).body;
    assert.equal(created.members.length, 1);
    const joined = (await call('POST', '/v1/coop/join', { code: created.code.toLowerCase() }, b.token)).body;
    assert.equal(joined.members.length, 2);
    await call('POST', '/v1/coop/contribute', { totalUnits: 60000 }, a.token);
    let v = (await call('POST', '/v1/coop/contribute', { totalUnits: 30000 }, a.token)).body; // lower total ignored
    assert.equal(v.members.find(m => m.name === 'Kral Sanayi').units, 60000);
    v = (await call('POST', '/v1/coop/contribute', { totalUnits: v.goal }, b.token)).body;
    assert.equal(v.completed, true);
    assert.equal((await call('POST', '/v1/coop/contribute', { totalUnits: -5 }, b.token)).status, 400);
    const c1 = currentContract(now.t).id;
    now.t += 7 * 24 * 3600 * 1000;
    assert.notEqual(currentContract(now.t).id, c1);
    assert.equal((await call('GET', '/v1/coop/mine', null, a.token)).body.coop, null, 'old co-op ends with the week');
  } finally { server.close(); }
});

test('clubs: unique names, weekly score, leaderboard, leave, report, delete account', async () => {
  const now = { t: Date.UTC(2027, 2, 3, 12) };
  const { server, call, store } = await start(now);
  try {
    const a = (await call('POST', '/v1/register', { name: 'Ayla' })).body;
    const b = (await call('POST', '/v1/register', { name: 'Barış' })).body;
    const club = (await call('POST', '/v1/clubs/create', { name: 'Fabrika Kralları' }, a.token)).body;
    assert.equal((await call('POST', '/v1/clubs/create', { name: 'fabrika kralları' }, b.token)).status, 409);
    assert.equal((await call('POST', '/v1/clubs/create', { name: 'xx' }, b.token)).status, 400);
    await call('POST', '/v1/clubs/join', { code: club.code }, b.token);
    await call('POST', '/v1/clubs/score', { weekXp: 1200 }, a.token);
    const v = (await call('POST', '/v1/clubs/score', { weekXp: 800 }, b.token)).body;
    assert.equal(v.weekScore, 2000);
    assert.equal((await call('GET', '/v1/clubs/top')).body.clubs[0].name, 'Fabrika Kralları');
    now.t += 7 * 24 * 3600 * 1000;
    assert.equal((await call('GET', '/v1/clubs/mine', null, a.token)).body.weekScore, 0, 'weekly reset');
    assert.equal((await call('POST', '/v1/report', { type: 'club', target: club.code, reason: 'name' }, b.token)).status, 200);
    assert.equal(store.data.reports.length, 1);
    await call('POST', '/v1/clubs/leave', {}, b.token);
    assert.equal((await call('DELETE', '/v1/player', null, a.token)).body.deleted, true);
    assert.equal(Object.keys(store.data.clubs).length, 0, 'empty club removed');
    assert.equal((await call('GET', '/v1/clubs/mine', null, a.token)).status, 401, 'token no longer valid');
  } finally { server.close(); }
});

test('bad input is rejected safely', async () => {
  const now = { t: Date.UTC(2027, 2, 3, 12) };
  const { server, call } = await start(now);
  try {
    const a = (await call('POST', '/v1/register', {})).body;
    assert.equal(a.name, 'Factory');
    const res = await fetch(server.address ? `http://127.0.0.1:${server.address().port}/v1/clubs/create` : '', {
      method: 'POST', headers: { authorization: `Bearer ${a.token}` }, body: '{not json',
    });
    assert.equal(res.status, 400);
  } finally { server.close(); }
});
