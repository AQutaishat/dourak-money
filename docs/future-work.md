# Dourak — Future Work (Deferred Scope)

Items deliberately pushed out of the current phase(s), tracked here so they're
not forgotten but also don't creep into active scope uninvited. Nothing here
should be built unless a future prompt explicitly pulls it back into scope.

## Infrastructure / DevOps

- **GitHub Actions deployment pipeline** — add a CI/CD workflow that deploys to
  the production Oracle server automatically (e.g. on push to `main`, or on a
  tag/release), instead of the current manual `ssh` + `git pull` +
  `docker compose up -d --build` steps. Needs: a way to reach the server from
  GitHub Actions (SSH key stored as a repo secret, or a self-hosted runner on
  the server itself), and care around not disrupting the JWT secret / `.env`
  file already set up there. Also consider adding a test/build gate (run
  `dotnet test` and `npm run build`) before deploying, so a broken commit on
  `main` never reaches production automatically.

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
- **Super Admin pages** — a separate admin-only area (not the regular organizer
  dashboard) that can see and act across **all** users' data platform-wide:
  - View/search all members and all circles across every user, not just one's own.
  - Perform administrative actions on them (e.g. deactivate a circle, edit/remove
    a problematic member or user, investigate an issue).
  - Show platform-wide statistics (e.g. total users, total circles, active vs.
    completed circles, total volume tracked, growth over time).
  - This is a distinct role/permission level from "organizer" — needs its own
    authorization design when it's picked up.

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
