# Dourak — دورك

A simple, Arabic-first organizer for **جمعية / Savings Circles (ROSCA)**. Phase 1 is a
tracking and coordination tool for one organizer — it never holds, transfers, or
processes money.

## Status: Phase 1 (see [Definition of Done](#definition-of-phase-1-done) below)

## Architecture

Clean Architecture backend + a React SPA frontend.

```
backend/
  src/
    Dourak.Domain          — entities, enums, business rules (no external dependencies)
    Dourak.Application     — MediatR commands/queries, validators, DTOs, interfaces
    Dourak.Infrastructure  — EF Core, PostgreSQL, ASP.NET Identity, JWT
    Dourak.Api             — controllers, DI wiring, Swagger, middleware
  tests/
    Dourak.Domain.Tests        — unit tests for business rules (draw fairness, uniqueness, schedule generation...)
    Dourak.Application.Tests   — end-to-end handler tests against EF Core (in-memory)
frontend/
  src/
    api/          — typed API client (axios + React Query)
    auth/          — auth context (JWT in localStorage)
    i18n/          — Arabic/English dictionaries (react-i18next)
    theme/         — MUI theme + RTL emotion cache
    pages/         — one folder per screen
```

### Why this shape

- **Business rules live in `Dourak.Domain`** (`SavingsCircle` aggregate): payout-order
  uniqueness, random-draw fairness, schedule generation, locking, member replacement.
  Application handlers are thin orchestration around the aggregate — they load it,
  call one domain method, save.
- **No repository/unit-of-work layer** on top of EF Core — `DourakDbContext` already
  is one; adding another layer would be ceremony without benefit for this MVP.
- **MediatR** keeps each use case in its own small, testable class instead of fat
  controllers, without pulling in CQRS/event-sourcing machinery Phase 1 doesn't need.
- **A single pipeline behavior (`CircleOwnershipBehavior`)** enforces "only the
  organizer who owns this circle can act on it" everywhere, instead of repeating an
  `if (circle.OrganizerUserId != currentUser.UserId)` check in every handler.

## Tech stack

| Concern | Choice | Why |
|---|---|---|
| Backend | ASP.NET Core 10 (LTS) Web API | Current LTS (supported through Nov 2028), strong typing, mature EF Core, good AI-agent support |
| Database | PostgreSQL | Free, solid decimal/date handling, easy to self-host |
| ORM | EF Core 10 (code-first migrations) | No custom SQL needed for Phase 1's relational model |
| Auth | ASP.NET Identity + JWT bearer | Organizer-only auth; no cookies/session server needed for an SPA |
| Validation | FluentValidation | Declarative, testable, integrates with MediatR pipeline |
| Backend tests | xUnit + FluentAssertions + EF InMemory | Fast, no external DB needed for handler tests |
| Frontend | React 19 + TypeScript (Vite) | Fast dev loop, huge ecosystem, good AI-agent support |
| UI kit | MUI v6 | Built-in RTL support, accessible components, fast to build forms/tables with |
| Data fetching | TanStack Query | Caching/invalidation without hand-rolled state management |
| i18n | react-i18next | Mature, small, easy Arabic/English + RTL switching |

## Local setup

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- Docker (for PostgreSQL) — or a local PostgreSQL instance

### 1. Database
```bash
docker compose up -d postgres
```

### 2. Backend
```bash
cd backend
dotnet ef database update --project src/Dourak.Infrastructure --startup-project src/Dourak.Api
dotnet run --project src/Dourak.Api
```
The API listens on `http://localhost:5080` (adjust via `--urls`) with Swagger at `/swagger`
in Development. Migrations also apply automatically on startup (see `Program.cs`) —
the explicit `dotnet ef database update` above is only needed if you want to apply them
without starting the API.

Update `src/Dourak.Api/appsettings.json` (or an environment variable /
`appsettings.Development.json`) with your own `Jwt:Secret` before any real deployment —
the checked-in value is a placeholder.

**For the docker-compose deployment specifically:** `docker-compose.yml` already
reads the JWT secret from a `JWT_SECRET` environment variable
(`Jwt__Secret: "${JWT_SECRET:-CHANGE_ME_...}"`), falling back to the placeholder
only if it isn't set. Copy `.env.example` to `.env` (in the same directory as
`docker-compose.yml`) and set a real secret there — generate one with
`openssl rand -base64 64`. `.env` is gitignored, so it survives every
`git pull` + `docker compose up -d --build` untouched; you only need to set it
once per server, not on every deploy.

### 3. Frontend
```bash
cd frontend
npm install
npm run dev
```
Runs on `http://localhost:5173`. Set `VITE_API_BASE_URL` in `.env` if the API isn't on
`http://localhost:5080/api`.

### 4. Everything via Docker
```bash
docker compose up -d
```
Builds and runs Postgres + the API (port 5000). Run the frontend separately with `npm run dev`
for now (a frontend Dockerfile/nginx step can be added when there's a real deployment target).

## Tests
```bash
cd backend
dotnet test
```
21 Domain tests cover: random-draw completeness/fairness, payout-order uniqueness,
schedule generation, activation locking, member replacement, contribution/late-status
derivation. 3 Application tests cover the full organizer journey end-to-end (create →
members → order → activate → contributions → payout → history) against a real EF Core
model.

## Phase 1 business rules (enforced in code)

See `Dourak.Domain.Entities.SavingsCircle` for the authoritative implementation. Summary:

1. Every active member appears exactly once in the payout order (domain validation + unique DB index).
2. Every cycle has exactly one recipient (FK + one-per-sequence unique index).
3. Expected pool = active members × contribution amount, computed at schedule generation.
4. The payout order locks on activation; before that it can be freely reset.
5. Members with financial history are deactivated, never deleted.
6. Contribution paid amount can't exceed the expected amount (partial payments supported).
7. "Late" status is derived from due date + outstanding balance, never stored — it can't drift.
8. All money columns are `decimal(18,2)` — no floating point.

## Phase 1 Business Decisions

These resolve the BRD's open questions with the simplest safe rule for v1 (see the
in-code comments on `SavingsCircle` for where each is enforced):

- **Member leaves after start** → deactivate only; history stays attached to the member record.
- **Replace a member** → supported, but only for a *future* (not-yet-paid-out) payout position; past cycles keep the original member.
- **Change contribution amount after activation** → not supported; cancel and recreate the circle.
- **Change payout order after activation** → locked; only future positions can be reassigned via replace-member.
- **Payout before everyone paid** → allowed, with a UI warning; Dourak only records what the organizer tells it.
- **Multiple payout positions per member** → not supported in Phase 1.
- **Pay future cycles in advance** → not supported; one contribution record per member per cycle.
- **Skip a cycle** → not supported; cycles are tracked, not skipped.
- **Add a member after activation** → not supported; membership and schedule freeze together at activation.
- **Reverse a completed payout** → supported via "reopen" (sets it back to Pending) for correcting mistakes.
- **Organizer as a member** → optional, supported at circle creation.

## Known limitations / future-phase boundaries

Deliberately **not** built in Phase 1 (see BRD §12 and the competitor studies):
member self-service accounts, invitation links, payment-proof uploads, multiple
organizers, push/SMS/email reminders (Phase 1 uses copy/share-to-WhatsApp text only),
trust/reputation scoring, PDF/Excel export, weekly/biweekly frequencies, any money
movement (wallet, gateway, bank integration, KYC, loans, investments), public/discoverable
circles.

## Definition of Phase 1 Done

An organizer can, end to end, without a spreadsheet: register/login, create a circle,
add members, set the contribution amount/currency/start date, define the payout order
manually or by random draw, confirm and lock it, activate the circle, view the generated
schedule, record full/partial contributions, see paid/unpaid/late/collected/expected/
outstanding at a glance, record the payout, see current/next recipient, view member and
circle history, and share a status update to WhatsApp — in Arabic or English. This has
been verified against a live PostgreSQL-backed API (see the domain/application test
suite and the manual end-to-end HTTP smoke test performed during development).
