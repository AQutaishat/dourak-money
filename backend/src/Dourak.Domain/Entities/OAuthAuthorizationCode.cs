using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// A short-lived, single-use code issued after the user logs in on the OAuth consent page
/// (<c>GET/POST /api/oauth/authorize</c>), exchanged for real tokens at <c>POST /api/oauth/token</c>.
/// PKCE-only (no client secret — see <see cref="OAuthClient"/>): <see cref="CodeChallenge"/> is
/// checked against the code_verifier the client presents at exchange time, exactly like every
/// public OAuth client (mobile/desktop apps, MCP clients) is expected to do.
/// </summary>
public class OAuthAuthorizationCode : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
}
