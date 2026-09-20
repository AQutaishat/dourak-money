using Dourak.Application.Auth;
using Dourak.Application.Circles.Dtos;
using Dourak.Application.Common.Behaviors;
using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Queries;

/// <summary>
/// Shared helpers for the circle read model. Phase 2 added two cross-cutting needs to every
/// query: resolving organizer *names* (they live in Identity, not in the circle tables) and
/// knowing whether the caller is the organizer or a view-only member.
/// </summary>
internal static class CircleQueryHelpers
{
    public static async Task<Dictionary<string, string>> ResolveOrganizerNamesAsync(
        IIdentityService identity, IEnumerable<string> organizerUserIds)
    {
        var ids = organizerUserIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();
        var profiles = await identity.GetProfilesAsync(ids);
        return profiles.ToDictionary(p => p.UserId, p => p.DisplayLabel);
    }

    /// <summary>prompt02 §Draft circles: last payment month = start date + (participants − 1) months.</summary>
    public static DateOnly? ComputeLastPaymentMonth(DateOnly startDate, int participantCount) =>
        participantCount <= 0 ? null : startDate.AddMonths(participantCount - 1);
}

// ---------- My Circles ----------

/// <summary>
/// Phase 2: returns circles I organize AND circles I have accepted an invitation to
/// (prompt02 "Member visibility" — accepted members log in and see their circle).
/// </summary>
public record GetMyCirclesQuery : IRequest<IReadOnlyList<CircleSummaryDto>>;

public class GetMyCirclesQueryHandler : IRequestHandler<GetMyCirclesQuery, IReadOnlyList<CircleSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public GetMyCirclesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<IReadOnlyList<CircleSummaryDto>> Handle(GetMyCirclesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var circles = await _db.Circles
            .Where(c => c.OrganizerUserId == userId
                        || c.Members.Any(m => m.UserId == userId && m.IsActive && m.InvitationStatus == InvitationStatus.Accepted))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id, c.Name, c.Currency, c.ContributionAmount, c.Frequency, c.StartDate, c.Status,
                c.OrganizerUserId, c.CreatedAt,
                MemberCount = c.Members.Count(m => m.IsActive && (m.InvitationStatus == InvitationStatus.NotInvited || m.InvitationStatus == InvitationStatus.Accepted))
            })
            .ToListAsync(cancellationToken);

        var organizerNames = await CircleQueryHelpers.ResolveOrganizerNamesAsync(_identity, circles.Select(c => c.OrganizerUserId));

        return circles.Select(c => new CircleSummaryDto(
            c.Id, c.Name, c.Currency, c.ContributionAmount, c.Frequency.ToString(), c.StartDate,
            c.Status.ToString(), c.MemberCount,
            organizerNames.GetValueOrDefault(c.OrganizerUserId, "—"),
            c.CreatedAt,
            c.OrganizerUserId == userId)).ToList();
    }
}

// ---------- Circle Detail ----------

public record GetCircleDetailQuery(int CircleId) : IRequest<CircleDetailDto>, ICircleReadRequest;

public class GetCircleDetailQueryHandler : IRequestHandler<GetCircleDetailQuery, CircleDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public GetCircleDetailQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<CircleDetailDto> Handle(GetCircleDetailQuery request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles
            .Include(c => c.Members)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Contributions)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Payout)
            .FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        var participantCount = circle.Members.Count(m => m.IsParticipating);
        var organizerNames = await CircleQueryHelpers.ResolveOrganizerNamesAsync(_identity, new[] { circle.OrganizerUserId });

        var hasPayments = circle.Cycles.Any(cy =>
            cy.Contributions.Any(co => co.PaidAmount > 0) || (cy.Payout != null && cy.Payout.Status == PayoutStatus.Paid));

        var myMemberId = circle.Members
            .Where(m => m.UserId != null && m.UserId == _currentUser.UserId)
            .Select(m => (int?)m.Id).FirstOrDefault();

        return new CircleDetailDto(
            circle.Id, circle.Name, circle.Description, circle.Currency, circle.ContributionAmount,
            circle.Frequency.ToString(), circle.StartDate, circle.Status.ToString(),
            circle.PayoutOrderMethod?.ToString(), circle.PayoutOrderConfirmed,
            participantCount,
            organizerNames.GetValueOrDefault(circle.OrganizerUserId, "—"),
            circle.CreatedAt,
            circle.OrganizerUserId == _currentUser.UserId,
            participantCount * circle.ContributionAmount,
            CircleQueryHelpers.ComputeLastPaymentMonth(circle.StartDate, participantCount),
            !hasPayments,
            myMemberId);
    }
}

// ---------- Members ----------

public record GetMembersQuery(int CircleId) : IRequest<IReadOnlyList<MemberDto>>, ICircleReadRequest;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, IReadOnlyList<MemberDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMembersQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var isOrganizer = await _db.Circles
            .AnyAsync(c => c.Id == request.CircleId && c.OrganizerUserId == _currentUser.UserId, cancellationToken);

        var members = await _db.CircleMembers
            .Where(m => m.CircleId == request.CircleId)
            .Select(m => new
            {
                m.Id, m.Name, m.Phone, m.Email, m.Notes, m.IsActive, m.UserId, m.InvitationStatus,
                Position = _db.PayoutPositions.Where(p => p.MemberId == m.Id).Select(p => (int?)p.Position).FirstOrDefault()
            })
            // Every member-facing table shows members in their circle payout order, not join
            // order — a member without an assigned position yet (Draft, before the order is set)
            // falls back to join order at the end.
            .ToListAsync(cancellationToken);
        members = members.OrderBy(m => m.Position ?? int.MaxValue).ThenBy(m => m.Id).ToList();

        // prompt02 privacy decision: members may see names, status, schedule and the membership
        // list — but NOT other members' phone/email. The organizer still sees everything, and a
        // member always sees their own contact details.
        return members.Select(m =>
        {
            var maySeeContact = isOrganizer || (m.UserId != null && m.UserId == _currentUser.UserId);
            return new MemberDto(
                m.Id, m.Name,
                maySeeContact ? m.Phone : null,
                maySeeContact ? m.Email : null,
                maySeeContact ? m.Notes : null,
                m.IsActive, m.Position,
                m.InvitationStatus.ToString(), m.UserId,
                m.IsActive && (m.InvitationStatus == InvitationStatus.NotInvited || m.InvitationStatus == InvitationStatus.Accepted));
        }).ToList();
    }
}

// ---------- Payout Order Preview ----------

public record GetPayoutOrderQuery(int CircleId) : IRequest<IReadOnlyList<PayoutOrderEntryDto>>, ICircleReadRequest;

public class GetPayoutOrderQueryHandler : IRequestHandler<GetPayoutOrderQuery, IReadOnlyList<PayoutOrderEntryDto>>
{
    private readonly IAppDbContext _db;
    public GetPayoutOrderQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayoutOrderEntryDto>> Handle(GetPayoutOrderQuery request, CancellationToken cancellationToken) =>
        await _db.PayoutPositions
            .Where(p => p.CircleId == request.CircleId)
            .OrderBy(p => p.Position)
            .Select(p => new PayoutOrderEntryDto(p.Position, p.MemberId, p.Member!.Name))
            .ToListAsync(cancellationToken);
}

// ---------- Schedule ----------

public record GetScheduleQuery(int CircleId) : IRequest<IReadOnlyList<ScheduleCycleDto>>, ICircleReadRequest;

public class GetScheduleQueryHandler : IRequestHandler<GetScheduleQuery, IReadOnlyList<ScheduleCycleDto>>
{
    private readonly IAppDbContext _db;
    public GetScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ScheduleCycleDto>> Handle(GetScheduleQuery request, CancellationToken cancellationToken) =>
        await _db.Cycles
            .Where(c => c.CircleId == request.CircleId)
            .OrderBy(c => c.SequenceNumber)
            .Select(c => new ScheduleCycleDto(
                c.Id, c.SequenceNumber, c.DueDate, c.RecipientMemberId, c.Recipient!.Name,
                c.ExpectedPoolAmount, c.Contributions.Sum(co => co.PaidAmount), c.Status.ToString(), c.Payout!.Status.ToString()))
            .ToListAsync(cancellationToken);
}

// ---------- Full per-month schedule detail ----------

/// <summary>Every cycle's full per-member payment breakdown, for the redesigned Schedule tab.</summary>
public record GetCircleMonthsDetailQuery(int CircleId) : IRequest<IReadOnlyList<CircleMonthDto>>, ICircleReadRequest;

public class GetCircleMonthsDetailQueryHandler : IRequestHandler<GetCircleMonthsDetailQuery, IReadOnlyList<CircleMonthDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCircleMonthsDetailQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CircleMonthDto>> Handle(GetCircleMonthsDetailQuery request, CancellationToken cancellationToken)
    {
        var cycles = await _db.Cycles
            .Where(c => c.CircleId == request.CircleId)
            .Include(c => c.Recipient)
            .Include(c => c.Payout).ThenInclude(p => p!.Payments)
            .Include(c => c.Contributions).ThenInclude(co => co.Member)
            .Include(c => c.Contributions).ThenInclude(co => co.Payments)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        var isOrganizer = await _db.Circles
            .AnyAsync(c => c.Id == request.CircleId && c.OrganizerUserId == _currentUser.UserId, cancellationToken);

        var claimsByContribution = await _db.PaymentClaims
            .Where(pc => pc.CircleId == request.CircleId)
            .ToListAsync(cancellationToken);
        var claimsLookup = claimsByContribution.ToLookup(c => c.ContributionId);

        // Every member-facing table shows members in their circle payout order, not whatever
        // order the contributions happen to be stored in.
        var positions = await _db.PayoutPositions
            .Where(p => p.CircleId == request.CircleId)
            .ToDictionaryAsync(p => p.MemberId, p => p.Position, cancellationToken);

        return cycles.Select(c => new CircleMonthDto(
            c.Id, c.SequenceNumber, c.DueDate, c.RecipientMemberId, c.Recipient!.Name,
            c.ExpectedPoolAmount, c.Contributions.Sum(co => co.PaidAmount), c.Status.ToString(), c.Payout!.Status.ToString(),
            c.Payout!.ExpectedAmount, c.Payout!.ActualAmount ?? 0m, c.Payout!.PaidAt,
            c.Payout!.Payments.OrderBy(p => p.PaidAt)
                .Select(p => new PayoutRowDto(p.Id, p.Amount, p.PaidAt, p.HasEvidence)).ToList(),
            c.Contributions
                .OrderBy(co => positions.TryGetValue(co.MemberId, out var pos) ? pos : int.MaxValue)
                .ThenBy(co => co.MemberId)
                .Select(co => BuildMemberRow(co, claimsLookup[co.Id].ToList(), isOrganizer)).ToList()
        )).ToList();
    }

    private CircleMonthMemberDto BuildMemberRow(Contribution co, List<PaymentClaim> claims, bool isOrganizer)
    {
        // prompt02 §6 privacy rule: a claim (its status, and its very existence) is visible only
        // to the organizer and to the member who submitted it — everyone else sees only the
        // plain paid amount, with no indication it came from a claim.
        var maySeeClaim = isOrganizer || (co.Member!.UserId != null && co.Member.UserId == _currentUser.UserId);

        var rows = new List<PaymentRowDto>();
        foreach (var payment in co.Payments.OrderBy(p => p.PaidAt))
        {
            var linkedClaim = payment.PaymentClaimId is int claimId ? claims.FirstOrDefault(c => c.Id == claimId) : null;
            rows.Add(maySeeClaim && linkedClaim is not null
                ? new PaymentRowDto(payment.Amount, payment.PaidAt, linkedClaim.Status.ToString(), linkedClaim.Id)
                : new PaymentRowDto(payment.Amount, payment.PaidAt, null, null));
        }

        if (maySeeClaim)
        {
            // Claims that never (yet) produced a payment — still Pending, or Rejected — get their
            // own row so the member/organizer can see them, but they don't count toward PaidAmount
            // since no ContributionPayment exists for them.
            var linkedClaimIds = co.Payments.Where(p => p.PaymentClaimId.HasValue).Select(p => p.PaymentClaimId!.Value).ToHashSet();
            foreach (var claim in claims.Where(c => !linkedClaimIds.Contains(c.Id)))
                rows.Add(new PaymentRowDto(claim.ClaimedAmount, claim.SubmittedAt, claim.Status.ToString(), claim.Id));
        }

        rows = rows.OrderBy(r => r.Date).ToList();

        return new CircleMonthMemberDto(co.MemberId, co.Member!.Name, co.Member!.Email, co.ExpectedAmount, co.PaidAmount, co.PaidAt, rows);
    }
}

// ---------- Current Cycle Dashboard ----------

public record GetCurrentCycleDashboardQuery(int CircleId) : IRequest<CurrentCycleDashboardDto?>, ICircleReadRequest;

public class GetCurrentCycleDashboardQueryHandler : IRequestHandler<GetCurrentCycleDashboardQuery, CurrentCycleDashboardDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentCycleDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CurrentCycleDashboardDto?> Handle(GetCurrentCycleDashboardQuery request, CancellationToken cancellationToken)
    {
        var cycles = await _db.Cycles
            .Where(c => c.CircleId == request.CircleId)
            .Include(c => c.Recipient)
            .Include(c => c.Payout).ThenInclude(p => p!.Payments)
            .Include(c => c.Contributions).ThenInclude(co => co.Member)
            .Include(c => c.Contributions).ThenInclude(co => co.Payments)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (cycles.Count == 0) return null;

        // The "current" cycle is date-driven, not tied to whether its payout was confirmed: the
        // most recent cycle whose collection date has already arrived, defaulting to the first
        // cycle before anything is due yet, and staying on the last cycle forever after its own
        // due date has passed (there is nothing further to advance to).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var current = cycles.Where(c => c.DueDate <= today).OrderByDescending(c => c.SequenceNumber).FirstOrDefault()
            ?? cycles[0];

        // A member can record a payment against any future cycle's contribution, ahead of
        // schedule (Monthly Cycles tab) — if that's how this now-current cycle got fully paid,
        // flag it so the UI can say so instead of looking like an ordinary just-in-time payment.
        var previousCycle = cycles.FirstOrDefault(c => c.SequenceNumber == current.SequenceNumber - 1);
        var previousDueDate = previousCycle?.DueDate.ToDateTime(TimeOnly.MinValue);

        var isOrganizer = await _db.Circles
            .AnyAsync(c => c.Id == request.CircleId && c.OrganizerUserId == _currentUser.UserId, cancellationToken);

        var claims = await _db.PaymentClaims
            .Where(pc => pc.Contribution!.CycleId == current.Id)
            .Select(pc => new { pc.MemberId, pc.Status, pc.SubmittedAt })
            .ToListAsync(cancellationToken);

        // Every member-facing table shows members in their circle payout order, not whatever
        // order the contributions happen to be stored in.
        var positions = await _db.PayoutPositions
            .Where(p => p.CircleId == request.CircleId)
            .ToDictionaryAsync(p => p.MemberId, p => p.Position, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var dueDate = current.DueDate.ToDateTime(TimeOnly.MinValue);

        var rows = current.Contributions
            .OrderBy(co => positions.TryGetValue(co.MemberId, out var pos) ? pos : int.MaxValue)
            .ThenBy(co => co.MemberId)
            .Select(co =>
        {
            // prompt02 §6 privacy rule: claim state travels only to the organizer and to the
            // member who submitted it. Everyone else sees payment status and nothing more.
            var maySeeClaim = isOrganizer || (co.Member!.UserId != null && co.Member.UserId == _currentUser.UserId);
            var latest = maySeeClaim
                ? claims.Where(c => c.MemberId == co.MemberId).OrderByDescending(c => c.SubmittedAt).FirstOrDefault()
                : null;

            var paidInAdvance = false;
            if (previousDueDate.HasValue && co.PaidAmount >= co.ExpectedAmount)
            {
                var lastPaymentAt = co.Payments.Count > 0 ? co.Payments.Max(p => p.PaidAt) : co.PaidAt;
                paidInAdvance = lastPaymentAt.HasValue && lastPaymentAt.Value < previousDueDate.Value;
            }

            return new CurrentCycleMemberRowDto(
                co.MemberId, co.Member!.Name, co.ExpectedAmount, co.PaidAmount,
                co.ComputeDisplayStatus(dueDate, now), co.PaidAt,
                latest?.Status.ToString(),
                latest is not null && latest.Status == PaymentClaimStatus.Pending,
                paidInAdvance);
        }).ToList();

        var next = cycles.FirstOrDefault(c => c.SequenceNumber == current.SequenceNumber + 1);

        return new CurrentCycleDashboardDto(
            current.Id, current.SequenceNumber, current.DueDate,
            MembersTotal: rows.Count,
            MembersPaid: rows.Count(r => r.Status == "Paid"),
            MembersUnpaid: rows.Count(r => r.Status == "Unpaid"),
            MembersLate: rows.Count(r => r.Status == "Late"),
            Collected: rows.Sum(r => r.PaidAmount),
            Expected: rows.Sum(r => r.ExpectedAmount),
            Outstanding: rows.Sum(r => r.ExpectedAmount - r.PaidAmount),
            RecipientMemberId: current.RecipientMemberId,
            RecipientName: current.Recipient!.Name,
            PayoutStatus: current.Payout!.Status.ToString(),
            NextRecipientMemberId: next?.RecipientMemberId,
            NextRecipientName: next?.Recipient?.Name,
            Members: rows,
            PendingClaimCount: isOrganizer ? claims.Count(c => c.Status == PaymentClaimStatus.Pending) : 0,
            PayoutExpectedAmount: current.Payout!.ExpectedAmount,
            PayoutActualAmount: current.Payout.ActualAmount ?? 0m,
            PayoutRows: current.Payout.Payments.OrderBy(p => p.PaidAt)
                .Select(p => new PayoutRowDto(p.Id, p.Amount, p.PaidAt, p.HasEvidence)).ToList());
    }
}

// ---------- Member History ----------

public record GetMemberHistoryQuery(int CircleId, int MemberId) : IRequest<MemberHistoryDto>, ICircleReadRequest;

public class GetMemberHistoryQueryHandler : IRequestHandler<GetMemberHistoryQuery, MemberHistoryDto>
{
    private readonly IAppDbContext _db;
    public GetMemberHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<MemberHistoryDto> Handle(GetMemberHistoryQuery request, CancellationToken cancellationToken)
    {
        var member = await _db.CircleMembers.FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CircleId == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        var position = await _db.PayoutPositions.Where(p => p.MemberId == member.Id).Select(p => (int?)p.Position).FirstOrDefaultAsync(cancellationToken);

        var contributions = await _db.Contributions
            .Where(c => c.MemberId == member.Id)
            .Include(c => c.Cycle).ThenInclude(cy => cy!.Payout)
            .OrderBy(c => c.Cycle!.SequenceNumber)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entries = contributions.Select(c =>
        {
            var due = c.Cycle!.DueDate.ToDateTime(TimeOnly.MinValue);
            var isRecipient = c.Cycle.RecipientMemberId == member.Id;
            return new MemberHistoryEntryDto(
                c.CycleId, c.Cycle.SequenceNumber, c.Cycle.DueDate, c.ExpectedAmount, c.PaidAmount,
                c.ComputeDisplayStatus(due, now), c.PaidAt, isRecipient,
                isRecipient ? c.Cycle.Payout?.Status.ToString() : null);
        }).ToList();

        return new MemberHistoryDto(member.Id, member.Name, position, entries);
    }
}

// ---------- Circle History (past/completed cycles) ----------

public record GetCircleHistoryQuery(int CircleId) : IRequest<IReadOnlyList<CircleHistoryCycleDto>>, ICircleReadRequest;

public class GetCircleHistoryQueryHandler : IRequestHandler<GetCircleHistoryQuery, IReadOnlyList<CircleHistoryCycleDto>>
{
    private readonly IAppDbContext _db;
    public GetCircleHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CircleHistoryCycleDto>> Handle(GetCircleHistoryQuery request, CancellationToken cancellationToken)
    {
        var cycles = await _db.Cycles
            .Where(c => c.CircleId == request.CircleId && c.Status == CycleStatus.Completed)
            .Include(c => c.Recipient)
            .Include(c => c.Payout)
            .Include(c => c.Contributions).ThenInclude(co => co.Member)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return cycles.Select(c =>
        {
            var due = c.DueDate.ToDateTime(TimeOnly.MinValue);
            var unpaid = c.Contributions.Where(co => co.ComputeDisplayStatus(due, now) == "Unpaid").Select(co => co.Member!.Name).ToList();
            var late = c.Contributions.Where(co => co.ComputeDisplayStatus(due, now) == "Late").Select(co => co.Member!.Name).ToList();
            return new CircleHistoryCycleDto(
                c.Id, c.SequenceNumber, c.DueDate, c.Recipient!.Name, c.ExpectedPoolAmount,
                c.Contributions.Sum(co => co.PaidAmount), unpaid, late,
                c.Payout!.Status.ToString(), c.Payout.PaidAt);
        }).ToList();
    }
}

// ---------- Phase 2: pending invitations for the signed-in user (prompt02 §4) ----------

public record GetMyPendingInvitationsQuery : IRequest<IReadOnlyList<PendingInvitationDto>>;

public class GetMyPendingInvitationsQueryHandler : IRequestHandler<GetMyPendingInvitationsQuery, IReadOnlyList<PendingInvitationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public GetMyPendingInvitationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<IReadOnlyList<PendingInvitationDto>> Handle(GetMyPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var rows = await _db.CircleMembers
            .Where(m => m.UserId == userId && m.IsActive && m.InvitationStatus == InvitationStatus.Pending)
            .Select(m => new
            {
                MemberId = m.Id,
                m.InvitedAt,
                m.CircleId,
                CircleName = m.Circle!.Name,
                m.Circle.Description,
                m.Circle.Currency,
                m.Circle.ContributionAmount,
                m.Circle.StartDate,
                m.Circle.OrganizerUserId,
                MemberCount = m.Circle.Members.Count(x => x.IsActive && (x.InvitationStatus == InvitationStatus.NotInvited || x.InvitationStatus == InvitationStatus.Accepted))
            })
            .OrderByDescending(r => r.InvitedAt)
            .ToListAsync(cancellationToken);

        var organizerNames = await CircleQueryHelpers.ResolveOrganizerNamesAsync(_identity, rows.Select(r => r.OrganizerUserId));

        return rows.Select(r => new PendingInvitationDto(
            r.CircleId, r.MemberId, r.CircleName, r.Description, r.Currency, r.ContributionAmount,
            r.StartDate, organizerNames.GetValueOrDefault(r.OrganizerUserId, "—"), r.MemberCount, r.InvitedAt)).ToList();
    }
}

// ---------- Phase 2: payment claims (prompt02 §6) ----------

/// <summary>
/// Claims for one circle. The privacy rule is enforced here rather than in the controller:
/// the organizer sees every claim in their circle; a member sees only their own.
/// </summary>
public record GetCirclePaymentClaimsQuery(int CircleId, bool PendingOnly = false)
    : IRequest<IReadOnlyList<PaymentClaimDto>>, ICircleReadRequest;

public class GetCirclePaymentClaimsQueryHandler : IRequestHandler<GetCirclePaymentClaimsQuery, IReadOnlyList<PaymentClaimDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCirclePaymentClaimsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PaymentClaimDto>> Handle(GetCirclePaymentClaimsQuery request, CancellationToken cancellationToken)
    {
        var isOrganizer = await _db.Circles
            .AnyAsync(c => c.Id == request.CircleId && c.OrganizerUserId == _currentUser.UserId, cancellationToken);

        var query = _db.PaymentClaims
            .Where(pc => pc.CircleId == request.CircleId)
            .Include(pc => pc.Contribution).ThenInclude(c => c!.Cycle).ThenInclude(cy => cy!.Circle)
            .Include(pc => pc.Member)
            .AsQueryable();

        if (!isOrganizer)
            query = query.Where(pc => pc.SubmittedByUserId == _currentUser.UserId);

        if (request.PendingOnly)
            query = query.Where(pc => pc.Status == PaymentClaimStatus.Pending);

        var claims = await query.OrderByDescending(pc => pc.SubmittedAt).ToListAsync(cancellationToken);
        return claims.Select(PaymentClaimMapper.ToDto).ToList();
    }
}

/// <summary>Claims submitted by the signed-in user across all their circles (their own inbox).</summary>
public record GetMyPaymentClaimsQuery : IRequest<IReadOnlyList<PaymentClaimDto>>;

public class GetMyPaymentClaimsQueryHandler : IRequestHandler<GetMyPaymentClaimsQuery, IReadOnlyList<PaymentClaimDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyPaymentClaimsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PaymentClaimDto>> Handle(GetMyPaymentClaimsQuery request, CancellationToken cancellationToken)
    {
        var claims = await _db.PaymentClaims
            .Where(pc => pc.SubmittedByUserId == _currentUser.UserId)
            .Include(pc => pc.Contribution).ThenInclude(c => c!.Cycle).ThenInclude(cy => cy!.Circle)
            .Include(pc => pc.Member)
            .OrderByDescending(pc => pc.SubmittedAt)
            .ToListAsync(cancellationToken);

        return claims.Select(PaymentClaimMapper.ToDto).ToList();
    }
}

internal static class PaymentClaimMapper
{
    public static PaymentClaimDto ToDto(PaymentClaim pc)
    {
        var cycle = pc.Contribution?.Cycle;
        var circle = cycle?.Circle;
        return new PaymentClaimDto(
            pc.Id, pc.CircleId, circle?.Name ?? string.Empty,
            cycle?.Id ?? 0, cycle?.SequenceNumber ?? 0, cycle?.DueDate ?? default,
            pc.MemberId, pc.Member?.Name ?? string.Empty,
            pc.ClaimedAmount, pc.Contribution?.ExpectedAmount ?? 0m, circle?.Currency ?? string.Empty,
            pc.Status.ToString(), pc.Note, pc.RejectionReason,
            pc.SubmittedAt, pc.ReviewedAt,
            pc.HasEvidence, pc.EvidenceOriginalFileName, pc.EvidenceContentType);
    }
}
