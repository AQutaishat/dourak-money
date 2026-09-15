using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Common.Behaviors;

/// <summary>
/// Phase 2 visibility rule (prompt02 "Member visibility"): any member who has *accepted* their
/// invitation can read everything about their circle — schedule, payments, member list — the
/// same view the organizer sees. They can never mutate anything: write requests keep using
/// <see cref="ICircleOwnedRequest"/> and stay organizer-only.
///
/// Two markers instead of one flag on every handler keeps "who may read" and "who may write"
/// impossible to confuse, and keeps the check in one place exactly like Phase 1 did.
/// </summary>
public interface ICircleReadRequest
{
    int CircleId { get; }
}

public class CircleReadAccessBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CircleReadAccessBehavior(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // A request that is already organizer-owned is covered by CircleOwnershipBehavior; don't check twice.
        if (request is ICircleReadRequest readable and not ICircleOwnedRequest)
        {
            var circle = await _db.Circles
                .Where(c => c.Id == readable.CircleId)
                .Select(c => new { c.OrganizerUserId })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("SavingsCircle", readable.CircleId);

            if (circle.OrganizerUserId != _currentUser.UserId)
            {
                var isAcceptedMember = await _db.CircleMembers.AnyAsync(
                    m => m.CircleId == readable.CircleId
                         && m.UserId == _currentUser.UserId
                         && m.IsActive
                         && m.InvitationStatus == InvitationStatus.Accepted,
                    cancellationToken);

                if (!isAcceptedMember)
                    throw new ForbiddenAccessException("You do not have access to this Savings Circle.");
            }
        }

        return await next();
    }
}
