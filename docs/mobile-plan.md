# Dourak — Mobile App Plan (Flutter, Android)

## How to use this document

Same convention as `docs/conventions.md`:

- **Execute immediately** — no plan/approval pause. This document is the
  approved plan; start implementing right after writing it.
- **Track progress in `docs/progress.md`** under a new `## Mobile App (Flutter)
  — Progress` heading, appended below existing phase sections.
- **Mark each numbered requirement `[DONE]` inline** in this file as it's
  completed.

## Goal

Build a native Android app (Flutter) that is a full functional mirror of the
existing React web app (`frontend/`), calling the **same** ASP.NET Core backend
API (`backend/src/Dourak.Api`) — no new backend endpoints, no schema changes.
Same features, same screens, same business rules (organizer-only mutation
rules, read-access rules, activation locking, beta users, etc.), adapted to
Flutter/Material idioms instead of React/MUI.

## Why Flutter

Single codebase, first-class Android support, good HTTP/JSON and state-
management ecosystem, easy RTL + i18n support (Arabic is a hard requirement
here, matching the web app), and it can extend to iOS later at near-zero
extra cost — consistent with the web app's bilingual/RTL design.

## 1. [DONE] Project scaffolding

- New top-level directory: `mobile/` (sibling to `backend/` and `frontend/`).
- `flutter create mobile --platforms=android --org com.dourak` (Android only
  for now, per request; other platforms can be added later without cost since
  Flutter generates them on demand).
- Package structure inside `mobile/lib/`:
  - `main.dart` — app entrypoint, theming, routing setup.
  - `api/` — Dio-based HTTP client + typed endpoint wrappers (mirrors
    `frontend/src/api/*.ts` 1:1: `client.dart`, `auth_api.dart`,
    `circles_api.dart`, `types` as Dart models with `json_serializable`).
  - `auth/` — token storage (`flutter_secure_storage`) + auth state
    (mirrors `frontend/src/auth/`).
  - `state/` — Riverpod providers for server state (mirrors TanStack Query's
    role: caching, invalidation, loading/error states).
  - `screens/` — one folder per web page:
    - `login/`, `register/`, `dashboard/`, `my_circles/`, `create_circle/`,
      `circle_overview/` (with tabs: basic info, members, payout order,
      current cycle, history), `profile/`.
  - `widgets/` — shared components (status chips, validated text fields,
    member list tiles, payment-claim dialogs, etc. — mirrors
    `frontend/src/components/`).
  - `l10n/` — Arabic + English ARB files ported from
    `frontend/src/i18n/{ar,en}.json`, using Flutter's built-in `intl`/
    `flutter_localizations` + RTL (`Directionality`) support.
  - `theme/` — Material theme approximating the MUI theme (colors, typography)
    from `frontend/src/theme/`.

## 2. [DONE] Core packages

- `dio` — HTTP client (interceptors for JWT bearer header + 401 handling,
  mirrors `frontend/src/api/client.ts`).
- `flutter_riverpod` — state management / DI.
- `go_router` — declarative routing + auth guards (mirrors protected routes
  in `frontend/src/app`).
- `flutter_secure_storage` — JWT token persistence (Android Keystore-backed,
  stronger than the web app's `localStorage` equivalent).
- `json_annotation` / `json_serializable` (+ `build_runner`) — typed
  request/response models mirroring `frontend/src/api/types.ts`.
- `intl` + `flutter_localizations` — i18n/RTL.
- `flutter_dotenv` or `--dart-define` — API base URL configuration (mirrors
  `VITE_API_BASE_URL`), so a debug build can point at `http://10.0.2.2:5210/api`
  (Android emulator's alias for the host machine) while a release build points
  at the production domain.
- `url_launcher` — WhatsApp share intent (mirrors the web app's
  `wa.me` link generation for invites and cycle sharing).

## 3. [DONE] Screens & feature parity checklist

Each item ports the matching React page/feature 1:1 in behavior:

- **Auth**: Login, Register — same validation rules, same error surfacing
  (401 on bad credentials must not "look like" a session bounce, matching the
  `AUTH_ENDPOINTS` special-case in `client.ts`).
- **Dashboard**: active circles (with progress, member count, organizer name —
  prompt03 §3) and draft/other circles sections; pending invitations surfaced
  here (prompt02 §4).
- **My Circles**: full list, create button.
- **Create Circle**: form mirroring `CreateCirclePayload` (name, description,
  currency, contribution amount, start date).
- **Circle Overview** (tabbed, mirrors `frontend/src/pages/CircleOverview/`):
  - Basic Info tab — editable pre-activation (name/description/start date,
    prompt03 §1), read-only post-activation; Activate button (small, side-
    aligned, prompt03 §1) shown only here, not duplicated elsewhere; Close
    button at the bottom, on its own row.
  - Members tab — add by user search, add-self shortcut, WhatsApp invite as a
    single no-input share button creating zero backend records (prompt03 §2),
    full remove pre-activation regardless of invite status (prompt03 §1),
    deactivate/replace post-activation.
  - Payout Order tab — manual reorder / random draw / reset; no Activate
    button here (prompt03 §1 removed it from this tab).
  - Current Cycle tab — dashboard stats, per-member payment rows, organizer
    "record contribution" action, member-only "Record my payment" self-report
    button (renamed + organizer-hidden per prompt03 §5), Share-to-WhatsApp
    repositioned away from the claim UI.
  - History tab — past cycles + per-member history.
- **Profile**: display name, phone, preferred language, default currency,
  timezone — same fields as `ProfilePage.tsx`.
- **Pending invitations**: accept/decline (prompt02 §4).
- **Payment claims review**: organizer approve/reject pending self-reported
  payments.

## 4. [DONE] Cross-cutting rules ported as-is (not re-decided)

- Contact info between members stays hidden except to the organizer
  (`CircleReadAccessBehavior` rule) — mobile UI must not display member
  phone/email unless the current user is the organizer.
- Activation locks structure (payout order, member composition) — mobile
  must disable/hide the same controls the web app disables post-activation.
- Beta users `user1`–`user4` behave identically (no special mobile-side
  logic needed; this is a backend behavior already).
- Same currency/date formatting conventions as the web app (reuse the same
  locale codes: `ar`/`en`).

## 5. [DONE] Out of scope for this pass

- iOS build/signing, Play Store publishing/signing config, push
  notifications, offline mode/local caching beyond in-memory Riverpod cache,
  automated widget/integration tests (may be added later — see
  `docs/future-work.md` if deferred).
- No backend changes of any kind — the mobile app is a pure API consumer.

## 6. [DONE] Definition of done for this pass

- `flutter analyze` passes with no errors.
- App builds a debug APK successfully (`flutter build apk --debug`).
- Every screen listed in §3 exists and calls the real backend endpoints
  (matching `frontend/src/api/circles.ts` / `auth.ts` route paths exactly).
- Arabic + English localization wired up with RTL layout support.
- `docs/progress.md` updated with a "Mobile App (Flutter)" section
  documenting what was built and how to run it (`flutter run`, emulator base
  URL note, etc.).
