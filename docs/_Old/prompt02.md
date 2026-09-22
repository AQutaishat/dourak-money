# Dourak — Phase 2: Member Self-Service & Invitations

## How to use this document

This document is the **complete and only scope** for this phase of work. When I
hand this file back to you:

- **First, re-read everything in `docs/`** — the BRD, both competitor studies, this
  file, and `future-work.md` — to link all the ideas together before writing any
  code. This is about building a full mental model of intent and prior decisions,
  not about pulling BRD scope in (see the next point).
- **Ignore the BRD's own Phase 2 (sections 9-10) as a scope source.** Do not build
  anything from the BRD's Phase 2 just because it's there. This document defines
  the actual scope. You may borrow a specific idea, term, or pattern from the BRD
  only where it genuinely fits what's described here — never as a way to quietly
  pull in extra BRD scope.
- Re-read the current codebase (`backend/`, `frontend/`) before starting, so you're
  working from what's actually implemented, not assumptions.
- **Execute immediately.** Do not stop to produce a plan and wait for approval —
  that back-and-forth already happened; this document IS the approved plan. Build
  it milestone by milestone as you did for Phase 1, but without pausing for a
  separate sign-off step first.
- **Track progress as you go** — create `docs/progress.md` if it doesn't exist yet.
  After completing each meaningful step, append a short entry (step name + one-line
  description of what was accomplished, or which business rule it satisfies).
  When a numbered requirement below is fully done, mark it complete in _this_ file
  too (e.g. change `### 2. Adding a member` to `### 2. [DONE] Adding a member`),
  so this document stays an accurate live record of what's shipped vs. pending.

## Context: what Phase 1 actually does today

- The organizer adds members as **plain records** — just a name, optional
  phone/email/notes.
- Members have **no user account, no login, no notifications, no acceptance step**.
- Members can't see anything themselves — the organizer is the only person who
  ever opens the app; everyone else's status is just data the organizer looks at
  and manages on their behalf (and shares via WhatsApp text if they want to update
  people).

## The vision for this phase

When I create a new group (جمعية / circle), I want to:

1. Add members by **selecting them from already-registered Dourak users** — not
   just typing a plain-text name.
2. Selected users get **notified** they've been added to a circle.
3. They can **accept or decline**.
4. Once accepted, they can **log in themselves** and see the circle.

## Member visibility — read this carefully

**Any member added to a group can see everything in that group: payments, the
schedule/plan, and the full member list — the same view the organizer sees.**
Members **cannot modify** anything (no recording payments, no changing the
schedule, no managing members) — view-only access to their circle's data.

This is a deliberate design choice: a جمعية is a small trust-based group where
everyone already knows the full picture (who paid, who's next, who's in the
group) — this is not a system with per-member data hiding _within_ a circle.

**Decision on the flagged concern:**

- Members **may not** see other members' contact info (phone/email) — contact
  details stay hidden between members. Names, payment status, schedule, and
  membership list are still fully visible; just not phone numbers/emails.
- Members **can** see who the organizer is (identifying the organizer — at least
  their name — is fine and expected).

Raise any other visibility concern you notice as you implement — don't guess
silently on privacy-shaped decisions.

## Functional requirements

### 1. [DONE] User profile — add phone number

Add a phone number field to the registered user's profile/account information
(alongside name, email, preferred language, etc. that already exist).

### 2. [DONE] Adding a member — search existing users

Replace/extend the "add member" flow with **one textbox using autocomplete-style
live search**:

- Search-as-you-type, showing matching results while the organizer is still typing.
- Matches by **name, email, or phone number**.
- Organizer picks a result to add that user to the circle (pending their
  acceptance — see below).

### 3. [DONE] Inviting someone not yet registered

In the same "add member" flow, include an option to **invite someone who isn't a
Dourak user yet**:

- Organizer enters the person's info (at minimum a phone number).
- Dourak generates a share action that sends them the **current application URL**
  via WhatsApp (using the existing WhatsApp-share pattern already in the app),
  inviting them to register.
- This is a lightweight invite-to-register flow, not a full deep-link/invitation-
  token system unless you find that's trivially easy to add alongside it.

### 4. [DONE] Accept / decline flow

- An invited (but not-yet-accepted) member shows up somewhere the invitee can see
  and act on it: **accept** or **decline**.
- **Where the invitee sees pending invitations:** a section on their **main/home
  page** listing circles they've been invited to, with Accept/Decline actions.
- A **notification bell icon with an unread-count badge** in the top navigation is
  the natural long-term home for this too, but is **deferred to future work** (see
  `docs/future-work.md`) — the main-page invitations section is what ships now.

### 5. [DONE] Member status inside a circle

On the **Circle Details / Members** page, next to each member, show whether they
have:

- **Accepted**
- **Declined**
- Still **Pending**

**Declined members are treated as not in the group** — excluded the same way an
inactive/deactivated member would be (not part of payout order, schedule, or
contribution tracking). The organizer can **re-invite** a declined member (sends
a fresh invitation through the same flow).

### 6. [DONE] Payment self-reporting, evidence, and organizer approval

Two ways a contribution gets marked paid, both must coexist:

**a) Organizer-recorded (already exists in Phase 1):** the organizer can directly
mark that a member has paid — no change to this path, it stays immediate/final,
same as today.

**b) Member self-report, pending organizer approval (new):**

- A member can mark **their own** current required payment as "I paid" — only
  for themselves, never on behalf of another member.
- This does **not** immediately count as paid. It creates a **pending payment
  notification/claim** awaiting organizer review.
- The member can **attach an image or document as evidence** (e.g. a bank
  transfer screenshot or receipt) when submitting this claim.
- The organizer needs a way to **see incoming payment claims** (with their
  attached evidence) and **approve or reject** each one:
  - **Approved** → the contribution becomes paid, same end state as if the
    organizer had recorded it directly.
  - **Rejected** → the contribution stays unpaid; the member who submitted it
    **can see that their claim was rejected**.
- **Privacy on these claims:** a member's payment claim/notification is visible
  only to **that member and the organizer** — other members must not see it.
  (This is a exception to the "members see everything" rule in the visibility
  section above — payment _status_ — paid/unpaid/late — stays visible to
  everyone as already specified, but the underlying claim/evidence/rejection
  notification is private between the submitting member and the organizer.)

This requires an attachment/evidence storage mechanism (image or document) tied
to a payment claim — design this properly rather than as an afterthought.

### 7. [DONE] Simplified registration screen

Remove the **Name** and **Preferred Language** fields from the register screen —
it should only ask for **Email** and **Password**. Name (and phone, per §1) are
added later by the user themselves, from their profile page (see §8).

### 8. [DONE] Account menu + profile page

**Top navigation:** add a button showing an **anonymous-person icon** plus the
current user's **display name** next to it. If the user has no name set yet, show
their **email** instead. Clicking it opens a dropdown menu containing:

- **My Profile** — navigates to the profile page (below)
- **Logout** — move the existing logout action here (out of wherever it currently
  lives in the top nav)

**Profile page** lets the user:

- Change their **name**
- Change their **phone**
- View their **email as read-only** — cannot be changed here

**Validation rules:**

- **Name and phone are optional** — a user can leave either blank.
- If provided, **name, phone, and email must all be unique** across users — if a
  user tries to save a name or phone that's already taken by another user, block
  the save (same principle already applies to email via existing registration).
- Implementation note: normalize phone numbers (e.g. strip spaces/formatting) and
  names (e.g. case-insensitive comparison) before checking uniqueness, the same
  way email uniqueness is already normalized — otherwise trivially different
  strings that are really the same value would slip past the check.

## [DONE] UI/UX fixes and refinements to existing (Phase 1) screens

These are polish/fix items on screens that already exist — not new Phase 2
features, but part of this same execution pass. Apply the same "common
behaviour" pattern wherever a rule is described as shared (e.g. field validation
style) rather than re-implementing it differently per screen.

### [DONE] Login screen
- Wrong email or wrong password currently shows a flash error, then the page
  **reloads and clears the fields** — remove this behavior entirely.
- Instead: show a clear, non-dismissing message — **"Invalid credentials"** — and
  **keep whatever the user typed** in the fields. No page reload.
- Field-level validation (this becomes the **common validation pattern** used
  everywhere a form has required/format-checked fields, including register and
  create-circle below):
  - On **blur** (moving focus out of the field): if the field is empty when
    required, or fails a format check, show a **red border** on the field plus a
    **clear inline error text** (e.g. "This field is required" / "Invalid email
    format").
  - Applies to both email and password on this screen.

### [DONE] Register screen
- Remove **Name** and **Preferred Language** fields (already specified in §7) —
  only Email and Password remain.
- Email: same common on-blur validation as above — red border + error text if
  empty or invalid format.
- Password: show a **clear, visible hint of the password rules** near the field
  (not hidden until failure) — then on blur, if the value doesn't meet the rules
  or is empty, show red border + error text. Replace the current unclear **popup**
  error with this inline pattern.
- If the email is already registered, show a **clear, specific message** saying
  so (not a generic/unclear failure).

### [DONE] Top navigation — branding, layout, and account menu
- Change the displayed app name from **"دورك"** to **"تطبيق دورك"** or **"برنامج
  دورك"** (include the word app/program, not just the bare name).
- Fix menu alignment per language direction — this is currently wrong:
  - **Arabic (RTL):** nav menu items (Dashboard, My Circles, etc.) sit on the
    **right**, right after the app name with a gap between them. The language
    selector and the account/person-icon menu (see §8 — now containing Logout)
    sit on the **left** (far end).
  - **English (LTR):** nav menu items sit on the **left**, next to the app name.
    The language selector and account/person-icon menu sit on the **right** (far
    end).
  - In short: nav links follow the app name on the "start" side of the reading
    direction; the language switcher + account menu always sit on the "end" side,
    mirrored correctly for each direction.
- **Browser tab title:** currently shows "frontend" — change it to **"Dourak"**.

### [DONE] Dashboard — circle cards
- Show the **circle creator (organizer/admin) name** and **creation date** on the
  circle info card, in both the draft and active card sections.
- **Active** circle status badge: currently gray — change to **green, blue, or
  otherwise clearly distinct from gray/neutral**.
- Active circle cards should also show: **member count** and the **single
  (per-member) payment amount**.

### [DONE] Create Circle screen
- Circle name: apply the common validation pattern — on blur, if empty, red
  border + "this field is required" text.
- Contribution amount input: when focused, **select the existing text** (so
  typing immediately replaces the default `0` instead of appending after it —
  fixes the annoying "always has a leading zero" behavior).
- Add **JOD** to the supported currency list.
- **Remove the frequency selector** (currently shows "Monthly") — for the
  foreseeable future the app is monthly-only, so don't show a field for it at
  all; just treat it as fixed/implicit.
- **Remove the "I am a member of this circle" checkbox** entirely for now.
- Add a **Cancel** button that returns to the previous screen without creating
  the circle.

### [DONE] Circle Details — Draft circles
- The **first tab** shown should be a **Basic Info** tab with: name, description,
  start date, contribution amount, **total monthly amount** (computed:
  `members count × contribution amount`), and **last payment month** (computed:
  start date + number of months equal to member count).
- Add an option to **delete a draft circle** (or any circle with zero payments
  recorded yet) — with a confirmation popup before deleting.
- Add a **return/close button** to go back to the previous page (My Circles).
- Members list: add a button to **add yourself (the organizer) as a member**, if
  you aren't already one — a quick self-add shortcut.
- Action buttons in the members table need **tooltips**.
- The **Activate Circle** action: move it so it lives inside the **re-order
  members / payout order tab**, but is also **always visible beneath all tabs**
  on the draft circle details page (a persistent/sticky action, not tab-specific).
  It must show a **confirmation dialog** whose message specifically asks the
  organizer to confirm the **member order** before activating (e.g. "Are you sure
  of the member order?"), not a generic confirmation.
- Payout Order tab: **remove the separate "Confirm Order" action** — the order
  simply becomes fixed once the circle is activated, so there's no need for a
  distinct confirm step before that.

### [DONE] Circle Details — Active circles
- Active circles should also show basic info similar to the draft view — **or,
  preferably, merge this into the top of the Current Cycle tab as a read-only
  info block** rather than a separate tab. Use your judgment on whichever reads
  better; merging into Current Cycle is the preferred direction.
- Rename the button currently labeled **"تأكيد الاستلام"** to **"تأكيد استلام
  صاحب الدور"** (more specific — confirms the *recipient's* receipt).
- Members tab → History action: when clicked, the history page/section should
  show clear context in its heading, e.g. **"تاريخ دفعات العضو أنس"** (payment
  history of member X) — not just the bare member name with no explanation of
  what page you're on.
- On the member history view: add a **Close** button below the table, in
  addition to the existing **Back** button at the top. Both **Back and Close
  should return to the Members tab inside Circle Details** (not to the circle's
  first/default tab).
- History table: use the **same colored status badges** as the Current Cycle
  payments table (green/orange/etc. per status) — keep this visual language
  consistent across both tables.
- Add a **Close** button at the bottom of the active circle details page to
  return to the previous page.
- Rename the top **"Confirm"** button/menu (the one opening Pause/Resume/Cancel)
  to something clearer — e.g. **"Actions"** or **"Options"** — "Confirm" is the
  wrong word for what it does.
- Current Cycle payments table: **remove the standalone "Unpaid" button** at the
  top of the table. Instead, add a **per-row "Send Reminder"** button next to
  each unpaid member, which sends a payment-reminder message to that specific
  member via WhatsApp (reuse the existing WhatsApp-share pattern).
- Above the Current Cycle summary cards, add a prominent line (medium/large
  font) showing **"صاحب الدور"** plus the **current recipient's name** — this
  should stand out, not be buried inside a smaller card.

## [DONE] Other requirement — local Postgres GUI client (developer tooling, not app scope)

Add a **Postgres browser/GUI client** as a Docker service in `docker-compose.yml`
so the database can be opened and browsed visually (tables, run queries, inspect
data) instead of only via `psql` or code.

- **You choose the best tool for this** — pick whichever you judge best for ease
  of use and lightweight footprint (e.g., pgAdmin, Adminer, or similar), and
  explain briefly why.
- **Needed on both local dev and the production Oracle deployment** — I want to
  be able to browse the production database too, not just locally. Since this
  does mean a database GUI becomes reachable on the production server, use your
  judgment on keeping it reasonably secured (e.g. requiring its own login/auth,
  not binding it to a port that's wide open to the internet without protection)
  — flag whatever tradeoff you land on rather than silently exposing it insecurely.

## Explicitly out of scope for this phase

Moved to `docs/future-work.md` (see that file for the full list) — most of these
came from the BRD's own Phase 2/3 and are **not** being pulled into this phase:

- Multiple organizers / co-organizer roles
- Advanced reporting/export
- Public/discoverable circles
- Trust/reputation scoring
- Chat system
- Notification bell icon with unread badge (deferred from requirement #4 above)

Keep this phase focused on: **search-and-invite existing users → accept/decline →
full view-only visibility inside the circle (minus contact info) → member
payment self-report with evidence, subject to organizer approval.**
