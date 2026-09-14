using Dourak.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace Dourak.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;

    public IdentityService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResult> RegisterAsync(string name, string email, string password, string preferredLanguage)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return new AuthResult(false, null, null, null, new[] { "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = name,
            PreferredLanguage = preferredLanguage
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new AuthResult(false, null, null, null, result.Errors.Select(e => e.Description).ToList());

        var (token, expiresAt) = _tokenGenerator.Generate(user);
        return new AuthResult(true, user.Id, token, expiresAt, Array.Empty<string>());
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return new AuthResult(false, null, null, null, new[] { "Invalid email or password." });

        var (token, expiresAt) = _tokenGenerator.Generate(user);
        return new AuthResult(true, user.Id, token, expiresAt, Array.Empty<string>());
    }

    public async Task<(string? Name, string? Email, string? PreferredLanguage)> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? (null, null, null) : (user.DisplayName, user.Email, user.PreferredLanguage);
    }

    public async Task<bool> UpdateProfileAsync(string userId, string name, string preferredLanguage)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.DisplayName = name;
        user.PreferredLanguage = preferredLanguage;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
