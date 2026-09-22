# Dourak — Project Metadata

A single place for every piece of "outward-facing" information about Dourak that's been
scattered across chat, code, and published pages — app identity, legal text, store-listing
copy, contact info, and third-party integration metadata. Pulled together on 2026-09-22 from:
`mobile/pubspec.yaml`, `README.md`, `docs/proposal.md`, `docs/chatgpt-app-submission.md`, and
the live pages at `dourak.money/{privacy,terms,support}`.

**Not included here on purpose**: server credentials, SSH keys, IPs — see the separate,
gitignored `docs/credential.md` for those. This file is safe to keep in the public repo.

---

## App identity

| | |
|---|---|
| Product name | **Dourak** (دورك) |
| Tagline | A simple, Arabic-first organizer for جمعية / Savings Circles (ROSCA) |
| One-line description | Helps a circle's organizer and members track membership, payout order, monthly contribution cycles, and self-reported payments — without ever touching or moving real money. |
| Primary domain | `https://dourak.money` |
| Admin site | `https://admin.dourak.money` (separate codebase, gated by an `Admin` role) |
| Logs (internal) | `https://logs.dourak.money` |
| DB admin (internal) | `https://db.dourak.money` |
| Owner / contact email | `anass.shaddad@gmail.com` (published on every legal page) |
| Android package id | `com.dourak.mobile` |
| Mobile app version (current) | see `mobile/pubspec.yaml` — `version: X.Y.Z+buildNumber` |
| Supported languages | Arabic (default/primary), English |
| Platforms | Web (React SPA), Android (Flutter) — no iOS build yet |

### Longer description (for a store listing "about" section, drawn from `docs/proposal.md`)

> A Savings Circle (جمعية) is a group of people who agree to contribute a fixed amount
> periodically — usually monthly — into a shared pool, which is then paid out in full to one
> member each cycle until everyone has received it once. Dourak is a tracking and coordination
> tool for exactly this: an organizer creates a circle, adds members, sets the contribution
> amount and payout order (manual or random draw), and Dourak generates the monthly schedule.
> Members can see their circle, report their own payments for the organizer to verify, and get
> reminders. **Dourak never holds, transfers, processes, or guarantees any actual money** —
> all real payment happens directly between members, by whatever means they choose.

---

## Play Store listing copy

**Status: drafted here for the first time — nothing below has been pasted into Play Console
yet, unlike everything else in this file which already exists somewhere live.** Treat this
section as a starting draft, not a already-published value.

- **Short description** (≤80 chars):
  `Organize your جمعية / savings circle — track members, payouts, and payments.`
- **Full description** (draft):
  > Dourak (دورك) helps you organize a savings circle (جمعية / ROSCA) with your family, friends,
  > or coworkers. Create a circle, add members, and set the contribution amount and payout
  > order — Dourak builds the monthly schedule automatically.
  >
  > • Track who has paid and who's due to receive each month's payout
  > • Members can self-report their own payments for the organizer to confirm
  > • Set payment reminders so no one forgets a due date
  > • Bilingual: Arabic and English, right-to-left support throughout
  > • Your data stays yours — Dourak never touches or moves real money; all payment happens
  >   directly between members
  >
  > Whether you're running a family جمعية or organizing one with coworkers, Dourak keeps
  > everyone on the same page without spreadsheets or group-chat confusion.
- **Category**: Finance, or Productivity/Tools (Finance is more discoverable but invites more
  scrutiny during Play review given the money-adjacent subject matter — Dourak's own terms are
  explicit that it never handles real money, which should be front-and-center if Finance is
  chosen).
- **Privacy policy URL** (required field in Play Console): `https://dourak.money/privacy/en.html`
- **Assets that already exist**: `icons/app-icon-512.png`, `icons/feature-graphic-1024x500.png`,
  `mobile/store-assets/play-store-icon-512.png`, plus phone/tablet screenshots in `icons/`
  (`Capture01.JPG`...`Capture05.JPG`, `Capture_tablet_*.JPG`).

---

## Privacy Policy

Published live at **`https://dourak.money/privacy/en.html`** (Arabic: `/privacy/ar.html`) —
source: `frontend/public/privacy/{en,ar}.html`. Last updated: September 22, 2026.

**Information collected**: account info (email, hashed password, display name); optional
profile info (phone, preferred language, currency, time zone); circle/membership data
(circles, contributions, payout order, member lists, payment claims); standard technical usage
logs; if signing in with Google — email, name, and profile picture only (never the Google
password, never Gmail/Drive/Calendar access).

**Explicitly not collected**: real payment details, bank accounts, card numbers — Dourak only
records who *self-reported* paying what.

**Key commitments**:
- Never sold, rented, or shared with third parties for advertising/marketing.
- Circle members see only what their role permits (display names, payment records in their own
  circle); organizers additionally see contact details of members they manage; ordinary members
  cannot see each other's contact details.
- WhatsApp invite links are generated locally — Dourak never accesses WhatsApp contacts/messages.
- Passwords are salted-hashed, never stored in plain text; every request uses JWT auth.
- Users can delete their own account and all its data at any time (Profile → Delete account),
  or by emailing the contact address above.

---

## Terms of Service

Published live at **`https://dourak.money/terms/en.html`** (Arabic: `/terms/ar.html`) —
source: `frontend/public/terms/{en,ar}.html`. Last updated: September 22, 2026.

**Key points**:
- Dourak is an organizing tool only — it does **not** process, hold, transfer, or guarantee any
  payment. A "reported payment" is a self-reported claim for the organizer to verify manually;
  Dourak has no way to confirm a real transfer happened and takes no responsibility for
  misreported or unpaid contributions.
- Dourak is not a party to any agreement between circle members, and isn't liable for disputes
  or losses arising from participating in a circle.
- Users are responsible for their account/Google-sign-in security and for the accuracy of what
  they enter.
- Acceptable use explicitly **permits** AI-assistant access to a user's own account (e.g. via
  the Dourak MCP server) as long as it's acting on that user's own behalf/data.
- Users can delete their own account at any time; Dourak may suspend/terminate an account that
  violates the terms or abuses the service.
- Provided "as is," no uptime/error-free guarantee beyond what applicable law requires.

---

## Support page

Published live at **`https://dourak.money/support/en.html`** (Arabic: `/support/ar.html`) —
source: `frontend/public/support/{en,ar}.html`. A public contact form (no sign-in required):
optional name, required email, required message, optional image/PDF attachment (5MB limit).
Submissions land in the admin site's "Support" page (`admin/src/pages/SupportRequestsPage.tsx`)
for the site owner to review — nothing is auto-answered.

---

## MCP server / AI-assistant integration (for ChatGPT/Claude app-store-style listings)

Live at **`https://dourak.money/api/mcp`** — see `README.md` § "MCP server" and
`docs/chatgpt-app-submission.md` for the full submission checklist. Summary of what a
third-party AI-app directory (e.g. OpenAI's Apps SDK / ChatGPT app directory) would ask for:

- **What it does**: lets a user's own AI assistant read and act on *their own* Dourak circles
  — never another user's data (every tool resolves identity from the authenticated session,
  never a caller-supplied id).
- **Auth**: full OAuth 2.1 (RFC 7591 dynamic client registration, RFC 8414/9728 discovery,
  PKCE S256, no client secret) in front of the same login users already have; a client that
  can't do OAuth (e.g. ChatGPT's custom-connector form) can instead paste a bearer token from
  `POST /api/auth/login`.
- **Tools exposed**: `get_my_circles`, `get_circle_details`, `get_current_cycle_status`,
  `get_circle_members`, `get_circle_history`, `get_pending_invitations`,
  `get_my_payment_claims`, `get_my_payment_reminders` (read); `create_circle`,
  `add_circle_member`, `activate_circle`, `submit_payment_claim`, `withdraw_payment_claim`,
  `set_payment_reminder`, `remove_payment_reminder` (write).
- **Still manual/outstanding for a public directory listing** (from
  `docs/chatgpt-app-submission.md`): org verification, app listing metadata (name/subtitle/
  description/category/logo/screenshots — largely draftable from this file), reviewer test
  credentials, domain verification, per-tool justification write-ups, and the actual portal
  submission. Until then it works today as a private/unlisted connector for anyone who already
  has the URL.

---

## Tech stack summary (for any "what is this built with" field)

Backend: ASP.NET Core 10 Web API (Clean Architecture — Domain/Application/Infrastructure/Api),
PostgreSQL, EF Core, ASP.NET Identity + JWT. Frontend: React 19 + TypeScript (Vite), MUI, RTL
support. Mobile: Flutter (Android only so far). Admin: separate React app under its own
subdomain. Deployment: Docker Compose on a self-managed Oracle Cloud VM, Caddy reverse proxy
with automatic Let's Encrypt HTTPS, GitHub Actions CI/CD. See `README.md` for full detail.
