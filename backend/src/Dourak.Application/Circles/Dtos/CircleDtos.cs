namespace Dourak.Application.Circles.Dtos;

public record CircleSummaryDto(
    int Id, string Name, string Currency, decimal ContributionAmount,
    string Frequency, DateOnly StartDate, string Status, int MemberCount,
    // Phase 2: dashboard cards show who created the circle and when (prompt02 §Dashboard).
    string OrganizerName, DateTimeOffset CreatedAt,
    // <summary>False when the current user is a member rather than the circle's organizer.</summary>
    bool IsOrganizer);

public record CircleDetailDto(
    int Id, string Name, string? Description, string Currency, decimal ContributionAmount,
    string Frequency, DateOnly StartDate, string Status, string? PayoutOrderMethod,
    bool PayoutOrderConfirmed, int MemberCount,
    string OrganizerName, DateTimeOffset CreatedAt, bool IsOrganizer,
    // <summary>prompt02 §Draft circles: members count × contribution amount.</summary>
    decimal TotalMonthlyAmount,
    // <summary>prompt02 §Draft circles: start date + (member count − 1) months.</summary>
    DateOnly? LastPaymentMonth,
    // <summary>True while the circle still has zero recorded payments, so it may be deleted.</summary>
    bool CanDelete,
    // <summary>The member row representing the current user in this circle, if any.</summary>
    int? MyMemberId);

public record MemberDto(
    int Id, string Name, string? Phone, string? Email, string? Notes, bool IsActive, int? PayoutPosition,
    // Phase 2 §5
    string InvitationStatus, string? UserId, bool IsParticipating);

public record PayoutOrderEntryDto(int Position, int MemberId, string MemberName);

public record ScheduleCycleDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, int RecipientMemberId, string RecipientName,
    decimal ExpectedPoolAmount, decimal CollectedAmount, string Status, string PayoutStatus);

/// <summary>
/// One row in a member's payment history for a month — either a plain organizer-recorded
/// installment (ClaimStatus/ClaimId null), or a claim at any stage: Pending/Rejected claims never
/// produced a payment (so they don't count toward PaidAmount) but still get their own row;
/// Approved claims are merged with the payment they produced into a single row instead of
/// appearing twice. ClaimStatus/ClaimId are only populated for the organizer or the member's own
/// row (prompt02 §6 privacy rule) — masked to null for anyone else, same as elsewhere.
/// </summary>
public record PaymentRowDto(decimal Amount, DateTimeOffset Date, string? ClaimStatus, int? ClaimId);

public record CircleMonthMemberDto(
    int MemberId, string MemberName, string? Email, decimal ExpectedAmount, decimal PaidAmount,
    // <summary>Null for a contribution paid before per-installment tracking existed — the
    // frontend falls back to showing this single PaidAt/PaidAmount pair in that case.</summary>
    DateTimeOffset? PaidAt,
    IReadOnlyList<PaymentRowDto> PaymentRows);

/// <summary>One payout installment to the recipient — mirrors <see cref="PaymentRowDto"/> but for
/// the recipient's side, with an evidence file reference instead of a claim.</summary>
public record PayoutRowDto(int PayoutPaymentId, decimal Amount, DateTimeOffset Date, bool HasEvidence);

/// <summary>Full per-member breakdown for one month of the circle's schedule (prompt: Schedule tab redesign).</summary>
public record CircleMonthDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, int RecipientMemberId, string RecipientName,
    decimal ExpectedPoolAmount, decimal CollectedAmount, string CycleStatus, string PayoutStatus,
    // <summary>The payout's own running total and expected amount — like a member's contribution,
    // paid in one or more installments (<see cref="PayoutRows"/>), capped at ExpectedAmount.</summary>
    decimal PayoutExpectedAmount, decimal PayoutActualAmount,
    // <summary>Null until at least one installment has been recorded.</summary>
    DateTimeOffset? PayoutPaidAt,
    IReadOnlyList<PayoutRowDto> PayoutRows,
    IReadOnlyList<CircleMonthMemberDto> Members);

public record CurrentCycleMemberRowDto(
    int MemberId, string MemberName, decimal ExpectedAmount, decimal PaidAmount, string Status, DateTimeOffset? PaidAt,
    // <summary>
    // prompt02 §6 privacy rule: only populated for the organizer and for the member's own row —
    // other members never learn that a claim exists, only the payment status.
    // </summary>
    string? MyClaimStatus,
    bool HasPendingClaim,
    // <summary>True when this member finished paying this cycle's contribution while an earlier
    // cycle was still current — i.e. they paid ahead of schedule from the Monthly Cycles tab.</summary>
    bool PaidInAdvance);

public record CurrentCycleDashboardDto(
    int CycleId, int SequenceNumber, DateOnly DueDate,
    int MembersTotal, int MembersPaid, int MembersUnpaid, int MembersLate,
    decimal Collected, decimal Expected, decimal Outstanding,
    int RecipientMemberId, string RecipientName, string PayoutStatus,
    int? NextRecipientMemberId, string? NextRecipientName,
    IReadOnlyList<CurrentCycleMemberRowDto> Members,
    // <summary>Number of payment claims waiting for the organizer's review (organizer only; 0 for members).</summary>
    int PendingClaimCount,
    // <summary>The payout's own expected/paid-so-far totals — like a member's contribution, it can
    // be paid in more than one installment, so "Confirm recipient's receipt" stays available and
    // capped at what's still outstanding until PayoutActualAmount reaches PayoutExpectedAmount.</summary>
    decimal PayoutExpectedAmount, decimal PayoutActualAmount,
    // <summary>Every installment paid to this cycle's recipient so far, same shape as the Monthly
    // Cycles tab's payout line — so the Current Cycle tab can show the identical breakdown.</summary>
    IReadOnlyList<PayoutRowDto> PayoutRows);

public record MemberHistoryEntryDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, decimal ExpectedAmount, decimal PaidAmount,
    string Status, DateTimeOffset? PaidAt, bool IsRecipientThisCycle, string? PayoutStatusIfRecipient);

public record MemberHistoryDto(int MemberId, string MemberName, int? PayoutPosition, IReadOnlyList<MemberHistoryEntryDto> Entries);

public record CircleHistoryCycleDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, string RecipientName,
    decimal ExpectedPool, decimal Collected, IReadOnlyList<string> UnpaidMembers, IReadOnlyList<string> LateMembers,
    string PayoutStatus, DateTimeOffset? PayoutPaidAt);

// ---------- Phase 2 ----------

/// <summary>prompt02 §4: a circle invitation awaiting this user's accept/decline, shown on their home page.</summary>
public record PendingInvitationDto(
    int CircleId, int MemberId, string CircleName, string? Description, string Currency,
    decimal ContributionAmount, DateOnly StartDate, string OrganizerName,
    int MemberCount, DateTimeOffset? InvitedAt);

/// <summary>
/// prompt02 §6: a member's payment self-report. Only ever returned to the submitting member
/// or to the circle's organizer.
/// </summary>
public record PaymentClaimDto(
    int Id, int CircleId, string CircleName, int CycleId, int SequenceNumber, DateOnly DueDate,
    int MemberId, string MemberName, decimal ClaimedAmount, decimal ExpectedAmount, string Currency,
    string Status, string? Note, string? RejectionReason,
    DateTimeOffset SubmittedAt, DateTimeOffset? ReviewedAt,
    bool HasEvidence, string? EvidenceFileName, string? EvidenceContentType);
