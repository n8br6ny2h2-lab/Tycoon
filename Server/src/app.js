// Factory King reference backend (no dependencies). All endpoints are JSON under /v1.
// Privacy: players are anonymous (random id + token); only a display name chosen in-game is stored.
import crypto from 'node:crypto';
import { sanitizeName, code, nonNegativeInt } from './rules.js';

const WEEK_MS = 7 * 24 * 3600 * 1000;
const CLUB_MAX = 30;
const COOP_MAX = 8;
const MAX_UNITS_PER_REPORT = 1e15;

export function weekIndex(now) { return Math.floor((now + 3 * 24 * 3600 * 1000) / WEEK_MS); } // weeks start Monday 00:00 UTC

export function currentContract(now) {
  const week = weekIndex(now);
  const start = week * WEEK_MS - 3 * 24 * 3600 * 1000;
  const kinds = [
    { id: 'coop_rush', nameEn: 'Rush Order', nameTr: 'Acil Sipariş', goalPerMember: 50000 },
    { id: 'coop_mega', nameEn: 'Mega Delivery', nameTr: 'Dev Teslimat', goalPerMember: 120000 },
    { id: 'coop_export', nameEn: 'Export Deal', nameTr: 'İhracat Anlaşması', goalPerMember: 80000 },
  ];
  const k = kinds[week % kinds.length];
  return { ...k, id: `${k.id}_${week}`, week, start, end: start + WEEK_MS, rewardDiamonds: 60, rewardCrate: 'crate_big' };
}

export function createApp({ store, events, now = () => Date.now(), rateLimit = 120 }) {
  const hits = new Map();

  function player(req) {
    const auth = req.headers['authorization'] || '';
    const token = auth.startsWith('Bearer ') ? auth.slice(7) : null;
    if (!token) return null;
    const hash = crypto.createHash('sha256').update(token).digest('hex');
    return Object.values(store.data.players).find(p => p.tokenHash === hash) || null;
  }

  function limited(key) {
    const minute = Math.floor(now() / 60000);
    const k = `${key}:${minute}`;
    const n = (hits.get(k) || 0) + 1;
    hits.set(k, n);
    if (hits.size > 10000) hits.clear();
    return n > rateLimit;
  }

  const routes = {
    'GET /v1/time': () => [200, { utc: new Date(now()).toISOString(), ms: now() }],

    'GET /v1/events': () => [200, events],

    'POST /v1/register': (req, body) => {
      const id = crypto.randomUUID();
      const token = crypto.randomBytes(24).toString('hex');
      const name = sanitizeName(body.name) || 'Factory';
      store.data.players[id] = { id, name, tokenHash: crypto.createHash('sha256').update(token).digest('hex'), created: now(), club: null, coop: null };
      store.save();
      return [200, { playerId: id, token, name }];
    },

    'POST /v1/player/name': (req, body, p) => {
      const name = sanitizeName(body.name);
      if (!name) return [400, { error: 'invalid_name' }];
      p.name = name;
      store.save();
      return [200, { name }];
    },

    // Apple 5.1.1(v)-style deletion of all online data of this player.
    'DELETE /v1/player': (req, body, p) => {
      leaveClub(p);
      for (const c of Object.values(store.data.coops)) delete c.members[p.id];
      delete store.data.players[p.id];
      store.save();
      return [200, { deleted: true }];
    },

    // ---------------- co-op contracts ----------------
    'GET /v1/coop/contract': () => [200, currentContract(now())],

    'POST /v1/coop/create': (req, body, p) => {
      const contract = currentContract(now());
      let c;
      do { c = code(); } while (store.data.coops[c]);
      store.data.coops[c] = { code: c, contractId: contract.id, end: contract.end, members: {}, created: now() };
      store.data.coops[c].members[p.id] = { name: p.name, units: 0 };
      p.coop = c;
      store.save();
      return [200, coopView(store.data.coops[c], contract)];
    },

    'POST /v1/coop/join': (req, body, p) => {
      const c = store.data.coops[String(body.code || '').toUpperCase()];
      const contract = currentContract(now());
      if (!c || c.contractId !== contract.id) return [404, { error: 'not_found' }];
      if (!c.members[p.id] && Object.keys(c.members).length >= COOP_MAX) return [409, { error: 'full' }];
      c.members[p.id] = c.members[p.id] || { name: p.name, units: 0 };
      p.coop = c.code;
      store.save();
      return [200, coopView(c, contract)];
    },

    // Contribution is reported as the player's cumulative total for this co-op (idempotent, never decreases).
    'POST /v1/coop/contribute': (req, body, p) => {
      const c = p.coop && store.data.coops[p.coop];
      const contract = currentContract(now());
      if (!c || c.contractId !== contract.id) return [404, { error: 'no_coop' }];
      if (now() >= c.end) return [409, { error: 'ended' }];
      const total = nonNegativeInt(body.totalUnits, MAX_UNITS_PER_REPORT);
      if (total === null) return [400, { error: 'invalid_units' }];
      const m = c.members[p.id];
      m.units = Math.max(m.units, total);
      m.name = p.name;
      store.save();
      return [200, coopView(c, contract)];
    },

    'GET /v1/coop/mine': (req, body, p) => {
      const c = p.coop && store.data.coops[p.coop];
      const contract = currentContract(now());
      if (!c || c.contractId !== contract.id) return [200, { coop: null, contract }];
      return [200, coopView(c, contract)];
    },

    // ---------------- clubs ----------------
    'POST /v1/clubs/create': (req, body, p) => {
      const name = sanitizeName(body.name);
      if (!name) return [400, { error: 'invalid_name' }];
      if (Object.values(store.data.clubs).some(c => c.name.toLocaleLowerCase('tr') === name.toLocaleLowerCase('tr'))) return [409, { error: 'name_taken' }];
      leaveClub(p);
      let c;
      do { c = code(); } while (store.data.clubs[c]);
      store.data.clubs[c] = { code: c, name, owner: p.id, members: { [p.id]: { name: p.name, week: weekIndex(now()), xp: 0 } }, created: now() };
      p.club = c;
      store.save();
      return [200, clubView(store.data.clubs[c])];
    },

    'POST /v1/clubs/join': (req, body, p) => {
      const c = store.data.clubs[String(body.code || '').toUpperCase()];
      if (!c) return [404, { error: 'not_found' }];
      if (!c.members[p.id] && Object.keys(c.members).length >= CLUB_MAX) return [409, { error: 'full' }];
      if (p.club !== c.code) leaveClub(p);
      c.members[p.id] = c.members[p.id] || { name: p.name, week: weekIndex(now()), xp: 0 };
      p.club = c.code;
      store.save();
      return [200, clubView(c)];
    },

    'POST /v1/clubs/leave': (req, body, p) => { leaveClub(p); store.save(); return [200, { left: true }]; },

    'GET /v1/clubs/mine': (req, body, p) => {
      const c = p.club && store.data.clubs[p.club];
      return [200, c ? clubView(c) : { club: null }];
    },

    // Weekly XP of the player (cumulative within the week).
    'POST /v1/clubs/score': (req, body, p) => {
      const c = p.club && store.data.clubs[p.club];
      if (!c) return [404, { error: 'no_club' }];
      const xp = nonNegativeInt(body.weekXp, 10_000_000);
      if (xp === null) return [400, { error: 'invalid_xp' }];
      const m = c.members[p.id];
      const week = weekIndex(now());
      if (m.week !== week) { m.week = week; m.xp = 0; }
      m.xp = Math.max(m.xp, xp);
      m.name = p.name;
      store.save();
      return [200, clubView(c)];
    },

    'GET /v1/clubs/top': () => {
      const list = Object.values(store.data.clubs).map(clubView).sort((a, b) => b.weekScore - a.weekScore).slice(0, 50);
      return [200, { clubs: list }];
    },

    // User-generated names can be reported (Apple guideline 1.2); moderators review store.data.reports.
    'POST /v1/report': (req, body, p) => {
      const type = ['club', 'player'].includes(body.type) ? body.type : null;
      if (!type || typeof body.target !== 'string') return [400, { error: 'invalid_report' }];
      store.data.reports.push({ type, target: body.target.slice(0, 64), reason: String(body.reason || '').slice(0, 200), by: p.id, at: now() });
      store.save();
      return [200, { reported: true }];
    },
  };

  const PUBLIC = new Set(['GET /v1/time', 'GET /v1/events', 'POST /v1/register', 'GET /v1/coop/contract', 'GET /v1/clubs/top']);

  function leaveClub(p) {
    const c = p.club && store.data.clubs[p.club];
    if (c) {
      delete c.members[p.id];
      if (Object.keys(c.members).length === 0) delete store.data.clubs[c.code];
      else if (c.owner === p.id) c.owner = Object.keys(c.members)[0];
    }
    p.club = null;
  }

  function clubView(c) {
    const week = weekIndex(now());
    const members = Object.values(c.members).map(m => ({ name: m.name, weekXp: m.week === week ? m.xp : 0 })).sort((a, b) => b.weekXp - a.weekXp);
    return { code: c.code, name: c.name, members, weekScore: members.reduce((s, m) => s + m.weekXp, 0), max: CLUB_MAX };
  }

  function coopView(c, contract) {
    const members = Object.values(c.members).map(m => ({ name: m.name, units: m.units })).sort((a, b) => b.units - a.units);
    const goal = contract.goalPerMember * Math.max(2, members.length);
    const total = members.reduce((s, m) => s + m.units, 0);
    return { code: c.code, contract, members, total, goal, completed: total >= goal, max: COOP_MAX };
  }

  return async function handle(req, res) {
    const url = new URL(req.url, 'http://localhost');
    const key = `${req.method} ${url.pathname}`;
    const send = (status, obj) => {
      res.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
      res.end(JSON.stringify(obj));
    };
    try {
      const route = routes[key];
      if (!route) return send(404, { error: 'unknown_route' });
      if (limited(req.socket?.remoteAddress || 'local')) return send(429, { error: 'rate_limited' });
      let body = {};
      if (req.method === 'POST') {
        const chunks = [];
        let size = 0;
        for await (const chunk of req) { size += chunk.length; if (size > 16 * 1024) return send(413, { error: 'too_large' }); chunks.push(chunk); }
        if (chunks.length) { try { body = JSON.parse(Buffer.concat(chunks).toString('utf8')); } catch { return send(400, { error: 'bad_json' }); } }
      }
      let p = null;
      if (!PUBLIC.has(key)) {
        p = player(req);
        if (!p) return send(401, { error: 'unauthorized' });
      }
      const [status, payload] = route(req, body, p);
      return send(status, payload);
    } catch (e) {
      return send(500, { error: 'server_error' });
    }
  };
}
