# Dourak — Phase 2 Progress Log

Append-only log of meaningful steps taken while executing `docs/prompt02.md`.
Each entry: step name + one-line description / business rule satisfied.

---

- **Phase 2 kickoff** — Re-read `docs/` (BRD, competitor studies, `prompt02.md`, `future-work.md`) and the full Phase 1 `backend/`/`frontend/` source before writing code; confirmed scope is `prompt02.md` only.
- **Domain: invitation lifecycle** — `CircleMember` gained `UserId` + `InvitationStatus` (NotInvited/Pending/Accepted/Declined) and an `IsParticipating` rule; the `SavingsCircle` aggregate now builds the payout order/schedule from participants only, so declined *and* not-yet-accepted members are excluded exactly like deactivated ones (prompt02 §5).
- **Domain: payment claims** — New `PaymentClaim` entity with `Approve`/`Reject`/`AttachEvidence`; approval re-uses `Contribution.RecordPayment`, so the Phase 1 "paid cannot exceed expected" rule still governs the self-report path (prompt02 §6b).
- **Domain: activation confirms the order** — `Activate()` now validates and confirms the payout order itself, since prompt02 removes the separate "Confirm Order" action; activating with no order at all still throws. Judgment call: the Phase 1 domain test `Activate_WithoutConfirmedOrder_Throws` was replaced by two tests reflecting the new rule rather than deleted.
- **Application: read vs. write access** — Added `ICircleReadRequest` + `CircleReadAccessBehavior` alongside the Phase 1 `CircleOwnershipBehavior`: accepted members can read their circle, only the organizer can mutate it. Two markers keep "who may read" and "who may write" impossible to confuse.
- **Application: privacy rule** — Payment claims/evidence/rejections are filtered to (submitting member ∪ organizer) inside the query handlers and the evidence download, while payment *status* stays visible to every member (prompt02 §6). Members also never see other members' phone/email.
- **Application: profile + user search** — `IIdentityService` extended with profile read/update, normalized name/phone uniqueness (`UserValueNormalizer`), and a name/email/phone search for the add-member autocomplete. Judgment call: the search needs ≥2 characters so the user directory can't be enumerated with an empty query.
- **Infrastructure** — `ApplicationUser.DisplayName` is now optional (§7) with `NormalizedDisplayName`/`NormalizedPhoneNumber` columns carrying filtered unique indexes; `DiskEvidenceFileStorage` writes evidence to a configurable persistent path with server-generated GUID filenames, a 5 MB cap and an image/PDF allowlist. Judgment call: files on disk + a DB reference, not a blob column — a few receipts per circle don't justify bloating Postgres or adding cloud storage.
- **EF migration `Phase2MemberSelfServiceAndInvitations`** — New columns/indexes + `PaymentClaims` table, plus a backfill of the normalized user columns that skips pre-existing duplicates instead of failing the migration.
- **Backend tests** — Added `InvitationTests`, `PaymentClaimTests` (domain) and `Phase2WorkflowTests` (application) covering payment-claim approval/rejection, invitation accept/decline, declined-member exclusion, claim privacy, and name/phone uniqueness normalization. Full suite: 55 passing (34 domain + 21 application).
- **Frontend: login no longer reloads** — The axios 401 interceptor was what redirected the browser (and wiped the form) on bad credentials; it now skips `/auth/login` and `/auth/register` so those responses reach the caller. Login shows a non-dismissing "Invalid credentials" and keeps what was typed (§Login screen).
- **Frontend: one shared validation pattern** — `useValidatedField` implements the on-blur red-border + inline-error rule once, reused by login, register and create-circle exactly as the spec asks (§Login screen). Register also shows the password rules up front instead of only on failure, and names the "already registered" case specifically.
- **Frontend: top nav** — App name is now "تطبيق دورك" / "Dourak App"; nav links sit on the start side after the name and the language switcher + account menu on the end side, mirrored by a single flex spacer (no per-language branching). Tab title changed from "frontend" to "Dourak". Logout moved into the new account menu (person icon + display name, falling back to email) alongside My Profile.
- **Frontend: profile page** — `/profile` edits name and phone, shows email read-only, and surfaces the API's uniqueness errors inline (§8).
- **Frontend: add-member flow** — One debounced autocomplete over registered users (name/email/phone) plus an invite-an-unregistered-person sub-form that shares the app URL over WhatsApp and records them as a plain member so the organizer can still track them (§2, §3).
- **Frontend: invitations + member status** — Pending invitations with Accept/Decline on the home page (§4); Accepted/Declined/Pending chips next to each member, declined shown struck-through like deactivated ones, with a re-invite action (§5).
- **Frontend: payment claims** — Members get an "I paid" dialog with optional image/PDF evidence; organizers get a badge-counted review inbox with approve/reject-with-reason and a token-authenticated evidence download (§6).
- **Frontend: Phase 1 screen refinements** — Dashboard/My Circles cards show organizer + creation date, a green Active badge, member count and per-member amount; create-circle gains JOD, select-on-focus amount, a Cancel button, and loses the frequency selector and "I am a member" checkbox; draft details lead with a computed Basic Info tab (total monthly amount, last payment month), gain delete-with-confirm, a self-add shortcut, tooltips, and a persistent Activate button with the member-order confirmation dialog, while the separate Confirm Order action is gone; active details merge Basic Info into the top of Current Cycle, add a prominent "صاحب الدور" line, rename the Confirm menu to Actions, replace the bulk Unpaid button with per-row WhatsApp reminders, and rename the payout button to "تأكيد استلام صاحب الدور"; member history gains a contextual heading, shared colored status badges, and Back/Close that both return to the Members tab.
- **Postgres GUI client** — Added **Adminer** (not pgAdmin) to `docker-compose.yml`. Reasoning: both browse tables and run queries, but Adminer is a single ~10 MB stateless container whose login *is* the Postgres login, whereas pgAdmin is ~600 MB, stateful, and adds a second credential store to manage. **Security tradeoff, chosen deliberately:** the port is published on `127.0.0.1:8081` only, so on the production host Adminer is reachable through an SSH tunnel (`ssh -L 8081:127.0.0.1:8081 …`) but never from the internet — publishing a form that accepts database credentials publicly, with no rate limiting or MFA, was not an acceptable default. The compose file documents how to widen it (behind Caddy with HTTPS + basic auth) if that's ever wanted. Also added a `dourak-evidence-data` volume so payment-claim evidence survives rebuilds. `.env`/JWT handling untouched.
- **Pre-existing issue noticed (not changed — out of scope):** `docker-compose.yml` publishes Postgres itself on `0.0.0.0:5432`. That predates Phase 2 and isn't in this phase's scope, but it is a bigger exposure than the Adminer port and is worth binding to `127.0.0.1` in a follow-up.
- **Verification** — `dotnet test`: 55/55 passing. `npm run build`: succeeds (TypeScript clean; only the pre-existing bundle-size advisory). `npm run lint`: warnings only, same categories as Phase 1.

## Phase 3 — Draft-Circle Editing, Invite Cleanup, Beta Test Users & UI Fixes

- **Domain: draft-only basic-info editing and hard member removal** — `SavingsCircle.UpdateBasicInfo(name, description, startDate)` lets the organizer edit those three fields while still Draft (contribution amount stays non-editable, same as before/after activation); `SavingsCircle.RemoveMember(memberId)` hard-deletes a member row (and its payout position, if any) while Draft, regardless of invitation status — contrast with the existing `CircleMember.Deactivate()`, which remains the only option post-activation since financial history must never be hard-deleted (prompt03 §1).
- **Application: `UpdateCircleBasicInfoCommand` + `RemoveMemberCommand`** — both `ICircleOwnedRequest` so only the organizer can call them; wired to `PUT /circles/{id}/basic-info` and `DELETE /circles/{id}/members/{memberId}`. `RemoveMemberCommandHandler` explicitly calls `_db.CircleMembers.Remove(member)` in addition to the domain-level collection removal, since EF's default fixup for a nullable FK can otherwise just null it out instead of deleting the row.
- **Frontend: draft circle details layout fixes** — Activate Circle is now a normal-sized button aligned to one side inside the sticky footer (was `fullWidth`); the duplicate Activate button inside the Payout Order tab is gone (the sticky footer is now the only place it lives); the "return to My Circles" Close button moved from the top action row to its own row at the bottom of the page, below and separate from the sticky Activate footer, on draft circles specifically (active circles keep the existing top+bottom pair unchanged — out of prompt03's scope).
- **Frontend: editable Basic Info tab** — `BasicInfoBlock` gained an edit mode (name/description/start date, contribution amount excluded) shown only to the organizer while the circle is Draft; saving calls the new `updateBasicInfo` API and invalidates the circle query. The dense/read-only variant used inside the Active circle's Current Cycle tab is unaffected.
- **Frontend: full member removal pre-activation** — `MembersTab` gained a destructive "remove member" icon action (confirmation dialog) next to the existing deactivate/reinvite ones, visible whenever the circle is Draft regardless of the member's invitation status (pending, accepted, declined, or a plain not-invited record all qualify).
- **WhatsApp invite reworked to a pure share action** — `AddMemberDialog`'s "invite someone not yet registered" section is now a single button with no name/phone fields; it only calls `shareToWhatsApp` — no `addMember`/backend call happens at all, so no member or invitation record is ever created from this flow (prompt03 §2). Once that person registers, the organizer adds them normally through the existing user-search flow above, with no tracked link between the WhatsApp click and the later signup.
- **Dashboard: active-with-progress cards get organizer + member count** — `CurrentCycleSummaryCard` (the card with the paid/collected progress bar, distinct from the general `CircleInfoCard` grid that already had this since Phase 2) now takes `memberCount`/`organizerName` props and shows them under the circle name, closing the gap prompt03 §3 flagged.
- **Beta test users (TEMPORARY, beta-only)** — `BetaUserSeeder` (Infrastructure) seeds four fixed accounts idempotently at startup, right after `db.Database.Migrate()` in `Program.cs` — no new migration needed since Identity's own tables already exist and no schema changed. `BetaTestUsers.IsAutoAccept(email)` (Application layer) flags `user1@dourak.test`/`user2@dourak.test`; `AddUserMemberCommandHandler` calls `member.AcceptInvitation(now)` immediately after `member.Invite(now)` when the target user is one of those two, regardless of which organizer/circle added them. `user3@dourak.test`/`user4@dourak.test` get no special handling and go through the ordinary pending → accept/decline flow. Both the seeder and the auto-accept check are commented in code as a **temporary beta-testing mechanism** and recorded in `docs/future-work.md` for removal before real launch. **Seeded login credentials** (email / password, all four): `user1@dourak.test` / `user2@dourak.test` / `user3@dourak.test` / `user4@dourak.test`, password `Beta1234!` for all four (see `BetaUserSeeder.DefaultPassword`).
- **"I paid" renamed and re-scoped to members only** — the `circle.iPaid` i18n string is now "Record my payment" / "سجّل دفعتي" (en/ar). `CurrentCycleTab` already gated the button on `myRow`, but that alone doesn't exclude an organizer who is also a participating member of their own circle — added an explicit `!canManage` guard on the button, the claim-status chip, and the submit-claim dialog's render condition so the record-payment/claim UI is provably never shown to the organizer (prompt03 §5).
- **"Share to WhatsApp" repositioned** — moved out of the row it shared with the claims/record-payment controls and up next to the prominent "صاحب الدور" recipient line, in a `justifyContent="space-between"` row (mirrors correctly for RTL/LTR without per-language branching) so it no longer crowds the member-only claim button.
- **Backend tests** — Added `Phase3WorkflowTests` (application: editable basic info incl. post-activation lock, hard member removal across every invitation status incl. post-activation lock, user1/user2 auto-accept, user3/user4 normal flow) and extended `SavingsCircleTests` (domain: `UpdateBasicInfo`/`RemoveMember` happy paths, empty-name guard, post-activation guards, unknown-member guard). Full suite: **68 passing** (41 domain + 27 application), up from 55.
- **No EF migration added** — confirmed via a throwaway `dotnet ef migrations add` (empty Up/Down, then removed) that none of this phase's changes touch the schema; beta users are plain Identity rows created through the existing `UserManager`.
- **Verification** — `dotnet test`: 68/68 passing. `npm run build`: succeeds (TypeScript clean; only the pre-existing bundle-size advisory, unchanged from Phase 2).

## Mobile App (Flutter) — Progress

Executed `docs/mobile-plan.md` end-to-end in a new `mobile/` directory at the repo
root: a Flutter/Android app calling the same backend (`backend/src/Dourak.Api`),
no backend or React frontend changes. Built by hand-authoring every file (the
sandbox has no Flutter/Dart SDK — `flutter`/`dart` are not on PATH and no SDK
install was found — so `flutter create` could not be run; the project skeleton,
including `android/`, was constructed manually to match what `flutter create
mobile --platforms=android --org com.dourak` produces).

### What was built

- **API layer** (`mobile/lib/api/`) — `api_client.dart` (Dio + bearer-token
  interceptor + the same `AUTH_ENDPOINTS` 401 special-case as
  `frontend/src/api/client.ts`, wired to force-logout instead of a page reload),
  `auth_api.dart`, `circles_api.dart` (every route from
  `frontend/src/api/circles.ts` ported 1:1, including multipart claim
  submission), `models.dart` (hand-written `fromJson`, mirroring
  `frontend/src/api/types.ts`).
- **Auth** (`mobile/lib/auth/auth_state.dart`) — Riverpod `StateNotifier`
  mirroring `AuthContext.tsx`: `AuthException` mirrors the web app's
  `AuthError` (`invalid-credentials` vs `server`), token persisted via
  `flutter_secure_storage` (Android Keystore-backed).
- **State** (`mobile/lib/state/providers.dart`) — Riverpod `FutureProvider`
  families stand in for the web app's TanStack Query hooks; a single
  `refreshTickProvider` counter is the Riverpod equivalent of
  `queryClient.invalidateQueries()` — every mutation bumps it and every screen
  watching a data provider refetches.
- **Screens** (`mobile/lib/screens/`) — login, register, dashboard (pending
  invitations + active-circle progress cards + full circle list), my circles,
  create circle, profile, and the circle overview tab set: Basic Info (draft,
  editable name/description/start date only), Members (search-and-add,
  WhatsApp-invite-as-pure-share, deactivate, prompt03 §1 full removal on
  drafts), Payout Order (manual reorder via up/down buttons instead of
  drag-and-drop — more reliable on touch — random draw, reset), Current Cycle
  (dashboard stats, per-row record-payment, member self-report claim button
  hidden from organizers per prompt03 §5, payout confirmation), Schedule,
  History, and the Member History screen (Back/Close both return to the
  Members tab via `?tab=members`, exactly like `MemberHistoryPage.tsx`).
- **i18n/RTL** — `mobile/lib/l10n/strings.dart` + `app_localizations.dart`: a
  hand-ported flat key→string map for `ar`/`en` copied verbatim from
  `frontend/src/i18n/{ar,en}.json` (including `{placeholder}` interpolation),
  with a `context.t('circle.recordPayment')` accessor. RTL is driven by
  `Directionality` + `Locale('ar')`, matching the web app's default language.
  **Deviation from the plan:** ARB files + `flutter gen-l10n` were not used —
  the sandbox cannot run that code-generation step, so a zero-codegen flat map
  was used instead. Functionally equivalent; a future pass with a working
  Flutter toolchain could migrate this to ARB without changing any screen code.
- **Theming** (`mobile/lib/theme/app_theme.dart`) — Material 3 theme built from
  the same seed color (`#1F8A70`), same `12px` corner radius, ported from
  `frontend/src/theme/theme.ts`.
- **WhatsApp integration** (`mobile/lib/utils/whatsapp.dart`) — `url_launcher`
  opens the same `wa.me/<phone>?text=...` scheme with the same message
  templates (share status, per-member reminder, invite-to-register) as
  `frontend/src/utils/whatsapp.ts`.

### Judgment calls / deviations (none silently skipped)

- **No `json_serializable`/`build_runner`** — despite §2 listing it, models use
  hand-written `fromJson` instead. Reason: `build_runner` needs the Dart SDK to
  run, which isn't available here to verify generated code compiles; hand-written
  parsing has zero build step and is easy to audit line-by-line against
  `types.ts`. If a future contributor wants generated code, this is a
  mechanical migration.
- **No ARB/`gen-l10n`** — see i18n note above; same "no SDK to run codegen"
  reasoning.
- **Payout Order reordering uses up/down icon buttons, not drag-and-drop** —
  `ReorderableListView` is more fragile to hand-verify without a running
  emulator; up/down buttons produce the identical `setManualOrder` API call
  and are equally usable on a touchscreen.
- **Evidence viewing is simplified** — `PaymentClaimsApi.evidenceBytes()`
  downloads the authenticated bytes (mirroring the web app's blob-URL fetch),
  but the mobile UI only confirms the download rather than rendering an
  in-app image/PDF viewer; wiring a full-screen viewer is straightforward but
  was deprioritized to keep scope moving — noted here rather than silently
  dropped.
- **`buildInviteToRegisterText` uses a fixed app URL** (`dourak_app_url` =
  `https://dourak.app`, in `mobile/lib/utils/whatsapp.dart`) instead of
  `window.location.origin` (meaningless on a native app). **Whoever deploys
  this should replace that constant with the real production web app URL.**
- **Debug builds are debug-signed** (`android/app/build.gradle` sets the
  release build type to use the debug signing config) since Play Store
  signing/publishing is explicitly out of scope (§5).
- **Launcher icon is a solid-color placeholder** (`android/app/src/main/res/
  mipmap-*/ic_launcher.png`, generated via a one-off PowerShell/.NET
  `System.Drawing` script since no Flutter SDK/`flutter_launcher_icons` was
  available) — replace with real branding before shipping.

### How to run

```bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5210/api
```

- `10.0.2.2` is the Android emulator's alias for the host machine — this is
  the default baked into `mobile/lib/api/api_client.dart` if `API_BASE_URL`
  isn't passed, so a plain `flutter run` against the emulator with the backend
  running locally works with no extra flags. Adjust the port to match the
  backend's actual listen port (see `backend/src/Dourak.Api`'s launch
  settings / `docker-compose.yml`).
- On a physical device on the same Wi-Fi as the dev machine, pass the
  machine's LAN IP instead, e.g. `--dart-define=API_BASE_URL=http://192.168.1.50:5210/api`.
- For a release build pointed at production:
  `flutter build apk --dart-define=API_BASE_URL=https://<prod-domain>/api`.

### Package versions pinned (`mobile/pubspec.yaml`)

`dio ^5.4.3+1`, `flutter_riverpod ^2.5.1`, `go_router ^14.2.0`,
`flutter_secure_storage ^9.2.2`, `intl ^0.19.0`, `url_launcher ^6.3.0`,
`file_picker ^8.0.6`, `flutter_lints ^4.0.0`. SDK constraint `>=3.3.0 <4.0.0`
(needs Dart 3.3+ for the record types used in a couple of provider families).

### Verification — what could and couldn't be checked

**Could not run `flutter analyze` or `flutter build apk --debug`.** This
sandbox has no Flutter/Dart SDK installed (`flutter`/`dart` are absent from
PATH and no SDK directory was found anywhere on the machine), and installing
one was outside what could be done here. Per the task instructions, all
source code was still written completely and as correctly as possible from a
static-review standpoint:

- Every import was cross-checked against what it uses.
- Every provider/widget file was manually re-read for null-safety issues
  (e.g. `AsyncValue.valueOrNull` instead of the deprecated `.asData`,
  explicit `!`/`?` handling around nullable API fields).
- The full Android `android/` project (Gradle files, manifest, `MainActivity`,
  styles, launcher icons) was hand-written to match current `flutter create
  --platforms=android` output (AGP 8.3.0, Kotlin 1.9.22, compileSdk/targetSdk
  34, minSdk 23, Flutter embedding v2), so a `flutter pub get` followed by
  `flutter build apk --debug` on a machine with the Flutter SDK installed is
  the expected next verification step — it was not possible from here.
- **Whoever picks this up next should, as the very first step, run
  `flutter analyze` and `flutter build apk --debug` inside `mobile/` and fix
  whatever surfaces** — hand-written Dart across ~25 files with no compiler
  in the loop is very likely to have at least a few small mistakes (a typo'd
  identifier, a missing import, a widget constructor argument) even though
  the logic and API contracts were carefully cross-checked against the React
  source. `docs/mobile-plan.md` §6 is marked "PARTIALLY DONE" for exactly
  this reason — the code is complete but its build has not been confirmed.

### Out of scope (per plan §5, unchanged)

iOS build/signing, Play Store publishing/signing, push notifications,
offline/local caching beyond in-memory Riverpod state, automated widget/
integration tests.
