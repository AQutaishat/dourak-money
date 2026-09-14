namespace Dourak.Application.Circles.Dtos;

public record CircleSummaryDto(
    int Id, string Name, string Currency, decimal ContributionAmount,
    string Frequency, DateOnly StartDate, string Status, int MemberCount);

public record CircleDetailDto(
    int Id, string Name, string? Description, string Currency, decimal ContributionAmount,
    string Frequency, DateOnly StartDate, string Status, string? PayoutOrderMethod,
    bool PayoutOrderConfirmed, int MemberCount);

public record MemberDto(int Id, string Name, string? Phone, string? Email, string? Notes, bool IsActive, int? PayoutPosition);

public record PayoutOrderEntryDto(int Position, int MemberId, string MemberName);

public record ScheduleCycleDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, int RecipientMemberId, string RecipientName,
    decimal ExpectedPoolAmount, string Status, string PayoutStatus);

public record CurrentCycleMemberRowDto(
    int MemberId, string MemberName, decimal ExpectedAmount, decimal PaidAmount, string Status, DateTimeOffset? PaidAt);

public record CurrentCycleDashboardDto(
    int CycleId, int SequenceNumber, DateOnly DueDate,
    int MembersTotal, int MembersPaid, int MembersUnpaid, int MembersLate,
    decimal Collected, decimal Expected, decimal Outstanding,
    int RecipientMemberId, string RecipientName, string PayoutStatus,
    int? NextRecipientMemberId, string? NextRecipientName,
    IReadOnlyList<CurrentCycleMemberRowDto> Members);

public record MemberHistoryEntryDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, decimal ExpectedAmount, decimal PaidAmount,
    string Status, DateTimeOffset? PaidAt, bool IsRecipientThisCycle, string? PayoutStatusIfRecipient);

public record MemberHistoryDto(int MemberId, string MemberName, int? PayoutPosition, IReadOnlyList<MemberHistoryEntryDto> Entries);

public record CircleHistoryCycleDto(
    int CycleId, int SequenceNumber, DateOnly DueDate, string RecipientName,
    decimal ExpectedPool, decimal Collected, IReadOnlyList<string> UnpaidMembers, IReadOnlyList<string> LateMembers,
    string PayoutStatus, DateTimeOffset? PayoutPaidAt);
