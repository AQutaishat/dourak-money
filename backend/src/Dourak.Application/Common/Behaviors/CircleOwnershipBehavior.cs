using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Common.Behaviors;

/// <summary>
/// Marker for any request that operates on a specific circle. A single MediatR
/// pipeline behavior then enforces "only the organizer who owns this circle can
/// act on it" everywhere, instead of repeating the check in every handler.
/// </summary>
public interface ICircleOwnedRequest
{
    int CircleId { get; }
}

public class CircleOwnershipBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CircleOwnershipBehavior(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ICircleOwnedRequest owned)
        {
            var organizerUserId = await _db.Circles
                .Where(c => c.Id == owned.CircleId)
                .Select(c => c.OrganizerUserId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("SavingsCircle", owned.CircleId);

            if (organizerUserId != _currentUser.UserId)
                throw new ForbiddenAccessException("You do not have access to this Savings Circle.");
        }

        return await next();
    }
}
