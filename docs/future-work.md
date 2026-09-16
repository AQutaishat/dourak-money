# Dourak — Future Work (Deferred Scope)

Items deliberately pushed out of the current phase(s), tracked here so they're
not forgotten but also don't creep into active scope uninvited. Nothing here
should be built unless a future prompt explicitly pulls it back into scope.

## Infrastructure / DevOps

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
