using System.Net;
using System.Net.Mail;
using Dourak.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dourak.Infrastructure.Email;

/// <summary>Real SMTP delivery — active once <see cref="EmailOptions.Host"/> is configured.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let a mail-provider outage break registration/reset flows for the caller —
            // the token is already generated and valid; log so it's diagnosable, and the user
            // can use "resend" once the provider issue is fixed.
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
        }
    }
}
