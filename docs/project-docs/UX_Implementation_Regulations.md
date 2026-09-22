# Dourak UX Implementation Regulations

These are binding conventions for any agent (Claude or otherwise) implementing frontend
work on Dourak. They were extracted from the actual codebase plus the specs that drove
it, so most entries cite their source. Follow them by default; deviating requires a
deliberate decision, not an accident.

Source docs referenced below:
- `docs/project-docs/Dourak_Business_Requirements.md` (BRD) — product principles, phase-1 spec
- `docs/_Old/prompt02.md` — Phase 2 UX spec (richest source of explicit requirements)
- `docs/_Old/prompt03.md` — Phase 3 corrections/refinements to prompt02

---

## 1. RTL / Arabic-first support

Arabic is not a translation layer bolted onto an English-first app — it's a first-class
target. (BRD Product Principle #2; BRD §6.23)

- Use CSS **logical properties** everywhere: `insetInlineStart/End`, `marginInlineStart/End`,
  `borderInlineStart/End`. Never use `left`/`right` directly — this is what makes mirroring
  automatic under `dir="rtl"` without conditional styling.
  (`frontend/src/components/UnverifiedEmailBanner.tsx:39-44`, `frontend/src/layouts/AppLayout.tsx:72,87`)
- `dir` and `lang` on `<html>` are set reactively; theme direction, emotion cache (stylis RTL
  plugin), and font family (Tajawal for RTL, Inter for LTR) all key off the current language.
  (`frontend/src/App.tsx:44-54`, `frontend/src/theme/theme.ts:9-23`)
- Nav layout mirrors per direction: links follow the app name on the "start" side; language
  switcher + account menu always on the "end" side; Drawer anchors to the reading-direction
  end (`left` for AR, `right` for EN). (prompt02.md lines 223-236; `AppLayout.tsx:21-32,150`)
- Any locale-dependent branching (help file, nav anchor, etc.) should reuse the same
  `i18n.language.startsWith("ar")` check for consistency, not a new ad-hoc test.

## 2. Form validation UX

One shared validation pattern for every form — do not write per-form validation logic.
(prompt02.md lines 197-221)

- Use `ValidatedTextField` / `useValidatedField` (`frontend/src/components/ValidatedTextField.tsx`).
- Errors appear **only on blur**, never while typing; they clear immediately on correction.
- Show red border + inline helper text — never a popup/alert for field-level errors.
- Numeric fields (e.g. contribution amount) use `SelectOnFocusTextField` so typing replaces
  the default `0` instead of appending to it. (prompt02.md lines 250-253)
- Password rules are shown as visible hints near the field up front, not hidden until failure.

## 3. Error handling / messaging

- Never clear the user's input or force a reload on a failed submission — show a persistent
  inline error and keep whatever they typed. (prompt02.md lines 198-201)
- Error messages must be **specific to the actual cause** ("this email is already registered"),
  never a generic "something went wrong." (prompt02.md line 220)
- Map generic/unfriendly API errors to context-aware copy at the call site rather than
  surfacing raw backend text. (`frontend/src/pages/CircleOverview/ActivateCircleButton.tsx:35-39`)

## 4. Confirmation dialogs for destructive/high-stakes actions

- Confirmation dialogs must name the **specific consequence**, not ask a generic "Are you
  sure?" (e.g. "Are you sure of the member order?" before activating a circle).
  (prompt02.md lines 271-277; BRD §6.9)
- If a confirmation dialog is triggered from more than one place in the UI (e.g. a tab and a
  sticky footer), centralize it in one shared component so the message can't drift between
  call sites. (`ActivateCircleButton.tsx:9-13`)
- General rule (BRD §7 rule 11): any change affecting an **active** circle requires clear,
  deliberate confirmation.
- Deleting a draft circle requires a confirmation popup. (prompt02.md line 267)

## 5. Non-blocking notifications ("nudge, don't gate")

- Informational banners (e.g. unverified-email) must be dismissible and non-modal. Every app
  action must remain usable regardless of the banner's state — never gate functionality
  behind a soft nudge. Dismissal is session-only (reappears on reload/new login if the
  underlying condition persists). (`frontend/src/components/UnverifiedEmailBanner.tsx:8-14`,
  `AppLayout.tsx:204-207`)

## 6. Loading / empty states — minimalism by default

- No toast library, no skeleton loaders. Use plain text loading states (`t("common.loading")`)
  and inline `Alert`/disabled-button states instead. This is a deliberate choice consistent
  with "no unnecessary complexity" — don't introduce a toast/skeleton system without
  discussing it first. (`frontend/src/pages/Dashboard/DashboardPage.tsx:16`)
- Empty states pair a tagline with a CTA button, and should still surface anything actionable
  above them (e.g. pending invitations render above "no circles yet", since a first-time
  invitee has no circles of their own). (`DashboardPage.tsx:18-33`)

## 7. Mobile responsiveness

- **Never allow horizontal scroll.** `overflowX: hidden` + `maxWidth: 100%` on html/body is a
  hard requirement — a stray horizontal scrollbar breaks MUI's scroll-lock math for every
  Dialog/Menu/Popover backdrop on phones. This is a real, previously-hit bug class, not
  theoretical. (`frontend/src/theme/theme.ts:27-40`)
- Dialogs get near-full-width margins under 600px (12px margin) instead of the default 32px
  desktop margin. (`theme.ts:34-38`)
- Below the `sm` breakpoint, collapse the desktop nav row into a hamburger + side Drawer with
  identical actions — don't try to shrink the row instead. (`AppLayout.tsx:21-32,37,80-84,149-199`)
- Mobile-first: the most common actions must be convenient from a phone. (BRD Product Principle #8)

## 8. MUI as a toolkit, not a style mandate

Dourak uses `@mui/material` for its component scaffolding (accessibility, focus states,
responsive breakpoints, Dialog/Drawer/Menu behavior) but **deliberately de-materializes**
the visual language to avoid a "banking dashboard" look (BRD Product Principle #6, BRD §6
intro; `frontend/src/theme/theme.ts:4-8`):

- `MuiButton` has `disableElevation: true` everywhere — flat buttons, no Material drop-shadow.
- `MuiCard` uses one fixed, subtle shadow (`0 1px 4px rgba(0,0,0,0.08)`) instead of Material's
  tiered elevation scale.
- Global `borderRadius: 12` (softer/rounder than Material's default 4px) — closer to a
  consumer messaging-app aesthetic (WhatsApp-simple) than Google's Material spec.
- Palette is a **single accent pair**: `#1F8A70` calm green (primary, "money/trust without
  looking like a bank") and `#F2A541` amber (secondary), on an off-white background
  (`#FAF9F6`). No broader Material tonal palette, no computed light/dark variants.
- **Do not** reach for MUI's default elevation scale, default 4px radius, or a wider Material
  color palette. If a new color is genuinely needed, extend the existing two-accent-color
  logic rather than introducing a Material-style full palette.

## 9. Color / status system

- Status badges (payment status, circle status, etc.) must reuse the **same color coding**
  across every screen/table — never invent a new color mapping per view.
  (prompt02.md lines 296-299)
- Status must be scannable at a glance: e.g. "Active" circle badge uses a clearly distinct
  color (green/blue), not neutral gray. (prompt02.md lines 242-243; BRD Product Principle #1)

## 10. Navigation & information architecture

- Account menu = person icon + display name (fallback to email) with a dropdown for
  Profile/Logout — never an ungrouped standalone logout button. (prompt02.md lines 163-171)
- Persistent primary actions (e.g. "Activate Circle") live in one sticky location, not
  duplicated inside a specific tab. When prompt03 corrected this, it removed the duplicate
  and kept only the sticky version — treat later correction docs as authoritative over
  earlier ones when they conflict. (`ActivateCircleButton.tsx`; prompt03.md lines 22-32)
- Close/Back buttons must return to a specific parent tab, not a default tab. Destructive and
  primary actions (e.g. Close and Activate) must never share a row, to avoid accidental
  misclicks. (prompt02.md lines 293-296; prompt03.md lines 33-36)

## 11. WhatsApp as the canonical share/reminder channel

- Route all sharing (circle status, unpaid-members list, per-member payment reminders,
  invite-to-register) through `frontend/src/utils/whatsapp.ts`'s `shareToWhatsApp()`, with
  locale-aware (AR/EN) message templates. (BRD §6.21-6.22; prompt02.md §3, lines 305-308)
- Keep invite-by-WhatsApp **lightweight** — no member/invitation record, no tracking link.
  prompt03 explicitly walked back an earlier heavier design; default to the simpler version
  unless specifically asked to add more. (prompt03.md lines 51-68)

## 12. Role-gated visibility (correctness, not just UX)

- The self-report "Record my payment" action must **never** be visible to the circle's
  organizer/admin. (prompt03.md lines 109-117)
- Payment claims/evidence are private between the submitting member and the organizer — the
  one explicit exception to the otherwise-full "members see everything" transparency rule.
  Audit any new feature touching payments against this. (prompt02.md lines 50-70, 147-152)

## 13. Tooltips / discoverability

- Icon-only action buttons (e.g. in tables) require tooltips. No exceptions. (prompt02.md line 272)

## 14. Traceability convention — keep it going

Code across this repo cites the spec section it implements inline, e.g.:

```ts
// prompt02 §Create Circle: ...
```

This made auditing the codebase against its specs fast and reliable. **Continue this
convention** for any new feature that traces to `docs/_Old/prompt02.md`, `prompt03.md`, or
the BRD — cite the doc and section in a comment at the implementation site.

---

## Provenance note

Most patterns above are directly traceable to `prompt02.md`/`prompt03.md`/BRD. A few are
implicit/emergent (no doc says to do them, but they're consistent, deliberate practice worth
preserving): CSS logical properties for RTL (§1), the mobile `overflowX: hidden` safety net
(§7), plain-text-only loading states (§6), centralizing shared confirmation dialogs (§4), and
mapping generic API errors to specific copy (§3). Treat these as equally binding — they were
derived from real bugs or real consistency problems, not arbitrary preference.
