using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dourak.Infrastructure.Identity;

public class AdminAccountOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Static, config-defined admin accounts — several are supported (not just one), each
    /// bound the same way appsettings.json/env vars already bind Cors:AllowedOrigins and
    /// Serilog:WriteTo arrays elsewhere in this project (Admin__Accounts__0__Email, etc. via
    /// docker-compose.yml). Empty by default — no admin account exists until this is set.
    /// </summary>
    public List<AdminAccountOptions> Accounts { get; set; } = new();
}

/// <summary>
/// Ensures the "Admin" role exists, and every configured account (see AdminOptions.Accounts)
/// exists and has it. Idempotent (safe on every startup): an account that already exists
/// (e.g. a regular user being promoted by listing their email here) only gets the role added,
/// its password is left untouched — only a brand-new account is created with the configured
/// password.
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

        foreach (var account in options.Value.Accounts)
        {
            if (string.IsNullOrWhiteSpace(account.Email)) continue;

            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(account.Password))
                {
                    logger.LogWarning("Admin account {Email} has no password configured — skipping creation (it doesn't exist yet).", account.Email);
                    continue;
                }

                user = new ApplicationUser
                {
                    UserName = account.Email,
                    Email = account.Email,
                    EmailConfirmed = true,
                    DisplayName = "Admin",
                    NormalizedDisplayName = "ADMIN",
                    PreferredLanguage = "en",
                };

                var createResult = await userManager.CreateAsync(user, account.Password);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning(
                        "Admin account seed failed for {Email}: {Errors}",
                        account.Email, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(user, AdminRole))
                await userManager.AddToRoleAsync(user, AdminRole);
        }
    }
}
