// Shared validation: names (players, clubs), codes, numbers. Mirrors CompanyNameRules in the game.
const BLOCKED = ['fuck', 'shit', 'bitch', 'nazi', 'hitler', 'porn', 'sex', 'amk', 'aq', 'orospu', 'piç', 'sik', 'yarrak', 'göt', 'kahpe', 'pezevenk'];

export function sanitizeName(raw) {
  if (typeof raw !== 'string') return null;
  let out = '';
  let lastSpace = false;
  for (const ch of raw.trim()) {
    if (out.length >= 20) break;
    if (ch === ' ') { if (!lastSpace && out.length) { out += ' '; lastSpace = true; } continue; }
    if (/[\p{L}\p{N}.\-&']/u.test(ch)) { out += ch; lastSpace = false; }
  }
  out = out.trim();
  if (out.length < 3) return null;
  const lower = out.toLocaleLowerCase('tr');
  const squashed = lower.replace(/[^\p{L}\p{N}]/gu, '');
  if (BLOCKED.some(w => lower.split(/\s+/).includes(w) || (w.length >= 4 && squashed.includes(w)))) return null;
  return out;
}

export function code(len = 6) {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let s = '';
  for (let i = 0; i < len; i++) s += alphabet[Math.floor(Math.random() * alphabet.length)];
  return s;
}

export function nonNegativeInt(v, max = Number.MAX_SAFE_INTEGER) {
  const n = Number(v);
  if (!Number.isFinite(n) || n < 0) return null;
  return Math.min(Math.floor(n), max);
}
