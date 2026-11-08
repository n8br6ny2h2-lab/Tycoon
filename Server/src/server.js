import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createApp } from './app.js';
import { Store } from './store.js';

const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const events = JSON.parse(fs.readFileSync(path.join(root, 'config', 'events.json'), 'utf8'));
const store = new Store(process.env.FK_DATA || path.join(root, 'data', 'db.json'));
const port = Number(process.env.PORT || 8080);

http.createServer(createApp({ store, events })).listen(port, () => console.log(`Factory King server on :${port}`));
process.on('SIGTERM', () => { store.flush(); process.exit(0); });
