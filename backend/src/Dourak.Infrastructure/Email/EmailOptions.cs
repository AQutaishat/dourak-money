namespace Dourak.Infrastructure.Email;

/// <summary>
/// SMTP settings for outbound email. When <see cref="Host"/> is empty (the default — no real
/// mail provider configured yet, see docs/future-work.md), DependencyInjection registers
/// <see cref="LoggingEmailSender"/> instead of <see cref="SmtpEmailSender"/>, so verification/
/// reset emails "send" by writing their content (including the actual link) to the structured
/// log instead of failing outright. Once a provider (Zoho Mail, Resend SMTP, etc.) is set up,
/// filling these in switches to real delivery with no code change.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@dourak.money";
    public string FromName { get; set; } = "Dourak";
    public bool UseSsl { get; set; } = true;
}
