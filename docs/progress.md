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
