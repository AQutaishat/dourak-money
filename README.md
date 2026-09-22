# Dourak — دورك

A simple, Arabic-first organizer for **جمعية / Savings Circles (ROSCA)**. Phase 1 is a
tracking and coordination tool for one organizer — it never holds, transfers, or
processes money.

## Status: Phase 2 — member self-service & invitations (see `docs/prompt02.md`, `docs/progress.md`)

Phase 2 adds: adding members by searching registered users, invitation accept/decline,
view-only circle access for accepted members, member payment self-reporting with evidence
subject to organizer approval, and a user profile with name/phone.

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
The API listens on `http://localhost:5210` by default (see `Properties/launchSettings.json`;
adjust via `--urls`) with Swagger at `/swagger` in Development. Migrations also apply automatically on startup (see `Program.cs`) —
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
Runs on `http://localhost:5173`. Set `VITE_API_BASE_URL` in `.env` (e.g.
`http://localhost:5210/api`) if the API isn't on `http://localhost:5080/api`, the
axios client's fallback default.

### 4. Everything via Docker
```bash
docker compose up -d
```
Builds and runs Postgres, the API (port 5000), the frontend (port 80), Adminer, and Seq
(see [Logging](#logging) below).

### 5. Browsing the database (Adminer)

`docker compose` runs **Adminer** on `http://localhost:8081`. Log in with
`System: PostgreSQL · Server: postgres · Username: dourak · Database: dourak`.

Adminer was chosen over pgAdmin because it is a single ~10 MB stateless container whose
login *is* the Postgres login — pgAdmin is ~600 MB, stateful, and adds a second set of
credentials to manage.

**On the production server it is bound to loopback only** (`127.0.0.1:8081`), so it is not
reachable from the internet. Reach it through an SSH tunnel:

```bash
ssh -L 8081:127.0.0.1:8081 <user>@<server>   # then open http://localhost:8081
```

This is a deliberate tradeoff: Adminer's login form accepts database credentials, so
exposing it publicly without rate limiting or MFA would be one weak password away from full
data access. See the comment in `docker-compose.yml` for how to widen it safely (behind the
existing Caddy reverse proxy with HTTPS + basic auth) if that's ever needed.

## Logging

The API uses **Serilog** (replacing the default logging provider entirely). Every
sink — which ones are active and their args — comes entirely from the `Serilog:WriteTo`
array in `appsettings.json` / `appsettings.Development.json`, read via
`Serilog.Settings.Configuration`'s `ReadFrom.Configuration(...)`; `Program.cs` has no
sink construction or conditional logic in code at all:

- **Console** — always on; what you see in the terminal when running `dotnet run`.
- **Rolling file** — always on; `backend/src/Dourak.Api/logs/dourak-YYYYMMDD.log`
  (daily, 14 days retained; gitignored). This is what persists once the terminal
  closes, since console output otherwise disappears with it.
- **Seq** — a structured log server you can query/filter in a browser. Off in the
  base `appsettings.json` (Production); `appsettings.Development.json` adds it back
  as a third `WriteTo` entry for local dev, and `docker-compose.yml` adds it back for
  containerized runs via `Serilog__WriteTo__2__Name`/`__Args__serverUrl` env vars —
  the same array-via-env-var pattern already used for `Cors__AllowedOrigins__0`.
  Either way, it's a pure configuration difference between environments, not code
  deciding whether Seq is reachable.

Every HTTP request also gets one structured log line (method, path, status, elapsed ms)
via `UseSerilogRequestLogging()`.

**Running Seq locally, alongside `dotnet run` (outside `docker compose`):**
```bash
docker run -d --name dourak-seq -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_NOAUTHENTICATION=true -p 127.0.0.1:5341:80 datalust/seq:latest
```
`appsettings.Development.json` already points at `http://localhost:5341`, so logs show
up there as soon as the container is running — no other config needed.
`SEQ_FIRSTRUN_NOAUTHENTICATION` skips Seq's own login; acceptable here only because
the port is bound to loopback (see below), not exposed to anything else.

**Via `docker compose`:** a `seq` service is already included and the `api` service adds
it as a `WriteTo` entry via `Serilog__WriteTo__2__Args__serverUrl: http://seq:80` — Seq's
*container* port is `80`; `5341` is only the host-side port mapping. Just
`docker compose up -d` and open `http://localhost:5341`.

**Security note (same tradeoff as Adminer above):** Seq has no authentication of its
own here, so both locally and **in production** it is bound to `127.0.0.1` only —
never reachable from the internet. On the Oracle server, reach it over an SSH tunnel:
```bash
ssh -L 5341:127.0.0.1:5341 <user>@<server>   # then open http://localhost:5341
```
If public access is ever genuinely needed, put it behind the existing Caddy reverse
proxy with HTTPS + basic auth first, and only then widen the port binding.

### Payment-claim evidence storage

Phase 2 evidence files (images/PDFs attached to a member's payment self-report) are written
to disk by the API under `Storage:EvidencePath` — `/app/data/evidence` in Docker, mounted as
the `dourak-evidence-data` volume so uploads survive `docker compose up -d --build`. Limits:
5 MB per file, images and PDFs only; the database stores only the reference.

## Admin site

`admin/` is a **separate codebase** from `frontend/` — its own `package.json`, own React app,
own Docker build — deployed under its own subdomain (`admin.dourak.money`). It shares the
same backend/database as the main app rather than having its own user store: access is
gated by an **`Admin` Identity role** on a normal user account, not a separate login system.

- **Pages**: a dashboard (total user/circle counts) and a users-management page (every user,
  their circles organized/joined, email-verified status, and admin actions: reset password,
  deactivate/reactivate, delete).
- **Auth**: logs in via the same `/api/auth/login` endpoint as the main app; the JWT only
  grants access to `/api/admin/*` endpoints if the account has the `Admin` role (enforced
  server-side via `[Authorize(Roles = "Admin")]` — the admin site's own login screen also
  checks this client-side for a clearer error message, but that check is UX only).
- **Getting an admin account**: set `ADMIN_1_EMAIL`/`ADMIN_1_PASSWORD` (and `ADMIN_2_*`/
  `ADMIN_3_*` for more — see `.env.example`) in `.env` — `AdminSeeder` (runs on every API
  startup, idempotent) creates each account if it doesn't exist and/or grants it the `Admin`
  role. To promote an *existing* Dourak user instead of creating a new account, set their
  email with no password — only the role is added, their password is untouched.
- **Networking**: not published on the host at all — the main `web` service's Caddy
  reverse-proxies `admin.dourak.money` to the internal `admin-web` container (see
  `frontend/Caddyfile`), and `admin-web`'s own Caddy proxies its `/api/*` calls to the `api`
  container the same way `frontend/Caddyfile` does — so the admin site never makes a
  cross-origin request and needs no CORS entry.
- **DNS**: needs its own `A` record — `admin.dourak.money` → the server's IP (in Cloudflare,
  alongside the existing `@`/`www` records) — before Caddy can obtain its Let's Encrypt cert
  for it.

```bash
cd admin
npm install
npm run dev       # local dev server, calls VITE_API_BASE_URL or /api
npm run build     # production build (also run by admin/Dockerfile)
```

## MCP server (AI assistant access)

Dourak exposes a [Model Context Protocol](https://modelcontextprotocol.io) server so an AI
assistant (Claude Desktop, ChatGPT custom connectors, etc.) can read and act on a *user's own*
circles on their behalf — there's no separate service or deployment step; it's mounted inside
the same API at **`https://dourak.money/api/mcp`** and already live in production.

- **Tools available** (`backend/src/Dourak.Api/Mcp/DourakMcpTools.cs`) — every call is scoped
  to whichever Dourak account the client authenticated as (resolved from the JWT, never passed
  as a parameter), so a connected assistant can only ever see/act on that one user's data:
  - *Read*: `get_my_circles`, `get_circle_details`, `get_current_cycle_status`,
    `get_circle_members`, `get_circle_history`, `get_pending_invitations`,
    `get_my_payment_claims`, `get_my_payment_reminders`.
  - *Write*: `create_circle`, `add_circle_member`, `activate_circle`, `submit_payment_claim`,
    `withdraw_payment_claim`, `set_payment_reminder`, `remove_payment_reminder`.
- **Connecting a client that supports MCP's OAuth flow (e.g. Claude Desktop's "Connect" custom
  connector)**: just point it at `https://dourak.money/api/mcp`. The server implements the full
  discovery/auth spec it expects — RFC 7591 dynamic client registration, RFC 8414 / RFC 9728
  metadata, PKCE S256, no client secret (`backend/src/Dourak.Api/Controllers/OAuthController.cs`)
  — so the client self-registers, opens a plain login page, and the user signs in with their
  **normal Dourak email + password** right there. Access tokens are the same short-lived JWTs
  `/api/auth/login` issues; refresh tokens are opaque, rotated on every use, 90-day lifetime.
- **Connecting a client that only accepts a bearer token (e.g. ChatGPT's custom-connector
  form)**: get a token from `POST /api/auth/login` (the same call the web app makes) and paste
  it in directly — this bypasses the OAuth server entirely, since ChatGPT has no field for the
  authorization-code flow above.
- **Nothing to configure**: OAuth clients register themselves per-connection (no pre-shared
  `client_id`); metadata is built from the same `App__FrontendBaseUrl` the rest of the app
  already uses, and Caddy already routes `/api/*` plus the two `.well-known` OAuth metadata
  paths in production — connecting "just works" against the URL above.
- **Known limitation**: no way yet to revoke a connected client's refresh token from the UI
  (e.g. a "disconnect this app" button on the profile page) — see `docs/future-work.md`.

## Deployment (GitHub Actions)

`.github/workflows/deploy.yml` deploys to the production Oracle server automatically
on every push to `main` (or manually via the Actions tab → "Deploy to production" →
Run workflow). It runs `dotnet test` and `npm run build` as gates first — a commit
that fails either never reaches the server — then SSHes in and re-runs the same
`git pull --ff-only && docker compose up -d --build` steps you'd otherwise run by hand.

**One-time setup**, in the GitHub repo → Settings → Secrets and variables → Actions
→ New repository secret, add:

| Secret | Value |
|---|---|
| `ORACLE_HOST` | The server's public IP or hostname (e.g. `84.13.136.178`) |
| `ORACLE_USERNAME` | The SSH user (e.g. `ubuntu`) |
| `ORACLE_SSH_KEY` | The **private** key (full contents, e.g. of `dourak_oracle`) whose matching public key is already in the server's `~/.ssh/authorized_keys` — generate a dedicated deploy key rather than reusing your personal one if you'd rather be able to revoke it independently |
| `ORACLE_DEPLOY_PATH` | Absolute path to the cloned repo on the server (e.g. `/home/ubuntu/dourak-money`) |

The server's `.env` (JWT secret, etc. — see "Local setup" → step 2 above) is untouched
by this workflow; it's gitignored and already persists across `git pull` on the server.

## Tests
```bash
cd backend
dotnet test
```
**55 tests.** 34 Domain tests cover: random-draw completeness/fairness, payout-order
uniqueness, schedule generation, activation locking, member replacement, contribution/late-status
derivation, plus the Phase 2 invitation lifecycle (accept/decline/re-invite, declined-member
exclusion) and payment-claim review rules. 21 Application tests cover the full organizer
journey end-to-end (create → members → order → activate → contributions → payout → history)
plus the Phase 2 flows — invite by user search, accept/decline, payment self-report with
approval/rejection, claim privacy, circle deletion, and name/phone uniqueness — against a
real EF Core model.

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

## Phase 2 business rules (enforced in code)

9. A member linked to a registered user must **accept** their invitation before they count as
   part of the circle; **declined** members are excluded exactly like deactivated ones
   (`CircleMember.IsParticipating`, used everywhere the aggregate builds the order/schedule).
10. A member can self-report **only their own** payment — the contribution is resolved from the
    caller's own member row, so there is no member id to spoof — and only one claim may be open
    per contribution at a time.
11. A self-reported payment is **not** paid until the organizer approves it; approval routes
    through `Contribution.RecordPayment`, so rule #6 still holds on that path.
12. A payment claim, its evidence and its rejection are visible **only** to the submitting
    member and the organizer. Payment *status* stays visible to every member.
13. Accepted members get **read-only** access to their whole circle; every mutation stays
    organizer-only (`ICircleReadRequest` vs. `ICircleOwnedRequest`).
14. Members never see other members' phone/email; they do see who the organizer is.
15. A user's name and phone are optional but **unique when set**, compared on a normalized
    value (case-insensitive name, formatting-stripped phone) in both the app and the database.
16. A circle can be deleted only while it has **zero recorded payments** — rule #5's "never
    hard-delete financial history" is preserved by never offering deletion after that point.

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
