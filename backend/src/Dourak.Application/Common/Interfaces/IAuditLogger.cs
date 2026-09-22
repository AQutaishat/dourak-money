namespace Dourak.Application.Common.Interfaces;

/// <summary>
/// Writes rows to the admin audit trail (see <see cref="Dourak.Domain.Entities.AuditLog"/>).
/// Most mutating MediatR commands get logged automatically via
/// <c>Common.Behaviors.AuditLoggingBehavior</c> (see <c>IAuditableAction</c>) — this is for the
/// handful of actions that happen outside MediatR (e.g. some AdminController endpoints).
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(string action, string? details = null, CancellationToken cancellationToken = default);
}
