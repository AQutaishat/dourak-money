using Dourak.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dourak.Infrastructure.Email;

/// <summary>
/// Fallback used when no SMTP provider is configured (see EmailOptions) — logs the email
/// (including the verification/reset link itself) at Information level instead of sending it,
/// so the feature works end-to-end (the link can be copied out of the log/Seq) before real
/// email delivery is wired up. Never used once EmailOptions.Host is set.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email NOT sent (no SMTP provider configured — see Email:Host) to {ToEmail}, subject {Subject}:\n{Body}",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
