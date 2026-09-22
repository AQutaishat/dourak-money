using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record ClientLogRequest(string Level, string Message, string? Details);

/// <summary>
/// Lets a client app (currently: mobile) relay a log entry through the API's own Serilog
/// pipeline — which already sinks to Console + a rolling file + Seq (see Program.cs /
/// appsettings.json's "Serilog:WriteTo") — instead of talking to Seq directly. Seq itself is
/// deliberately bound to 127.0.0.1 on the server (see README's security note on Adminer/Seq)
/// and is never reachable from a phone on the public internet, so this relay is the only way a
/// mobile-side event reaches Seq in production. It also means the app ships with no Seq
/// ingestion URL or API key at all — just the same API base URL it already talks to.
///
/// Deliberately a plain controller action rather than going through MediatR like every other
/// endpoint in this API: there's no domain logic here, just a structured-logging passthrough,
/// and routing it through a Command/Handler would add a layer for its own sake.
///
/// Requires auth so this can't be used as an open, unauthenticated log-spam endpoint — the
/// tradeoff is that a crash before login (or a login failure itself) isn't relayed. If that
/// turns out to matter, revisit with a coarser rate limit instead of removing [Authorize].
/// </summary>
[ApiController]
[Authorize]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger;
    public DiagnosticsController(ILogger<DiagnosticsController> logger) => _logger = logger;

    [HttpPost("log")]
    public IActionResult Log(ClientLogRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var level = request.Level.ToLowerInvariant() switch
        {
            "error" => LogLevel.Error,
            "warning" => LogLevel.Warning,
            _ => LogLevel.Information,
        };
        // Structured (not string-interpolated) so Seq indexes Message/Details/UserId as their
        // own searchable/filterable fields, not just baked into one opaque line of text.
        _logger.Log(level, "[MobileApp] {Message} — {Details} (user {UserId})", request.Message, request.Details, userId);
        return NoContent();
    }
}
