using Microsoft.AspNetCore.Identity;

namespace Dourak.Infrastructure.Identity;

/// <summary>Organizer account (BRD §6.1). Minimal profile: name, preferred language, currency, time zone.</summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "ar";
    public string DefaultCurrency { get; set; } = "SAR";
    public string? TimeZone { get; set; }
}
