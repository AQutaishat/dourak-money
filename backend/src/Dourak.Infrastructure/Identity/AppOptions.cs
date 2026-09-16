namespace Dourak.Infrastructure.Identity;

/// <summary>
/// General app-wide config not specific to Identity/JWT — currently just the base URL of the
/// web frontend, used to build email-verification and password-reset links
/// (e.g. "https://dourak.money" -> "https://dourak.money/verify-email?userId=...&amp;token=...").
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>No trailing slash. Falls back to the local Vite dev server for local dev.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
