using System.Security.Claims;
using Serilog.Context;

namespace Dourak.Api.Middleware;

/// <summary>
/// Pushes per-request Serilog context properties — RequestId and, when authenticated, UserId — so
/// every log event emitted while handling this request (including UseSerilogRequestLogging's own
/// summary line, since this middleware is registered before it) can be correlated back to the
/// same request and the same signed-in user without every individual log call needing to pass
/// them explicitly. Registered after UseAuthentication/UseAuthorization so ClaimsPrincipal is
/// already populated by the time this runs.
/// </summary>
public class RequestLogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    public RequestLogEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (userId != null ? LogContext.PushProperty("UserId", userId) : NoopDisposable.Instance)
        {
            await _next(context);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
