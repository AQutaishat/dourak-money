namespace Dourak.Application.Auth;

public record AuthResult(bool Succeeded, string? UserId, string? Token, DateTimeOffset? ExpiresAt, IReadOnlyList<string> Errors);

/// <summary>Profile as shown on the account page (prompt02 §8). Email is read-only there.</summary>
public record UserProfileDto(string UserId, string? Name, string? Email, string? Phone, string PreferredLanguage)
{
    /// <summary>prompt02 §8: show the display name, falling back to the email when no name is set yet.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(Name) ? (Email ?? UserId) : Name;
}

/// <summary>One hit from the "add member by searching existing users" autocomplete (prompt02 §2).</summary>
public record UserSearchResultDto(string UserId, string? Name, string? Email, string? Phone)
{
    public string DisplayLabel => string.IsNullOrWhiteSpace(Name) ? (Email ?? UserId) : Name;
}

public record UpdateProfileResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static readonly UpdateProfileResult Ok = new(true, Array.Empty<string>());
    public static UpdateProfileResult Fail(params string[] errors) => new(false, errors);
}

/// <summary>
/// Abstraction over ASP.NET Identity + JWT issuing, so Application handlers don't
/// depend directly on Infrastructure/Identity types.
/// </summary>
public interface IIdentityService
{
    /// <summary>prompt02 §7: registration now only takes email + password.</summary>
    Task<AuthResult> RegisterAsync(string email, string password);

    Task<AuthResult> LoginAsync(string email, string password);

    Task<UserProfileDto?> GetProfileAsync(string userId);

    /// <summary>
    /// prompt02 §8: name and phone are optional, but when supplied must be unique across users
    /// after normalization (case-insensitive name, formatting-stripped phone).
    /// </summary>
    Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? name, string? phone, string? preferredLanguage);

    /// <summary>prompt02 §2: live search over name / email / phone for the add-member autocomplete.</summary>
    Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(string term, string? excludeUserId, int limit = 10);

    Task<IReadOnlyList<UserProfileDto>> GetProfilesAsync(IReadOnlyCollection<string> userIds);
}

/// <summary>
/// prompt02 §8 implementation note: both uniqueness checks and the search must compare
/// *normalized* values, otherwise "Anas" / "anas " or "+962 79 123" / "+96279123" would count
/// as different people. Kept here (Application layer) so the same rules apply wherever a
/// name/phone is read or written.
/// </summary>
public static class UserValueNormalizer
{
    public static string? NormalizeName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name.Trim().ToUpperInvariant();

    /// <summary>Strips spaces, dashes, dots, slashes and parentheses; keeps a leading "+".</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var trimmed = phone.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        return hasPlus ? "+" + digits : digits;
    }
}
