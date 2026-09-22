# Dourak — Future Work (Deferred Scope)

Items deliberately pushed out of the current phase(s), tracked here so they're
not forgotten but also don't creep into active scope uninvited. Nothing here
should be built unless a future prompt explicitly pulls it back into scope.

## Notifications: WhatsApp API + push notifications

- **WhatsApp Business API integration** — today "WhatsApp" support is just a
  share-link (`wa.me/...`) the organizer taps to manually send an invite text
  they still have to compose/send themselves; there's no real integration.
  A real integration would use the WhatsApp Business Platform (Cloud API) to
  actually *send* messages from Dourak itself — e.g. an invite arriving
  automatically when a member is added, or a payment-due reminder sent over
  WhatsApp instead of (or alongside) email. Needs a Meta developer/business
  account, a verified WhatsApp Business phone number, and pre-approved
  message templates (Meta requires template approval for anything outside a
  live 24-hour conversation window) — none of that exists yet. Moderate
  effort: mostly account/template setup plus one new `IWhatsAppSender`-style
  seam in `Dourak.Infrastructure` next to the existing `IEmailSender`.
- **Push notifications** — no push channel exists on any platform yet
  (`docs/future-work.md`'s Phase-1-carryover list already noted this as
  out of scope; promoted here since it's now being actively considered).
  Needs Firebase Cloud Messaging (works for both Android and, later, iOS),
  which the mobile app doesn't currently depend on at all (no Firebase
  project exists — see the analytics section above, which would also want
  one for Crashlytics, so doing both together is worth considering). Would
  give the existing `PaymentReminderBackgroundService` (currently email-only,
  see `Dourak.Infrastructure/Reminders`) a second delivery channel with no
  change to its scheduling logic — just an additional `INotificationSender`
  implementation alongside `IEmailSender`. The web app has no equivalent
  push mechanism (browser push/service workers) and would need its own,
  separate effort if push there is ever wanted too.

## Observability: error/crash tracking — higher priority than analytics below

- **No error/crash tracking exists anywhere in production** — not on the web
  app, the admin site, the mobile app, or the API beyond raw Serilog/Seq logs
  (which nobody is alerted by; someone has to think to go look). Practically
  this means a real bug affecting real users — a web page throwing in
  production, a mobile crash, an unhandled API exception — can go completely
  unnoticed indefinitely unless a user happens to complain. This should be
  treated as more urgent than the analytics items below, since it's about
  *knowing something is broken* rather than *understanding usage*.
  - **Sentry** is the natural fit: one SDK family across React (`frontend/`),
    Flutter (`mobile/`), and ASP.NET Core (the API), with a free tier that's
    almost certainly enough at current scale. Gives stack traces, breadcrumbs,
    release tracking, and (for mobile) crash reporting, plus optional email/
    Slack alerting so issues surface immediately instead of being found by
    accident.
  - Low-to-moderate effort: mostly SDK install + init per app, no
    architectural change needed anywhere.
  - **Azure Application Insights** is the other strong option, especially
    for the backend: near-zero-config with ASP.NET Core
    (`AddApplicationInsightsTelemetry()`), and goes beyond error tracking
    into full APM — request/dependency tracing (e.g. seeing exactly which
    EF Core query or outbound call made a request slow), live metrics, and
    availability/uptime tests hitting `dourak.money` from outside. Sentry
    and App Insights overlap heavily on the error-tracking piece; App
    Insights is the deeper pick if request-performance visibility (not just
    crashes) turns out to matter, Sentry if a single unified SDK across
    React/Flutter/ASP.NET Core matters more. Worth trialing one rather than
    running both — pick based on which gap (crashes vs. performance) is
    actually felt once real users exist.
  - **Add distributed tracing to the Seq instance already running** —
    cheaper than either of the above since Seq is already deployed
    (`logs.dourak.money`) and accepts OpenTelemetry traces natively over
    OTLP: add the `OpenTelemetry.Extensions.Hosting` +
    `OpenTelemetry.Instrumentation.AspNetCore`/`.Http` packages to
    `Dourak.Api`, call
    `AddOpenTelemetry().WithTracing(...).AddOtlpExporter(...)` pointed at
    Seq's OTLP ingestion endpoint. Gets per-request traces (including
    outbound HTTP calls) alongside the structured logs Seq already has,
    without standing up a new service or account — the smallest-effort
    step of the three, though it doesn't cover the frontend/mobile crash
    side the way Sentry would.

## Analytics / Product Insight

- **Google Analytics 4 integration** — no analytics of any kind exist yet on
  the web app or admin site (mobile has no analytics SDK either). GA4 is the
  free, standard choice and would need: a `gtag.js`/`react-ga4` snippet on
  `frontend/` (page views + a handful of custom events — circle created,
  member added, circle activated, payment claim submitted), and for mobile
  the `firebase_analytics` Flutter package (needs a Firebase project, which
  also unlocks Crashlytics for free — worth doing alongside this rather than
  separately, though Sentry above is the more complete option). Low effort,
  mostly config not code.
  - **Alternatives worth considering instead of/alongside GA4**:
    - **PostHog** (self-hostable or cloud) — product analytics + session
      replay + feature flags in one tool, more useful than GA4 for
      understanding *why* users drop off mid-flow (e.g. abandoning circle
      creation), at the cost of being a bit heavier to set up.
    - **Plausible / Umami** — much simpler, privacy-friendly, cookie-consent-
      free page-view analytics if the goal is just "how many people visit and
      from where," not funnel/behavior analysis.

## Infrastructure / DevOps

- ~~**MCP server: refresh-token flow / OAuth**~~ — **[DONE]** implemented as
  a full OAuth 2.1 authorization-code flow specifically in front of the MCP
  server (`backend/src/Dourak.Api/Controllers/OAuthController.cs`), because
  it turned out to be a hard requirement, not just a convenience: Claude
  Desktop's "Connect" button for custom connectors only speaks MCP's OAuth
  authorization spec (RFC 7591 dynamic client registration, RFC 8414 /
  RFC 9728 discovery metadata, PKCE S256, no client secret) — it has no
  field to paste a bearer token the way ChatGPT's custom-connector form
  does. Public-client only (PKCE instead of a secret); access tokens are
  the same short-lived JWTs `/api/auth/login` issues, refresh tokens are
  opaque, rotated on every use, 90-day lifetime
  (`OAuthClient`/`OAuthAuthorizationCode`/`OAuthRefreshToken` entities).
  The web/mobile apps' own login still uses the plain `/api/auth/login`
  JWT flow unchanged — this OAuth server exists only for MCP clients.
  Still not built: revoking a refresh token from the UI (e.g. "disconnect
  this app" on the profile page) — today the only way to invalidate one is
  directly in the database.
- ~~**"Forgot password" functionality**~~ — **[DONE]** email verification +
  password reset both implemented (backend: `IIdentityService` email
  methods, `AuthController` endpoints; web: `/forgot-password`,
  `/reset-password`, `/verify-email` pages, verified/unverified badge on
  the profile page, a dismissible corner banner nudging unverified users
  to verify — never blocks any action). Two real gaps remain, both
  deliberate for now:
  - **No real SMTP provider configured** — `LoggingEmailSender` (see
    `Dourak.Infrastructure/Email`) logs verification/reset emails instead
    of sending them until `Email:Host` etc. are set (via `.env` — see
    `.env.example`). Set these up once a provider (Zoho Mail, Resend SMTP,
    etc.) is chosen; no code change needed, just config.
  - **Mobile (Flutter) doesn't have this flow yet** — web-only so far;
    port the same three screens + badge + banner to `mobile/` when picked
    up.
  - **No fallback recovery for a user stuck with an unreachable/fake
    email** (can't verify, can't receive a reset link) — the only path
    today is registering a fresh account with a real email and having
    their circle's organizer re-add them. A proper fix needs a real
    admin/support role (see "Super Admin pages" below) to manually verify
    identity out-of-band and reset an account — deliberately not built
    now rather than adding a weaker mechanism (e.g. security questions)
    as a stopgap.
- ~~**GitHub Actions deployment pipeline**~~ — **[DONE]** see
  `.github/workflows/deploy.yml`: backend (`dotnet test`) and frontend
  (`npm run build`) gates run on every push to `main`, then an SSH step
  re-runs `git pull --ff-only && docker compose up -d --build` on the Oracle
  server. Requires repo secrets `ORACLE_HOST`, `ORACLE_USERNAME`,
  `ORACLE_SSH_KEY` (private key matching a public key already in the
  server's `~/.ssh/authorized_keys`), and `ORACLE_DEPLOY_PATH` (the cloned
  repo's directory on the server) — set once in GitHub repo Settings →
  Secrets and variables → Actions. Also runnable on demand from the Actions
  tab (workflow_dispatch) without a new commit.

## Admin-configurable settings (maybe)

The admin site now has a Settings page (`admin/src/pages/SettingsPage.tsx`) backed by a generic
key/value table (`AppSetting` entity, `GET`/`PUT /api/admin/settings`) — adding a new setting is
just a new key in `AppSettingKeys.All` (backend) plus a form field (admin), no migration needed.
Four keys are wired up today: `maintenanceMode`, `announcementMessage`, `minSupportedAppVersion`,
`supportEmail` — all echoed back publicly through `GET /api/auth/config`, which both the web and
mobile apps already call at startup. Candidates for more keys, none built yet:

- **`claimEvidenceMaxSizeMb`** — the 5MB payment-evidence upload cap is hardcoded today (backend
  validation + the app's own helper text).
- **`defaultReminderDaysBefore`** — the default value pre-filled when a member sets up a payment
  reminder for the first time.
- **`lateGracePeriodDays`** — "late after N days" is a fixed 7 today (shown via
  `circle.gracePeriodHint`); making it a setting would let it be tuned without a redeploy.
- **`whatsappSupportNumber`** — an optional WhatsApp contact alongside the existing `/support` form.
- **`remoteLoggingEnabled`** (server-side kill switch) — lets mobile's error relay to
  `/api/diagnostics/log` be turned off without a new app release, same "off until configured"
  pattern as `GoogleAuthOptions.SignInEnabled`.

None of `maintenanceMode`/`minSupportedAppVersion` are actually *enforced* by either client yet
either — today they're just fields an admin can set and the apps can read, not something either
app currently acts on (no maintenance banner/block, no forced-update prompt). That's separate
follow-up work once/if those keys are populated.

## From Phase 2 (deferred out of `prompt02.md`)

- **Multiple organizers / co-organizer roles** — a circle currently has exactly
  one organizer. Shared/delegated organizer permissions (e.g. a co-organizer who
  can record payments but not delete the circle) is future work.
- **Advanced reporting/export** — PDF/Excel/CSV export of schedules, contribution
  history, payout history, etc.
- **Public/discoverable circles** — any notion of browsing/searching for circles
  to join that you weren't personally invited to. Dourak stays invite-only:
  members only ever join because an organizer who already knows them added them.
- **Trust/reputation scoring** — any score, badge, or rating derived from a
  member's payment history across circles.
- **Notification bell icon with unread-count badge** — the top-nav notification
  center UI. For now, pending invitations surface only on the invitee's main/home
  page (see `prompt02.md` §4). The bell/badge is the natural long-term home for
  this and other notification types, but is deferred.
- **Chat system** — any in-app messaging/chat between organizer and members, or
  among circle members.
- **Proper email and phone number format validation** — beyond the uniqueness
  checks already in `prompt02.md` §8, add real format validation for email
  (proper email pattern, not just "not empty") and phone number (valid phone
  number format/pattern, ideally with country-code awareness given Dourak's
  Arabic/GCC-first audience).
- ~~**Super Admin pages**~~ — **[PARTIALLY DONE]** the first version now
  exists: a separate `admin/` codebase deployed at `admin.dourak.money`,
  gated by an `Admin` Identity role (see `docs/progress.md` "Admin Site"
  for the full breakdown). Currently covers:
  - A dashboard with platform-wide totals (users, verified users, active
    users, circles by status).
  - A users-management page: every user, which circles they organize/
    belong to, email-verified status, reset password, deactivate/
    reactivate, delete.

  **Still not built** (deferred further, not part of this pass):
  - Any circle-level admin actions (view/search circles directly, edit/
    remove a specific circle or member from the admin side rather than
    through the user who owns it).
  - Growth-over-time / historical statistics (current dashboard is a
    point-in-time snapshot only).
  - An audit log of admin actions (who reset whose password, when,
    etc.) — currently nothing records this.
  - A UI for granting/revoking the `Admin` role itself (today it's done
    once via `.env`'s `ADMIN_EMAIL`/`ADMIN_PASSWORD` + a server restart —
    fine for a single admin, not for managing several).
  - **Revoking an MCP client's connection** — the "disconnect this app"
    action noted under the OAuth item above would naturally live here (an
    admin, or eventually the user's own profile page, revoking an
    `OAuthRefreshToken` row) rather than only being possible directly in
    the database.
  - **Support requests page enrichment** — the current Support Requests
    page (see `docs/meta-data.md` § Support page) just lists submissions;
    no reply-from-admin flow, no status (open/resolved), no filtering.
  - **A circle-level view of what MCP/AI-assistant activity happened** —
    since an assistant can now create circles, add members, and activate
    them on a user's behalf (see the MCP tools list), there's currently no
    way for an admin — or the user themselves — to tell that a given
    circle/action originated from an AI assistant rather than the app UI.
  - **Search/filter on the users table** — today it's every user, unpaged;
    fine at current scale, won't be once the user count grows.

## From Phase 3 (deferred out of `prompt03.md`)

- **Beta auto-accept for user1/user2 (`docs/prompt03.md` §4)** — `user1`/`user2` are
  currently auto-accepted into any circle the moment they're added, bypassing the
  normal invitation consent step entirely. This is a **temporary beta-testing
  mechanism only** (see `BetaTestUsers.IsAutoAccept` and `BetaUserSeeder` in
  `backend/src/Dourak.Infrastructure`/`Dourak.Application`) — **remove this bypass,
  and ideally the seeded beta accounts themselves, before any real production
  launch.** A real user must always explicitly consent to joining a circle; no
  permanent product rule should ever skip that.

## Carried over from the original Phase 1 BRD (still not built, still deferred)

These were already out of scope for Phase 1 and remain so unless a future prompt
says otherwise:

- Money movement of any kind: wallets, payment gateways, bank integration,
  automatic debits/collections, payout disbursement.
- KYC / identity verification.
- Credit scoring, loans, investments.
- Marketplace features.
- Push notifications, SMS, email notifications (only WhatsApp-share-text and
  in-app surfaces are in scope so far).
- Dispute management workflows (payment disputes, payout disputes, evidence,
  resolution flows).
- Weekly/biweekly/custom cycle frequencies (Monthly only, so far).
- Flexible circle structures: one member holding multiple payout positions,
  variable per-member contribution amounts, different payout values per member.
