---
name: run-dourak
description: Use when the user asks to run, start, launch, or test the Dourak app locally (backend, frontend, or admin site). Covers starting Docker services and the Vite dev servers, and the known port-80 conflict on Windows.
---

# Running Dourak locally

## 1. Start Docker Desktop (if not already running)

```powershell
Start-Process 'C:\Program Files\Docker\Docker\Docker Desktop.exe'
```

Then wait for the daemon:

```bash
until docker info >/dev/null 2>&1; do sleep 3; done
```

## 2. Start the backing services

```bash
cd <repo-root>
docker compose up -d
```

This starts `postgres`, `api` (port 5000), `admin-web`, `seq` (port 5341), and `adminer`
(port 8081). **The `web` service will fail** with:

```
Error response from daemon: Ports are not available: exposing port TCP 0.0.0.0:80 -> 0.0.0.0:0:
listen tcp 0.0.0.0:80: bind: An attempt was made to access a socket in a way forbidden by its
access permissions.
```

This is expected on Windows — port 80 is reserved by a Windows service (not a real conflicting
process; `Get-NetTCPConnection -LocalPort 80` shows owning PID 4 = "System"). Ignore it: every
other service still starts fine, and `web`'s own Caddy config is domain-routed for production
anyway (`dourak.money`), not useful for `localhost` testing. Use the Vite dev server instead
(step 3).

If you also see `Container name "/dourak-seq" is already in use` on a fresh `docker compose up`,
a stale container from a previous session is holding the name — remove it and retry:

```bash
docker rm -f dourak-seq
docker compose up -d
```

## 3. Start the frontend dev servers (not `docker compose`'s `web`/`admin-web` — use these for local testing)

```bash
cd frontend && nohup npm run dev -- --port 5173 > /tmp/vite-frontend.log 2>&1 &
cd admin && nohup npm run dev -- --port 5174 > /tmp/vite-admin.log 2>&1 &
```

Frontend → http://localhost:5173, Admin → http://localhost:5174, API → http://localhost:5000
(Swagger at `/swagger`), Adminer → http://localhost:8081, Seq → http://localhost:5341.

If a port is already taken, Vite auto-increments (5173→5174→5175...) — check the printed URL in
the log rather than assuming the port you asked for.

## 4. Ready-made test accounts (no registration/email-verification needed)

Seeded automatically on API startup by `BetaUserSeeder` (`backend/src/Dourak.Infrastructure/
Identity/BetaUserSeeder.cs`), already `EmailConfirmed = true`:

| Email | Password |
|---|---|
| user1@dourak.test | Beta1234! |
| user2@dourak.test | Beta1234! |
| user3@dourak.test | Beta1234! |
| user4@dourak.test | Beta1234! |

`user1`/`user2` auto-accept into any circle they're added to (see `BetaTestUsers` in
`Dourak.Application`) — use `user3`/`user4` if you need to demonstrate a *pending* invite that
requires a manual Accept.

## 5. Rebuilding after a backend change

```bash
docker compose up -d --build api
```

(swap `api` for `admin-web`/`web` as needed). The frontend dev server picks up source changes
automatically — no restart needed for `frontend/`/`admin/` edits.
