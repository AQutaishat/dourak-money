namespace Dourak.Application.Common.Interfaces;

/// <summary>
/// Abstraction over sending an email. Infrastructure provides an SMTP-backed
/// implementation when configured, or a logging fallback when it isn't (see
/// docs/future-work.md) — so email verification / password reset work end-to-end
/// (link is generated, click-through still works when the link is copied from
/// the log) even before real SMTP credentials are set up on a given environment.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
