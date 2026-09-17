# Web ↔ Mobile Feature Gap

Tracks web app changes that have **not yet been ported to the Flutter mobile app**. Whenever a change is made to `frontend/` and the equivalent isn't also made in `mobile/`, add an entry here. Remove an entry once the mobile side is implemented.

| Date | Web change | Where (web) | Mobile status |
|---|---|---|---|
| 2026-09-17 | Contributions are only marked "Late" after a 7-day grace period past the due date (previously late the instant the due date passed). Added `Contribution.GracePeriodDays = 7` in the domain layer (backend, so mobile gets the correct `status` value from the API automatically), plus an always-visible caption above the payments table explaining the rule: "Marked Late 7 days after the due date if still not fully paid." | `backend/src/Dourak.Domain/Entities/Contribution.cs`; `frontend/src/pages/CircleOverview/CurrentCycleTab.tsx` (caption); `frontend/src/i18n/en.json` / `ar.json` (`circle.gracePeriodHint`) | Backend logic applies automatically (same API). **Missing**: the explanatory caption in `mobile/lib/screens/circle_overview/current_cycle_tab.dart` and the `gracePeriodHint` string in `mobile/lib/l10n/strings.dart`. |

## Longer-standing known gaps (pre-dating this log)

- Mobile has no equivalent screens/flows for: forgot-password / reset-password, email verification banner + resend. See `docs/future-work.md`.
