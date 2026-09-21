using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// An MCP client (e.g. Claude Desktop) that dynamically registered itself against
/// <c>POST /api/oauth/register</c> (RFC 7591) before starting the authorization-code flow. Public
/// client only — no client secret, since the MCP spec expects native/desktop apps here and relies
/// on PKCE (see <see cref="OAuthAuthorizationCode"/>) instead of a confidential secret.
/// </summary>
public class OAuthClient : AuditableEntity
{
    /// <summary>Opaque id handed back at registration time; what the client sends as client_id afterwards.</summary>
    public string ClientId { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    /// <summary>Comma-separated — a client registers one or more redirect URIs up front.</summary>
    public string RedirectUris { get; set; } = string.Empty;

    public IReadOnlyList<string> RedirectUriList =>
        RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
