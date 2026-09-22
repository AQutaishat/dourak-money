namespace Dourak.Application.Common.Interfaces;

/// <summary>Exposes the authenticated organizer's identity to Application handlers.</summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? DisplayName { get; }

    /// <summary>Caller's IP address, if available (used for the audit trail). Best-effort only.</summary>
    string? IpAddress { get; }
}
