using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;

namespace Dourak.Application.Common.Behaviors;

/// <summary>
/// Marker for a MediatR request whose successful handling should be recorded in the admin
/// audit trail. Applied only to mutating commands (never queries) so the trail stays readable —
/// see the commands implementing this across Circles/Auth/Admin for the ones currently covered.
/// </summary>
public interface IAuditableAction
{
    /// <summary>Short code stored in AuditLog.Action, e.g. "CircleCreated".</summary>
    string AuditAction { get; }

    /// <summary>Optional free-text context for the row (e.g. "CircleId=12"). Null is fine.</summary>
    string? AuditDetails => null;
}

public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLoggingBehavior(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableAction auditable)
        {
            // AuthResult-returning commands (login/register) report success in the payload
            // rather than by throwing — a failed attempt shouldn't be silently skipped, but it
            // also isn't the same event, so it gets recorded with an explicit failure marker.
            string? userId = _currentUser.UserId;
            var action = auditable.AuditAction;
            var details = auditable.AuditDetails;

            if (response is AuthResult authResult)
            {
                if (!authResult.Succeeded)
                {
                    action += "Failed";
                }
                else
                {
                    userId ??= authResult.UserId;
                }
            }
            else if (response is OperationResult { Succeeded: false })
            {
                action += "Failed";
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserDisplayName = _currentUser.DisplayName,
                Action = action,
                Details = details,
                IpAddress = _currentUser.IpAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });

            // Best-effort — an audit write failure shouldn't roll back or fail the real action.
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Swallowed deliberately; the primary action already succeeded via `next()`.
            }
        }

        return response;
    }
}
