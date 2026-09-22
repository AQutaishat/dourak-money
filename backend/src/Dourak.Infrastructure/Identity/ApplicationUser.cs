using Microsoft.AspNetCore.Identity;

namespace Dourak.Infrastructure.Identity;

/// <summary>
/// User account (BRD §6.1). Phase 2: a user can be an organizer, a circle member, or both.
/// The phone number itself lives on <see cref="IdentityUser.PhoneNumber"/> (prompt02 §1).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Optional from Phase 2 on — registration only asks for email + password (prompt02 §7).</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// prompt02 §8: name and phone must be unique across users after normalization, otherwise
    /// "Anas" vs "anas " (or "+962 79…" vs "+96279…") would slip past the check. These mirror
    /// the way Identity already keeps NormalizedEmail alongside Email, and carry the unique indexes.
    /// </summary>
    public string? NormalizedDisplayName { get; set; }
    public string? NormalizedPhoneNumber { get; set; }

    public string PreferredLanguage { get; set; } = "ar";
    public string DefaultCurrency { get; set; } = "SAR";
    public string? TimeZone { get; set; }

    /// <summary>
    /// When the account was created. Nullable because it was added after users already existed —
    /// pre-existing rows are backfilled to null rather than a guessed timestamp; the admin UI
    /// renders those as "—".
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }
}
