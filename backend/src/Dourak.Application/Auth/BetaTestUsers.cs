namespace Dourak.Application.Auth;

/// <summary>
/// prompt03 §4 — TEMPORARY beta-testing mechanism, not a permanent product rule.
///
/// Four fixed seed accounts exist for beta testing (seeded by Infrastructure at startup, see
/// <c>BetaUserSeeder</c>): user1/user2 auto-accept any invitation the moment they're added to a
/// circle (no pending step, bypassing normal invitation consent), while user3/user4 go through
/// the ordinary invite → pending → accept/decline flow like any real user.
///
/// TODO before real production launch: remove this auto-accept bypass entirely (see
/// docs/future-work.md) — a real user must always explicitly consent to joining a circle.
/// </summary>
public static class BetaTestUsers
{
    public static readonly IReadOnlyCollection<string> AutoAcceptEmails = new[]
    {
        "user1@dourak.test",
        "user2@dourak.test",
    };

    public static bool IsAutoAccept(string? email) =>
        !string.IsNullOrWhiteSpace(email) && AutoAcceptEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
}
