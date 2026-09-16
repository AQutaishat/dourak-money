using System.Net;
using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dourak.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    /// <summary>Surfaced verbatim to the register screen so it can show a specific message (prompt02 §Register).</summary>
    public const string EmailAlreadyRegistered = "An account with this email already exists.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _appOptions;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        JwtTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IOptions<AppOptions> appOptions)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _appOptions = appOptions.Value;
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

        // Fire-and-forget in spirit, but awaited so a mail outage is logged, not silently lost —
        // SendEmailVerificationAsync itself never throws (see its try/catch in SmtpEmailSender).
        await SendEmailVerificationAsync(user.Id);

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
        new(u.Id, u.DisplayName, u.Email, u.PhoneNumber, u.PreferredLanguage, u.EmailConfirmed);

    // ---------- Email verification & password reset ----------

    public async Task SendEmailVerificationAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.EmailConfirmed || string.IsNullOrEmpty(user.Email)) return;

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{_appOptions.FrontendBaseUrl}/verify-email?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";

        await _emailSender.SendAsync(
            user.Email,
            "Confirm your Dourak email",
            $"""
            <p>Welcome to Dourak! Please confirm your email address to finish setting up your account.</p>
            <p><a href="{link}">Verify my email</a></p>
            <p>If you didn't create this account, you can ignore this email.</p>
            """,
            cancellationToken);
    }

    public async Task<OperationResult> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return OperationResult.Fail("Invalid verification link.");
        if (user.EmailConfirmed) return OperationResult.Ok;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? OperationResult.Ok
            : OperationResult.Fail("This verification link is invalid or has expired.");
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        // Deliberately silent for a non-existent email — the caller (ForgotPasswordCommandHandler)
        // always reports success either way, so this must never let a timing/response difference
        // reveal whether an email is registered.
        if (user is null || string.IsNullOrEmpty(user.Email)) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{_appOptions.FrontendBaseUrl}/reset-password?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";

        await _emailSender.SendAsync(
            user.Email,
            "Reset your Dourak password",
            $"""
            <p>We received a request to reset your Dourak password.</p>
            <p><a href="{link}">Reset my password</a></p>
            <p>If you didn't request this, you can safely ignore this email — your password won't change.</p>
            """,
            cancellationToken);
    }

    public async Task<OperationResult> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return OperationResult.Fail("This reset link is invalid or has expired.");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? OperationResult.Ok
            : OperationResult.Fail(result.Errors.Select(e => e.Description).ToArray());
    }
}
