using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dourak.Api.Controllers;

/// <summary>
/// OAuth 2.1 authorization server in front of the app's existing email/password login, so remote
/// MCP clients that require a real OAuth handshake (Claude Desktop's "Connect" button — unlike
/// ChatGPT's custom connector, it has no field to just paste a bearer token) can obtain a token
/// for <c>/api/mcp</c> without the user ever handling a raw JWT. Public-client only: no client
/// secret anywhere (see <see cref="OAuthClient"/>), security instead comes from PKCE
/// (RFC 7636, S256 only) on the authorization code and from rotating refresh tokens — exactly
/// what the MCP authorization spec expects for a desktop/native client.
///
/// Flow: client discovers this server's endpoints -> POST /register (RFC 7591) -> browser opens
/// GET /authorize (renders a plain login form, no session/cookie needed since the whole exchange
/// completes in a single request-response with the OAuth params carried as hidden fields) ->
/// POST /authorize checks the password via the same IIdentityService used by the normal API login,
/// issues a short-lived code, redirects back to the client's redirect_uri -> POST /token exchanges
/// the code (+ PKCE verifier) for an access token (a normal Dourak JWT) and a refresh token.
/// </summary>
[ApiController]
[Route("api/oauth")]
public class OAuthController : ControllerBase
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(90);

    private readonly IAppDbContext _db;
    private readonly IIdentityService _identityService;
    private readonly AppOptions _appOptions;

    public OAuthController(IAppDbContext db, IIdentityService identityService, IOptions<AppOptions> appOptions)
    {
        _db = db;
        _identityService = identityService;
        _appOptions = appOptions.Value;
    }

    // ---------- Dynamic client registration (RFC 7591) ----------

    public record RegisterClientRequest(string? Client_Name, List<string>? Redirect_Uris);

    [HttpPost("register")]
    public async Task<IActionResult> RegisterClient(RegisterClientRequest request, CancellationToken cancellationToken)
    {
        if (request.Redirect_Uris is null || request.Redirect_Uris.Count == 0)
            return BadRequest(new { error = "invalid_client_metadata", error_description = "redirect_uris is required." });

        var client = new OAuthClient
        {
            ClientId = Guid.NewGuid().ToString("N"),
            ClientName = string.IsNullOrWhiteSpace(request.Client_Name) ? "MCP client" : request.Client_Name,
            RedirectUris = string.Join(',', request.Redirect_Uris)
        };
        _db.OAuthClients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            client_id = client.ClientId,
            client_name = client.ClientName,
            redirect_uris = request.Redirect_Uris,
            token_endpoint_auth_method = "none",
            grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" }
        });
    }

    // ---------- Authorization endpoint: login form + consent, all in one request ----------

    [HttpGet("authorize")]
    public async Task<IActionResult> AuthorizeGet(
        [FromQuery] string client_id, [FromQuery] string redirect_uri, [FromQuery] string response_type,
        [FromQuery] string code_challenge, [FromQuery] string? code_challenge_method,
        [FromQuery] string? state, [FromQuery] string? scope, CancellationToken cancellationToken)
    {
        var validation = await ValidateAuthorizeRequestAsync(client_id, redirect_uri, response_type, code_challenge, code_challenge_method, cancellationToken);
        if (validation is not null) return validation;

        return Content(RenderLoginPage(client_id, redirect_uri, code_challenge, code_challenge_method ?? "S256", state, scope, error: null), "text/html");
    }

    public record AuthorizeFormRequest(
        string Email, string Password, string Client_Id, string Redirect_Uri,
        string Code_Challenge, string Code_Challenge_Method, string? State, string? Scope);

    [HttpPost("authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AuthorizePost([FromForm] AuthorizeFormRequest form, CancellationToken cancellationToken)
    {
        var validation = await ValidateAuthorizeRequestAsync(
            form.Client_Id, form.Redirect_Uri, "code", form.Code_Challenge, form.Code_Challenge_Method, cancellationToken);
        if (validation is not null) return validation;

        var login = await _identityService.LoginAsync(form.Email, form.Password);
        if (!login.Succeeded)
        {
            return Content(RenderLoginPage(
                form.Client_Id, form.Redirect_Uri, form.Code_Challenge, form.Code_Challenge_Method,
                form.State, form.Scope, error: "Invalid email or password."), "text/html");
        }

        var code = new OAuthAuthorizationCode
        {
            Code = GenerateOpaqueToken(),
            ClientId = form.Client_Id,
            UserId = login.UserId!,
            RedirectUri = form.Redirect_Uri,
            CodeChallenge = form.Code_Challenge,
            CodeChallengeMethod = string.IsNullOrWhiteSpace(form.Code_Challenge_Method) ? "S256" : form.Code_Challenge_Method,
            ExpiresAt = DateTimeOffset.UtcNow.Add(CodeLifetime)
        };
        _db.OAuthAuthorizationCodes.Add(code);
        await _db.SaveChangesAsync(cancellationToken);

        var redirect = form.Redirect_Uri
            + (form.Redirect_Uri.Contains('?') ? '&' : '?')
            + "code=" + Uri.EscapeDataString(code.Code)
            + (string.IsNullOrEmpty(form.State) ? "" : "&state=" + Uri.EscapeDataString(form.State));
        return Redirect(redirect);
    }

    private async Task<IActionResult?> ValidateAuthorizeRequestAsync(
        string clientId, string redirectUri, string responseType, string codeChallenge, string? codeChallengeMethod,
        CancellationToken cancellationToken)
    {
        if (responseType != "code")
            return BadRequest(new { error = "unsupported_response_type" });
        if (string.IsNullOrWhiteSpace(codeChallenge) || (codeChallengeMethod is not null && codeChallengeMethod != "S256"))
            return BadRequest(new { error = "invalid_request", error_description = "PKCE with S256 is required." });

        var client = await _db.OAuthClients.FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
        if (client is null) return BadRequest(new { error = "invalid_client" });
        if (!client.RedirectUriList.Contains(redirectUri))
            return BadRequest(new { error = "invalid_request", error_description = "redirect_uri is not registered for this client." });

        return null;
    }

    private static string RenderLoginPage(
        string clientId, string redirectUri, string codeChallenge, string codeChallengeMethod,
        string? state, string? scope, string? error)
    {
        var enc = HtmlEncoder.Default;
        var errorHtml = error is null ? "" : $"<p style=\"color:#c0392b\">{enc.Encode(error)}</p>";
        const string style = "body{font-family:system-ui,sans-serif;max-width:380px;margin:60px auto;padding:0 16px}"
            + "h1{font-size:20px}input{width:100%;padding:10px;margin:6px 0 14px;box-sizing:border-box;font-size:16px}"
            + "button{width:100%;padding:10px;font-size:16px;background:#2e7d32;color:#fff;border:none;border-radius:4px}";

        return "<!DOCTYPE html>"
            + "<html lang=\"en\"><head><meta charset=\"utf-8\"><title>Sign in to Dourak</title>"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<style>" + style + "</style></head><body>"
            + "<h1>Sign in to connect Dourak</h1>"
            + "<p>An app is requesting access to your Dourak account.</p>"
            + errorHtml
            + "<form method=\"post\" action=\"/api/oauth/authorize\">"
            + $"<input type=\"hidden\" name=\"Client_Id\" value=\"{enc.Encode(clientId)}\">"
            + $"<input type=\"hidden\" name=\"Redirect_Uri\" value=\"{enc.Encode(redirectUri)}\">"
            + $"<input type=\"hidden\" name=\"Code_Challenge\" value=\"{enc.Encode(codeChallenge)}\">"
            + $"<input type=\"hidden\" name=\"Code_Challenge_Method\" value=\"{enc.Encode(codeChallengeMethod)}\">"
            + $"<input type=\"hidden\" name=\"State\" value=\"{enc.Encode(state ?? "")}\">"
            + $"<input type=\"hidden\" name=\"Scope\" value=\"{enc.Encode(scope ?? "")}\">"
            + "<label>Email<input type=\"email\" name=\"Email\" required autofocus></label>"
            + "<label>Password<input type=\"password\" name=\"Password\" required></label>"
            + "<button type=\"submit\">Sign in and connect</button>"
            + "</form></body></html>";
    }

    // ---------- Token endpoint ----------

    public record TokenFormRequest(
        string Grant_Type, string? Code, string? Redirect_Uri, string? Client_Id,
        string? Code_Verifier, string? Refresh_Token);

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenFormRequest form, CancellationToken cancellationToken)
    {
        return form.Grant_Type switch
        {
            "authorization_code" => await ExchangeCodeAsync(form, cancellationToken),
            "refresh_token" => await ExchangeRefreshTokenAsync(form, cancellationToken),
            _ => BadRequest(new { error = "unsupported_grant_type" })
        };
    }

    private async Task<IActionResult> ExchangeCodeAsync(TokenFormRequest form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(form.Code) || string.IsNullOrEmpty(form.Code_Verifier) || string.IsNullOrEmpty(form.Redirect_Uri))
            return BadRequest(new { error = "invalid_request" });

        var code = await _db.OAuthAuthorizationCodes.FirstOrDefaultAsync(c => c.Code == form.Code, cancellationToken);
        if (code is null || code.Used || code.ExpiresAt < DateTimeOffset.UtcNow)
            return BadRequest(new { error = "invalid_grant" });
        if (code.RedirectUri != form.Redirect_Uri || (form.Client_Id is not null && code.ClientId != form.Client_Id))
            return BadRequest(new { error = "invalid_grant" });
        if (!VerifyPkce(code.CodeChallenge, form.Code_Verifier))
            return BadRequest(new { error = "invalid_grant", error_description = "PKCE verification failed." });

        code.Used = true;
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokenResponseAsync(code.UserId, code.ClientId, cancellationToken);
    }

    private async Task<IActionResult> ExchangeRefreshTokenAsync(TokenFormRequest form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(form.Refresh_Token)) return BadRequest(new { error = "invalid_request" });

        var stored = await _db.OAuthRefreshTokens.FirstOrDefaultAsync(t => t.Token == form.Refresh_Token, cancellationToken);
        if (stored is null || stored.Revoked || stored.ExpiresAt < DateTimeOffset.UtcNow)
            return BadRequest(new { error = "invalid_grant" });

        // Rotate: this token is single-use, a fresh one is minted below by IssueTokenResponseAsync.
        stored.Revoked = true;
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokenResponseAsync(stored.UserId, stored.ClientId, cancellationToken);
    }

    private async Task<IActionResult> IssueTokenResponseAsync(string userId, string clientId, CancellationToken cancellationToken)
    {
        var auth = await _identityService.IssueTokenAsync(userId);
        if (!auth.Succeeded) return BadRequest(new { error = "invalid_grant", error_description = string.Join(' ', auth.Errors) });

        var refreshToken = new OAuthRefreshToken
        {
            Token = GenerateOpaqueToken(),
            ClientId = clientId,
            UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime)
        };
        _db.OAuthRefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        var expiresIn = (int)(auth.ExpiresAt!.Value - DateTimeOffset.UtcNow).TotalSeconds;
        return Ok(new
        {
            access_token = auth.Token,
            token_type = "Bearer",
            expires_in = expiresIn,
            refresh_token = refreshToken.Token
        });
    }

    // ---------- Metadata (RFC 8414 / RFC 9728) — see also the Caddyfile routes to /.well-known/oauth-* ----------

    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult AuthorizationServerMetadata()
    {
        var baseUrl = _appOptions.FrontendBaseUrl.TrimEnd('/');
        return Ok(new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/api/oauth/authorize",
            token_endpoint = $"{baseUrl}/api/oauth/token",
            registration_endpoint = $"{baseUrl}/api/oauth/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" }
        });
    }

    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult ProtectedResourceMetadata()
    {
        var baseUrl = _appOptions.FrontendBaseUrl.TrimEnd('/');
        return Ok(new
        {
            resource = $"{baseUrl}/api/mcp",
            authorization_servers = new[] { baseUrl }
        });
    }

    // ---------- Helpers ----------

    private static string GenerateOpaqueToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    /// <summary>RFC 7636 S256: challenge is base64url(SHA256(verifier)).</summary>
    private static bool VerifyPkce(string codeChallenge, string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var computed = WebEncoders.Base64UrlEncode(hash);
        return computed == codeChallenge;
    }
}
