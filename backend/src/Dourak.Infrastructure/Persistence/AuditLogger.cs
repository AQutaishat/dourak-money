using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;

namespace Dourak.Infrastructure.Persistence;

/// <summary>
/// Direct-write implementation of <see cref="IAuditLogger"/> for actions taken outside MediatR
/// (see AdminController's user management endpoints, which call IIdentityService directly rather
/// than going through a command that AuditLoggingBehavior could intercept).
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLogger(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string? details = null, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            UserDisplayName = _currentUser.DisplayName,
            Action = action,
            Details = details,
            IpAddress = _currentUser.IpAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
