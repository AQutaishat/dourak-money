namespace Dourak.Application.Common.Interfaces;

/// <summary>Exposes the authenticated organizer's identity to Application handlers.</summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? DisplayName { get; }
}
