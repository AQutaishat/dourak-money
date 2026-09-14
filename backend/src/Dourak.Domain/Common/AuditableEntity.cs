namespace Dourak.Domain.Common;

/// <summary>
/// Base class for entities that need creation/modification traceability.
/// Business rule: financial records must never silently lose history (BRD rule #6, #9).
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
