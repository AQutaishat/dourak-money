using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dourak.Infrastructure.Identity;

public class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Empty by default — no admin account is seeded until this is set (via .env's
    /// ADMIN_EMAIL, see docker-compose.yml), so a fresh environment never has a surprise
    /// pre-existing admin login.</summary>
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Ensures the "Admin" role exists, and — when Admin:Email/Password are configured — that
/// account has it. Idempotent (safe on every startup): if the account already exists (e.g. a
/// regular user is being promoted), only the role is added, its password is left untouched.
/// </summary>
public static class AdminSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<AdminOptions> options,
        ILogger logger)
    {
        if (!await roleManager.RoleExistsAsync(AdminRole))
            await roleManager.CreateAsync(new IdentityRole(AdminRole));

        var adminOptions = options.Value;
        if (string.IsNullOrWhiteSpace(adminOptions.Email)) return;

        var user = await userManager.FindByEmailAsync(adminOptions.Email);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(adminOptions.Password))
            {
                logger.LogWarning("Admin:Email is set but Admin:Password is empty — skipping admin account creation for {Email}.", adminOptions.Email);
                return;
            }

            user = new ApplicationUser
            {
                UserName = adminOptions.Email,
                Email = adminOptions.Email,
                EmailConfirmed = true,
                DisplayName = "Admin",
                NormalizedDisplayName = "ADMIN",
                PreferredLanguage = "en",
            };

            var createResult = await userManager.CreateAsync(user, adminOptions.Password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning(
                    "Admin account seed failed for {Email}: {Errors}",
                    adminOptions.Email, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, AdminRole))
            await userManager.AddToRoleAsync(user, AdminRole);
    }
}
