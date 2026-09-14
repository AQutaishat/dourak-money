using System.Net;
using System.Text.Json;
using Dourak.Application.Common.Exceptions;
using Dourak.Domain.Exceptions;
using FluentValidation;

namespace Dourak.Api.Middleware;

/// <summary>
/// Translates Application/Domain exceptions into consistent ProblemDetails-style JSON,
/// so controllers stay free of try/catch noise.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, errors) = exception switch
        {
            ValidationException vex => (HttpStatusCode.BadRequest, "Validation failed.",
                vex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException nf => (HttpStatusCode.NotFound, nf.Message, null),
            ForbiddenAccessException fa => (HttpStatusCode.Forbidden, fa.Message, null),
            DomainException de => (HttpStatusCode.BadRequest, de.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = new { title, status = (int)status, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
