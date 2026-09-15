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
    decimal ExpectedPoolAmount, string Status, string PayoutStatus);

public record CurrentCycleMemberRowDto(
    int MemberId, string MemberName, decimal ExpectedAmount, decimal PaidAmount, string Status, DateTimeOffset? PaidAt,
    // <summary>
    // prompt02 §6 privacy rule: only populated for the organizer and for the member's own row —
    // other members never learn that a claim exists, only the payment status.
    // </summary>
    string? MyClaimStatus,
    bool HasPendingClaim);

public record CurrentCycleDashboardDto(
    int CycleId, int SequenceNumber, DateOnly DueDate,
    int MembersTotal, int MembersPaid, int MembersUnpaid, int MembersLate,
    decimal Collected, decimal Expected, decimal Outstanding,
    int RecipientMemberId, string RecipientName, string PayoutStatus,
    int? NextRecipientMemberId, string? NextRecipientName,
    IReadOnlyList<CurrentCycleMemberRowDto> Members,
    // <summary>Number of payment claims waiting for the organizer's review (organizer only; 0 for members).</summary>
    int PendingClaimCount);

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
