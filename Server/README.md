# Factory King reference server

Optional backend for the online features. **The game works fully without it.** Online features stay switched off while `FactoryKing/Assets/_Project/Resources/Online/online.txt` has an empty `baseUrl`.

| Feature | Endpoints |
|---|---|
| Server time (anti clock-cheat) | `GET /v1/time` |
| Remote event schedule (new events without an app update) | `GET /v1/events` → `config/events.json` |
| Anonymous player | `POST /v1/register`, `POST /v1/player/name`, `DELETE /v1/player` (deletes all online data) |
| Co-op contracts (weekly, up to 8 players) | `GET /v1/coop/contract`, `POST /v1/coop/create`, `POST /v1/coop/join`, `POST /v1/coop/contribute`, `GET /v1/coop/mine` |
| Clubs (up to 30, weekly score) | `POST /v1/clubs/create`, `/join`, `/leave`, `/score`, `GET /v1/clubs/mine`, `GET /v1/clubs/top` |
| Reporting club/player names | `POST /v1/report` (stored for moderators) |

## Run locally
```bash
cd Server
npm test        # 5 tests
npm start       # http://localhost:8080   (PORT, FK_DATA env vars)
```
Requires Node.js 20 or newer. There are no dependencies.

## Before production (needs your decisions)
- **Hosting:** any Node host works (Render, Fly.io, Railway, a small VPS). Put it behind HTTPS and set `baseUrl` in `online.txt` to the HTTPS address.
- **Database:** the JSON file store is fine for testing and small player counts. For more players, replace `src/store.js` with Postgres, Redis or Firestore.
- **Moderation:** club names are filtered and can be reported (Apple guideline 1.2 for user-generated content). Someone needs to review `reports` and remove offending clubs. There is no chat, on purpose.
- **Trust:** contributions are reported by the client and only sanity-checked. Server-side validation would need server-authoritative economy, which is a large project.
- **Privacy:** only a random id, a token hash and the chosen company/club names are stored. Update the App Privacy answers and the privacy policy as described in `Docs/APPSTORE/AppPrivacy.md`.
