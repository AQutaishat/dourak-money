using Dourak.Application.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    /// <summary>Surfaced verbatim to the register screen so it can show a specific message (prompt02 §Register).</summary>
    public const string EmailAlreadyRegistered = "An account with this email already exists.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;

    public IdentityService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return new AuthResult(false, null, null, null, new[] { EmailAlreadyRegistered });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            // prompt02 §7: no name or preferred language at registration — set later from the profile page.
            DisplayName = null,
            PreferredLanguage = "ar"
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
            return new AuthResult(false, null, null, null, new[] { "Invalid credentials." });

        var (token, expiresAt) = _tokenGenerator.Generate(user);
        return new AuthResult(true, user.Id, token, expiresAt, Array.Empty<string>());
    }

    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : ToProfile(user);
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetProfilesAsync(IReadOnlyCollection<string> userIds)
    {
        if (userIds.Count == 0) return Array.Empty<UserProfileDto>();
        var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        return users.Select(ToProfile).ToList();
    }

    public async Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? name, string? phone, string? preferredLanguage)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return UpdateProfileResult.Fail("User not found.");

        var normalizedName = UserValueNormalizer.NormalizeName(name);
        var normalizedPhone = UserValueNormalizer.NormalizePhone(phone);

        // prompt02 §8: name and phone are optional, but unique when supplied — compared on the
        // normalized value so trivially different spellings can't both be stored.
        if (normalizedName is not null)
        {
            var nameTaken = await _userManager.Users
                .AnyAsync(u => u.Id != userId && u.NormalizedDisplayName == normalizedName);
            if (nameTaken) return UpdateProfileResult.Fail("This name is already used by another account.");
        }

        if (normalizedPhone is not null)
        {
            var phoneTaken = await _userManager.Users
                .AnyAsync(u => u.Id != userId && u.NormalizedPhoneNumber == normalizedPhone);
            if (phoneTaken) return UpdateProfileResult.Fail("This phone number is already used by another account.");
        }

        user.DisplayName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        user.NormalizedDisplayName = normalizedName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.NormalizedPhoneNumber = normalizedPhone;
        if (preferredLanguage is "ar" or "en") user.PreferredLanguage = preferredLanguage;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? UpdateProfileResult.Ok
            : UpdateProfileResult.Fail(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(string term, string? excludeUserId, int limit = 10)
    {
        // prompt02 §2: match on name, email or phone. Name/phone are matched on their normalized
        // form so "anas" finds "Anas" and "079 123" finds "+96279123".
        var upper = term.ToUpperInvariant();
        var phoneTerm = UserValueNormalizer.NormalizePhone(term);

        var query = _userManager.Users.Where(u =>
            (u.NormalizedDisplayName != null && u.NormalizedDisplayName.Contains(upper))
            || (u.NormalizedEmail != null && u.NormalizedEmail.Contains(upper))
            || (phoneTerm != null && u.NormalizedPhoneNumber != null && u.NormalizedPhoneNumber.Contains(phoneTerm)));

        if (!string.IsNullOrEmpty(excludeUserId))
            query = query.Where(u => u.Id != excludeUserId);

        var users = await query.OrderBy(u => u.DisplayName ?? u.Email).Take(limit).ToListAsync();
        return users.Select(u => new UserSearchResultDto(u.Id, u.DisplayName, u.Email, u.PhoneNumber)).ToList();
    }

    private static UserProfileDto ToProfile(ApplicationUser u) =>
        new(u.Id, u.DisplayName, u.Email, u.PhoneNumber, u.PreferredLanguage);
}
