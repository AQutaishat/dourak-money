namespace Dourak.Domain.Entities;

/// <summary>
/// A single recorded user/admin action, backing the admin site's audit trail. Written either by
/// <c>AuditLoggingBehavior</c> (for MediatR commands marked <c>IAuditableAction</c>) or directly
/// through <c>IAuditLogger</c> for actions that don't go through MediatR (e.g. some AdminController
/// endpoints). Deliberately a flat, append-only row — no FK to AspNetUsers, since the actor's
/// display name is snapshotted at write time and the row must survive user deletion.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>The acting user's Id, if any (e.g. anonymous login failures have none).</summary>
    public string? UserId { get; set; }

    /// <summary>Snapshot of the actor's display name (or email) at the time of the action.</summary>
    public string? UserDisplayName { get; set; }

    /// <summary>Short code identifying the action, e.g. "CircleCreated", "UserLoggedIn".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional free-text/JSON context (e.g. circle id, amounts).</summary>
    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? IpAddress { get; set; }
}
