namespace Dourak.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Dourak";
    public string Audience { get; set; } = "DourakClient";
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7; // 7 days — organizer-only app, no refresh-token flow in Phase 1.
}
