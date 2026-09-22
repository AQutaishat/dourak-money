using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

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
        var level = request.Level.ToLowerInvariant() switch
        {
            "error" => LogLevel.Error,
            "warning" => LogLevel.Warning,
            _ => LogLevel.Information,
        };
        // Source="MobileApp" for just this one log event — overrides the process-wide
        // Source="Dourak.Api" default set in Program.cs (see its comment on enricher ordering).
        // UserId/RequestId are already on the ambient LogContext from
        // RequestLogEnrichmentMiddleware (this endpoint requires auth, so UserId is always set),
        // so this call no longer needs to extract or pass either one itself.
        using (LogContext.PushProperty("Source", "MobileApp"))
        {
            _logger.Log(level, "{Message} — {Details}", request.Message, request.Details);
        }
        return NoContent();
    }
}
