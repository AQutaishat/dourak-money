using Dourak.Application.Circles.Dtos;
using Dourak.Application.Common.Behaviors;
using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Queries;

// ---------- My Circles ----------

public record GetMyCirclesQuery : IRequest<IReadOnlyList<CircleSummaryDto>>;

public class GetMyCirclesQueryHandler : IRequestHandler<GetMyCirclesQuery, IReadOnlyList<CircleSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCirclesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CircleSummaryDto>> Handle(GetMyCirclesQuery request, CancellationToken cancellationToken) =>
        await _db.Circles
            .Where(c => c.OrganizerUserId == _currentUser.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CircleSummaryDto(
                c.Id, c.Name, c.Currency, c.ContributionAmount, c.Frequency.ToString(),
                c.StartDate, c.Status.ToString(), c.Members.Count(m => m.IsActive)))
            .ToListAsync(cancellationToken);
}

// ---------- Circle Detail ----------

public record GetCircleDetailQuery(int CircleId) : IRequest<CircleDetailDto>, ICircleOwnedRequest;

public class GetCircleDetailQueryHandler : IRequestHandler<GetCircleDetailQuery, CircleDetailDto>
{
    private readonly IAppDbContext _db;
    public GetCircleDetailQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CircleDetailDto> Handle(GetCircleDetailQuery request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        return new CircleDetailDto(
            circle.Id, circle.Name, circle.Description, circle.Currency, circle.ContributionAmount,
            circle.Frequency.ToString(), circle.StartDate, circle.Status.ToString(),
            circle.PayoutOrderMethod?.ToString(), circle.PayoutOrderConfirmed,
            circle.Members.Count(m => m.IsActive));
    }
}

// ---------- Members ----------

public record GetMembersQuery(int CircleId) : IRequest<IReadOnlyList<MemberDto>>, ICircleOwnedRequest;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, IReadOnlyList<MemberDto>>
{
    private readonly IAppDbContext _db;
    public GetMembersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken) =>
        await _db.CircleMembers
            .Where(m => m.CircleId == request.CircleId)
            .OrderBy(m => m.Id)
            .Select(m => new MemberDto(m.Id, m.Name, m.Phone, m.Email, m.Notes, m.IsActive,
                _db.PayoutPositions.Where(p => p.MemberId == m.Id).Select(p => (int?)p.Position).FirstOrDefault()))
            .ToListAsync(cancellationToken);
}

// ---------- Payout Order Preview ----------

public record GetPayoutOrderQuery(int CircleId) : IRequest<IReadOnlyList<PayoutOrderEntryDto>>, ICircleOwnedRequest;

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

public record GetScheduleQuery(int CircleId) : IRequest<IReadOnlyList<ScheduleCycleDto>>, ICircleOwnedRequest;

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
                c.ExpectedPoolAmount, c.Status.ToString(), c.Payout!.Status.ToString()))
            .ToListAsync(cancellationToken);
}

// ---------- Current Cycle Dashboard ----------

public record GetCurrentCycleDashboardQuery(int CircleId) : IRequest<CurrentCycleDashboardDto?>, ICircleOwnedRequest;

public class GetCurrentCycleDashboardQueryHandler : IRequestHandler<GetCurrentCycleDashboardQuery, CurrentCycleDashboardDto?>
{
    private readonly IAppDbContext _db;
    public GetCurrentCycleDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CurrentCycleDashboardDto?> Handle(GetCurrentCycleDashboardQuery request, CancellationToken cancellationToken)
    {
        var cycles = await _db.Cycles
            .Where(c => c.CircleId == request.CircleId)
            .Include(c => c.Recipient)
            .Include(c => c.Payout)
            .Include(c => c.Contributions).ThenInclude(co => co.Member)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        var current = cycles.FirstOrDefault(c => c.Status == Domain.Enums.CycleStatus.Pending);
        if (current is null) return null;

        var now = DateTimeOffset.UtcNow;
        var dueDate = current.DueDate.ToDateTime(TimeOnly.MinValue);

        var rows = current.Contributions.Select(co => new CurrentCycleMemberRowDto(
            co.MemberId, co.Member!.Name, co.ExpectedAmount, co.PaidAmount,
            co.ComputeDisplayStatus(dueDate, now), co.PaidAt)).ToList();

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
            Members: rows);
    }
}

// ---------- Member History ----------

public record GetMemberHistoryQuery(int CircleId, int MemberId) : IRequest<MemberHistoryDto>, ICircleOwnedRequest;

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
            .Include(c => c.Cycle)
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

public record GetCircleHistoryQuery(int CircleId) : IRequest<IReadOnlyList<CircleHistoryCycleDto>>, ICircleOwnedRequest;

public class GetCircleHistoryQueryHandler : IRequestHandler<GetCircleHistoryQuery, IReadOnlyList<CircleHistoryCycleDto>>
{
    private readonly IAppDbContext _db;
    public GetCircleHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CircleHistoryCycleDto>> Handle(GetCircleHistoryQuery request, CancellationToken cancellationToken)
    {
        var cycles = await _db.Cycles
            .Where(c => c.CircleId == request.CircleId && c.Status == Domain.Enums.CycleStatus.Completed)
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
