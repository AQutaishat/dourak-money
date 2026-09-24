# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## What this is

Dourak (دورك) — an Arabic-first organizer for جمعية / savings circles (ROSCA). Phase 1+2:
tracking/coordination tool with organizer accounts, member self-service (invitations, payment
self-reporting), and an admin site. It never holds, transfers, or processes money itself.

Four codebases in this repo, each independent:
- `backend/` — ASP.NET Core 10 Web API (Clean Architecture)
- `frontend/` — React 19 + TypeScript SPA (the main app, dourak.money)
- `admin/` — separate React app for admin.dourak.money (own package.json/build, shares the backend)
- `mobile/` — Flutter/Android mirror of the web app

## Commands

### Backend
```bash
cd backend
dotnet run --project src/Dourak.Api          # API on http://localhost:5210, Swagger at /swagger (Dev)
dotnet test                                    # full suite (55 tests)
dotnet test --filter "FullyQualifiedName~SavingsCircleTests"   # single test class
dotnet ef database update --project src/Dourak.Infrastructure --startup-project src/Dourak.Api
```
Migrations also auto-apply on API startup (`Program.cs`). Requires Postgres running
(`docker compose up -d postgres`).

### Frontend / Admin
```bash
cd frontend  # or cd admin
npm install
npm run dev       # frontend :5173, admin :5174 if run alongside
npm run build      # tsc -b && vite build
npm run lint       # oxlint (frontend only — admin has no lint script)
```
Set `VITE_API_BASE_URL` in `.env` if the API isn't at the axios client's default.

### Mobile
```bash
cd mobile
flutter run -d chrome --web-port=8090   # pinned port — already whitelisted in backend CORS
```

### Everything via Docker
```bash
docker compose up -d
```
Runs Postgres, API (:5000), main web + admin-web behind Caddy (:80/:443), Adminer (:8081),
Seq (:5341). **On Windows, the `web` service fails to bind port 80** (reserved by a Windows
service) — expected; every other service still starts. Use the Vite dev servers for local
frontend/admin testing instead of the Docker `web`/`admin-web` containers (their Caddy config
is domain-routed for production). See the `run-dourak` skill for full local-run details
(seeded test accounts, rebuild steps, etc.) and `deploy-dourak` for deploying to production.

## Architecture

### Backend: Clean Architecture, one direction of dependency
```
Dourak.Domain          — entities, enums, business rules. No external dependencies.
Dourak.Application     — MediatR commands/queries, validators (FluentValidation), DTOs, interfaces
Dourak.Infrastructure  — EF Core, PostgreSQL, ASP.NET Identity, JWT, email, storage
Dourak.Api             — controllers, DI wiring, Swagger, middleware, MCP server, OAuth
```

**Business rules live entirely in the `SavingsCircle` aggregate** (`Dourak.Domain.Entities`):
payout-order uniqueness, random-draw fairness, schedule generation, activation locking, member
replacement, invitation lifecycle. Application-layer handlers are thin orchestration — load the
aggregate, call one domain method, save. When changing circle behavior, look there first, not
in the handler.

**No repository/unit-of-work layer** on top of EF Core — `DourakDbContext` already is one.

**Authorization is enforced by a single MediatR pipeline behavior**, not per-handler checks:
`CircleOwnershipBehavior` (`Dourak.Application/Common/Behaviors`) enforces "only the organizer
who owns this circle can act on it." Requests implement `ICircleOwnedRequest` (mutation,
organizer-only) or `ICircleReadRequest` (read, organizer or accepted member) to opt into this —
don't hand-roll ownership checks inside a handler.

**Key derived/invariant rules to preserve when touching this code** (see `SavingsCircle` for
authoritative enforcement):
- "Late" status is always derived from due date + balance, never stored.
- Payout order locks on activation; before that it resets freely.
- Members with financial history are deactivated, never deleted; a circle can only be deleted
  with zero recorded payments.
- A member can self-report only their own payment (contribution resolved from the caller's own
  member row — no member id param to spoof); only one open claim per contribution.
- Self-reported payments route through `Contribution.RecordPayment` on organizer approval, so
  the "paid can't exceed expected" rule still applies.
- Payment claims/evidence/rejections are visible only to the submitting member + organizer;
  payment *status* is visible to all members. Members never see other members' phone/email.
- All money columns are `decimal(18,2)`.

**Logging**: Serilog entirely config-driven via `Serilog:WriteTo` in `appsettings*.json` /
env vars (`Serilog__WriteTo__N__...`) — `Program.cs` has no sink construction or conditionals.
Sinks: console, rolling file (`Dourak.Api/logs/`), optional Seq.

**MCP server**: mounted inside the API at `/api/mcp` (`Dourak.Api/Mcp/DourakMcpTools.cs`),
scoped to the caller's own JWT-resolved account. Full OAuth 2.0 dynamic-client-registration
flow lives in `Dourak.Api/Controllers/OAuthController.cs`.

### Frontend (`frontend/src/`)
```
api/       — typed API client (axios + TanStack Query)
auth/      — auth context (JWT in localStorage)
i18n/      — Arabic/English dictionaries (react-i18next)
theme/     — MUI v6 theme + RTL emotion cache (stylis-plugin-rtl)
pages/     — one folder per screen
```
RTL and i18n are first-class — the app is Arabic-first with English as a second locale; any
new UI text goes through `i18n/`, and layout must work mirrored under `theme/`'s RTL cache.

### Admin (`admin/`)
Genuinely separate codebase (own `package.json`, own React app, own Dockerfile), not a route
inside `frontend/`. Shares the same backend/DB — access is gated by an `Admin` Identity role on
a normal user account (`[Authorize(Roles = "Admin")]` server-side), not a separate login system.
In production it's reverse-proxied by the main `web` container's Caddy so it's same-origin to
the API (no CORS entry needed); it is never published directly on the host.

### Mobile (`mobile/lib/`)
```
api/ auth/ screens/ state/ theme/ utils/ widgets/ l10n/
```
Riverpod for state, go_router for navigation, dio for HTTP — a feature-mirror of the web
frontend against the same backend API, not an independently-designed app.

## Production access

Production server/SSH/DB credentials are in `docs/credentials/credential.md` — use them
directly to connect (e.g. SSH to the Oracle server, Adminer/Seq tunnels) instead of asking
the user for credentials each time.

## Working docs

`docs/` contains living project documents — check these before starting non-trivial work:
- `docs/working-docs/progress.md` — running log of what's shipped, by phase.
- `docs/working-docs/future-work.md` — known gaps / deliberately deferred work.
- `docs/conventions.md` — rules for `docs/promptNN.md` scope documents (if you're handed one:
  re-read all of `docs/` first, execute immediately without a plan/approval pause, log progress
  in `progress.md` under a phase heading, mark requirements `[DONE]` inline).
- `docs/project-docs/` — BRD, competitor studies — source of truth for product scope/decisions.
