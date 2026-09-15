# Dourak — Phase 3: Draft-Circle Editing, Invite Cleanup, Beta Test Users & UI Fixes

## How to use this document

Same convention as `docs/prompt02.md`:

- **First, re-read everything in `docs/`** (BRD, competitor studies, `prompt02.md`,
  `progress.md`, `future-work.md`, and this file) plus the current codebase
  (`backend/`, `frontend/`) before writing any code — build the full mental model
  first, don't assume Phase 2's implementation details from memory.
- **Execute immediately** when this file is handed back — no plan/approval pause.
- **Track progress in `docs/progress.md`** (the same file Phase 2 used — do not
  create a separate `progress03.md`). Before adding new entries, add a clear
  section separator/heading such as `## Phase 3 — Draft-Circle Editing, Invite
  Cleanup, Beta Test Users & UI Fixes` so Phase 3 entries are visually distinct
  from the existing Phase 2 entries above them, then append entries under it as
  usual. Mark each numbered requirement below `[DONE]` inline in *this* file as
  it's completed, same as `prompt02.md`.

## Requirements

### 1. [DONE] Draft circle details — layout and editing fixes

- **Activate Circle button sizing**: it currently spans the full width of its
  section. Make it a normal-sized (small/medium) button, aligned to one side
  (left or right — pick whichever fits the RTL/LTR layout better), not a
  full-width block.
- **Remove the Activate Circle button from the Payout Order / member-reorder
  tab.** It was placed there in Phase 2 (`prompt02.md`'s draft-circle section) —
  remove it from that tab entirely. It should exist only as the persistent/sticky
  action beneath all tabs (as already specified in `prompt02.md`), not duplicated
  inside the tab too.
- **Close button placement**: move the "return to My Circles" close button to the
  **bottom** of the draft circle details page — not at the top, and not on the
  same row as the Activate Circle button. Keep it visually separate from the
  activate action so they can't be confused or mis-clicked.
- **Basic Info becomes editable** (while the circle is still a draft / not yet
  activated): the organizer/admin should be able to edit the circle's **name**,
  **description**, and **start date** directly from the Basic Info tab. Once the
  circle is activated, these fields go back to read-only (activation still locks
  structure, per `prompt02.md`'s existing rules) — contribution amount stays
  non-editable throughout, same as today.
- **Member removal while still draft**: while a circle has not yet been activated,
  the organizer can **completely remove** a member from the circle (not just mark
  them declined/inactive — an outright delete of that membership row), regardless
  of whether that member is a registered user, an accepted member, or still
  pending invitation. This only applies pre-activation; post-activation member
  handling rules from `prompt02.md` (deactivate, replace-for-future-position)
  are unchanged.

### 2. [DONE] WhatsApp invite flow — don't add a member record at all

Rework the "invite someone not yet registered via WhatsApp" flow from
`prompt02.md` §3, which currently creates a member placeholder:

- **Sending a WhatsApp invite must NOT add anything to the circle's member list.**
  No member record, no pending-invitation row — nothing persisted against the
  circle at all.
- The **"Add Member" dialog's WhatsApp-invite option should be reduced to a single
  button** — no phone number field, no name field required. Clicking it
  immediately opens/sends the WhatsApp share message (same message content
  pattern as the existing WhatsApp-share feature) inviting the person to
  register on Dourak. That's the entire interaction.
- **After that person registers on their own**, the organizer adds them to the
  circle the normal way — via the existing user-search-and-select flow
  (`prompt02.md` §2) — same as adding any other already-registered user. There is
  no link between the WhatsApp invite button and any specific later signup; it's
  just a share action, not a tracked invitation.

### 3. [DONE] Home page (Dashboard) — active circle cards missing info

`prompt02.md`'s Dashboard section asked for member count and organizer/creator
name on circle cards, but **the active circles section (the one with a progress
indicator) is still missing them**. Add to each **active** circle card in the
dashboard's first/active section:

- **Member count**
- **Organizer / manager (creator) name**

(Same information already specified for the general card requirements in
`prompt02.md` — this just closes the gap on the specific active-with-progress
card variant that was missed.)

### 4. [DONE] Beta-testing seed users with auto-approval for two of them

For this beta-testing phase, set up **four** fixed test users:

- **`user1`, `user2`** — when either of these is added to *any* circle, they are
  **added as already-accepted immediately** — no pending/invitation step, no
  need for them to log in and accept. This applies regardless of which organizer
  adds them or which circle.
- **`user3`, `user4`** — these accounts exist too, but go through the **normal**
  invite → pending → accept/decline flow like any other real user. No special
  treatment.

Implementation notes (use judgment, flag anything unclear rather than guessing
silently):
- These are real seeded user accounts (need login credentials — pick a
  reasonable seeding mechanism, e.g. a startup seeder or migration-adjacent data
  seed, consistent with how the codebase already seeds/initializes data if it
  does; document the login credentials for these four accounts in
  `docs/progress.md` so they're easy to find for testing).
- The auto-approval behavior for `user1`/`user2` should be clearly scoped as a
  **temporary beta-testing mechanism** — comment it in code as such, and record
  it in `docs/future-work.md` as something to remove/reconsider before any real
  production launch (auto-accept bypassing invitation consent is not a
  permanent product rule).

### 5. [DONE] "I paid" self-report button — copy and placement

- **Rename the button text.** Currently "I paid" (or similar) — change it to
  something clearer, e.g. **"Record my payment"** or **"Register my payment"**
  (pick whichever reads more naturally in both Arabic and English translations).
- **This button (and the payment-claim UI around it) must never appear to the
  circle organizer/admin** — it's a member self-service action only. Verify this
  is actually enforced today; if the organizer can currently see it on their own
  view of the circle, fix that.
- **Reposition the "Share to WhatsApp" action** relative to the payment
  claim/record-payment button so they don't visually compete/crowd each other.
  Acceptable directions (pick whichever reads best in the actual layout,
  mirrored correctly for RTL):
  - Move "Share to WhatsApp" to the bottom-right (in RTL) of its section, or
  - Move it to the top of the section, or
  - Move it into its own tab/content area,
  - or another placement of your judgment.
  The important constraint is just: don't leave the two crowding each other, and
  the record-payment button must stay member-only (never visible to the
  organizer), per the point above.

## Notes

- This phase is corrective/refinement work on top of `prompt02.md`'s Phase 2
  features, not new business scope beyond what's listed above — keep changes
  scoped to these five points.
- If anything here conflicts with an explicit rule already locked in
  `prompt02.md` (e.g. visibility rules, activation-locking rules), flag the
  conflict rather than silently overriding it.
