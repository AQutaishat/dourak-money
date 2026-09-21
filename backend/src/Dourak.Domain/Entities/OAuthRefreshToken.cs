using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// Long-lived opaque token letting an MCP client (e.g. Claude Desktop) get a fresh short-lived
/// JWT access token via <c>POST /api/oauth/token</c> (grant_type=refresh_token) without the user
/// re-authenticating every week — the gap flagged in docs/future-work.md before this file existed.
/// Rotated on every use (the old row is marked <see cref="Revoked"/>, a new one issued) so a
/// leaked-and-reused refresh token is detectable, not just replayable forever.
/// </summary>
public class OAuthRefreshToken : AuditableEntity
{
    public string Token { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
