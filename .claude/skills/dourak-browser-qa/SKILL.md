---
name: dourak-browser-qa
description: Use when the user asks to visually test, screenshot, QA, or drive the Dourak frontend through a real browser (e.g. verifying a UI change, capturing screenshots for docs, checking Arabic/English/RTL rendering). Covers Playwright setup and Dourak-specific gotchas (locale switching, Arabic text corruption via bash, reliable selectors).
---

# Browser-driving/QA for Dourak's frontend

Prerequisite: the app must be running — see the `run-dourak` skill first.

## Setup (one-time per environment)

```bash
mkdir -p <scratchpad>/pw && cd <scratchpad>/pw
npm init -y && npm install playwright
npx playwright install chromium
```

## Switching language without clicking through the UI

`i18next-browser-languagedetector` reads `localStorage.i18nextLng`. Set it before navigating —
no login-page dropdown click needed:

```js
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
await context.addInitScript((tok) => {
  localStorage.setItem('dourak_token', tok);   // pre-authenticate, skip the login form
  localStorage.setItem('i18nextLng', 'ar');    // or 'en'
}, JWT_TOKEN);
```

Get a JWT fast via `POST http://localhost:5000/api/auth/login` with one of the seeded beta
accounts (see `run-dourak` skill) — no need to drive the login form in a script unless the
login page itself is what's being tested.

## ⚠️ Arabic text gets corrupted through bash/curl on this Windows environment

Piping Arabic strings through `curl -d '{"name":"سلمى النجار"}'` via the Bash tool on this
machine silently mangles the UTF-8 bytes into literal `?` characters — confirmed by checking
the hex bytes (`3f3f3f3f` = ASCII `?`, not `efbfbd`/U+FFFD, i.e. genuine corruption before the
request even leaves bash, not a terminal-display artifact). **Never build Arabic-content API
payloads via bash heredoc/curl -d.** Instead, write a `.js` file with the Write tool (which
preserves UTF-8 correctly) and run it with `node` — Node's `http` module + JSON.stringify keeps
the bytes intact end-to-end. If Arabic data already got corrupted this way, fix it with another
Node script (PUT the correct value) — for an already-Active circle's own name (not editable via
the app's API once past Draft), fix directly via SQL: write the `UPDATE` statement to a `.sql`
file with the Write tool, then `docker exec -i dourak-postgres psql -U dourak -d dourak < file.sql`
(never `docker exec ... psql -c "..."` with Arabic inline in the bash command).

## Reliable form-filling without knowing every MUI selector

For ordered text/number/month inputs in a plain form, this pattern (works regardless of UI
language, since it doesn't match on label text) reliably skips MUI's hidden shadow-copy
textareas and disabled/readonly fields:

```js
const inputs = await page.locator(
  'input[type="text"]:not([readonly]):not([disabled]), ' +
  'input[type="number"]:not([readonly]):not([disabled]), ' +
  'input[type="month"]:not([readonly]):not([disabled]), ' +
  'textarea:not([readonly])'
).all();
await inputs[0].fill('...');  // in DOM order
```

## The "Add Member" search only reliably matches by email, not partial Arabic name

`usersApi.search` didn't return results for a 2-3 character Arabic name prefix in testing
(possibly a normalization/collation quirk), but always matches on a partial email. When
scripting "add member by searching", search `user3@dourak.test` (or similar), not `لين`.

## Exact button/label text (avoids trial-and-error across dialogs)

Sourced from `frontend/src/i18n/ar.json` / `en.json` — grep those files for the current text
before scripting a click target; do not assume the ones below stay accurate forever. As of this
writing, the ones worth knowing because they're easy to guess wrong:
- Reject/Approve claim: `رفض` / `Reject`, `موافقة` / `Approve` (chip badge label, NOT the same
  string as the button — the pending-claim badge is `دفعة معلقة` / "Payment pending", not
  "بانتظار الموافقة").
- Rejected/Approved chip (for re-opening a claim's detail dialog): plain `مرفوضة`/`Rejected`,
  `مقبولة`/`Approved` — NOT `claimRejected`'s literal value if you're reading the wrong i18n key.
- Record a payment for a different month than the one open: the picker link's own text is
  `تسجيل المبلغ على: <current label>` / `Record the amount on: <current label>` — click the
  link (a `<button>` styled as a `MuiLink`, matched via `getByRole('button', { name: '<current
  label text, e.g. الدورة الحالية>' })`), not a generic label.
- MUI `Select` (language switcher, currency) doesn't respond to `.fill()` — use `.click()` then
  click the `MenuItem` by text, or just leave the default when it's already correct.

## Taking a screenshot after a state-changing action

Always `await page.waitForTimeout(400–700)` after a click that triggers a mutation before
screenshotting — React Query's optimistic/refetch cycle needs a beat, and a screenshot taken
too early captures the pre-update state.
