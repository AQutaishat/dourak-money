using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dourak.Infrastructure.Identity;

public class JwtTokenGenerator
{
    private readonly JwtSettings _settings;
    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    /// <summary>
    /// <paramref name="roles"/> becomes one "role" claim per role (e.g. "Admin") — the admin
    /// site's [Authorize(Roles = "Admin")] endpoints check this. Callers fetch roles via
    /// UserManager.GetRolesAsync before calling Generate (kept synchronous/simple here).
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) Generate(ApplicationUser user, IEnumerable<string>? roles = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            // DisplayName is optional from Phase 2 on; fall back to the email so the token
            // always carries something usable for the account menu label (prompt02 §8).
            new("displayName", string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? string.Empty) : user.DisplayName),
            new("preferredLanguage", user.PreferredLanguage),
        };
        foreach (var role in roles ?? Enumerable.Empty<string>())
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
