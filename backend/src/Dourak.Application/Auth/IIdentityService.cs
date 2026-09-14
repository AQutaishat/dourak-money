namespace Dourak.Application.Auth;

public record AuthResult(bool Succeeded, string? UserId, string? Token, DateTimeOffset? ExpiresAt, IReadOnlyList<string> Errors);

/// <summary>
/// Abstraction over ASP.NET Identity + JWT issuing, so Application handlers don't
/// depend directly on Infrastructure/Identity types.
/// </summary>
public interface IIdentityService
{
    Task<AuthResult> RegisterAsync(string name, string email, string password, string preferredLanguage);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<(string? Name, string? Email, string? PreferredLanguage)> GetProfileAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, string name, string preferredLanguage);
}
