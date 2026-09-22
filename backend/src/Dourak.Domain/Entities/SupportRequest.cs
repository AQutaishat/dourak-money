using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// A message submitted through the public "/support" page — no sign-in required, since someone
/// locked out of their account still needs a way to reach out. <see cref="AuditableEntity.CreatedAt"/>
/// is the request's submission date, shown to admins reviewing the queue.
/// </summary>
public class SupportRequest : AuditableEntity
{
    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Reuses the same disk-backed store as payment-claim evidence (<see cref="Common.Interfaces.IEvidenceFileStorage"/>) — same size/type limits, no need for a second storage seam.</summary>
    public string? AttachmentStoredFileName { get; set; }
    public string? AttachmentOriginalFileName { get; set; }
    public string? AttachmentContentType { get; set; }

    public bool HasAttachment => !string.IsNullOrEmpty(AttachmentStoredFileName);
}
