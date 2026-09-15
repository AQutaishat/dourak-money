using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dourak.Infrastructure.Identity;

/// <summary>
/// prompt03 §4 — TEMPORARY beta-testing mechanism, not a permanent product rule.
///
/// Seeds four fixed beta-test accounts (idempotent — safe to run on every startup) so testers
/// have real, ready-to-use logins without going through registration. user1/user2 are also the
/// two accounts <see cref="Dourak.Application.Auth.BetaTestUsers"/> auto-accepts into any circle
/// they're added to; user3/user4 exist purely as normal accounts for the ordinary invite flow.
///
/// TODO before real production launch: remove this seeder entirely (see docs/future-work.md) —
/// fixed, publicly-documented test credentials must never exist in a real deployment.
/// </summary>
public static class BetaUserSeeder
{
    public const string DefaultPassword = "Beta1234!";

    private static readonly (string Email, string DisplayName)[] Accounts =
    {
        ("user1@dourak.test", "Beta User 1"),
        ("user2@dourak.test", "Beta User 2"),
        ("user3@dourak.test", "Beta User 3"),
        ("user4@dourak.test", "Beta User 4"),
    };

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        foreach (var (email, displayName) in Accounts)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                NormalizedDisplayName = displayName.ToUpperInvariant(),
                PreferredLanguage = "ar",
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Beta user seed failed for {Email}: {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
